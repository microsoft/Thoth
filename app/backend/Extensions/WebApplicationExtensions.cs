// Copyright (c) Microsoft. All rights reserved.

using MinimalApi.Models;

namespace MinimalApi.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapApi(this WebApplication app)
    {
        var api = app.MapGroup("api");

        // Blazor 📎 Clippy streaming endpoint
        api.MapPost("openai/chat", OnPostChatPromptAsync);

        // Long-form chat w/ contextual history endpoint
        api.MapPost("chat", OnPostChatAsync);

        // Get chat sessions
        api.MapGet("chatsessions", OnGetChatSessionsAsync);

        // Get A chat session
        api.MapGet("chatsessions/{sessionId}", OnGetChatSessionAsync);

		// Upsert a chat session
		api.MapPost("chatsessions/{sessionId}", OnPostChatSessionAsync);

        // Get all documents
        api.MapGet("documents", OnGetDocumentsAsync);        

        api.MapGet("enableLogout", OnGetEnableLogout);

        return app;
    }

    private static IResult OnGetEnableLogout(HttpContext context)
    {
        var header = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
        var enableLogout = !string.IsNullOrEmpty(header);

        return TypedResults.Ok(enableLogout);
    }

    private static async IAsyncEnumerable<ChatChunkResponse> OnPostChatPromptAsync(
        PromptRequest prompt,
        OpenAIClient client,
        IConfiguration config,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var deploymentId = config["AZURE_OPENAI_CHATGPT_DEPLOYMENT"];
        var response = await client.GetChatCompletionsStreamingAsync(
            new ChatCompletionsOptions
            {
                DeploymentName = deploymentId,
                Messages =
                {
                    new ChatRequestSystemMessage("""
                        You're an AI assistant for developers, helping them write code more efficiently.
                        You're name is **Blazor 📎 Clippy** and you're an expert Blazor developer.
                        You're also an expert in ASP.NET Core, C#, TypeScript, and even JavaScript.
                        You will always reply with a Markdown formatted response.
                        """),
                    new ChatRequestUserMessage("What's your name?"),
                    new ChatRequestAssistantMessage("Hi, my name is **Blazor 📎 Clippy**! Nice to meet you."),
                    new ChatRequestUserMessage(prompt.Prompt)
                }
            }, cancellationToken);

        await foreach (var choice in response.WithCancellation(cancellationToken))
        {
            if (choice.ContentUpdate is { Length: > 0 })
            {
                yield return new ChatChunkResponse(choice.ContentUpdate.Length, choice.ContentUpdate);
            }
        }
    }

    private static async Task<IResult> OnPostChatAsync(
        ChatRequest request,
        ReadRetrieveReadChatService chatService,
        CancellationToken cancellationToken)
    {
        if (request is { History.Length: > 0 })
        {
            var response = await chatService.ReplyAsync(
                request.History, request.Overrides, cancellationToken);

            return TypedResults.Ok(response);
        }

        return Results.BadRequest();
    }    

    private static async IAsyncEnumerable<DocumentResponse> OnGetDocumentsAsync(
        BlobContainerClient client,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var blob in client.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            if (blob is not null and { Deleted: false })
            {
                var props = blob.Properties;
                var baseUri = client.Uri;
                var builder = new UriBuilder(baseUri);
                builder.Path += $"/{blob.Name}";

                var metadata = blob.Metadata;
                var documentProcessingStatus = GetMetadataEnumOrDefault<DocumentProcessingStatus>(
                    metadata, nameof(DocumentProcessingStatus), DocumentProcessingStatus.NotProcessed);
                var embeddingType = GetMetadataEnumOrDefault<EmbeddingType>(
                    metadata, nameof(EmbeddingType), EmbeddingType.AzureSearch);

                yield return new(
                    blob.Name,
                    props.ContentType,
                    props.ContentLength ?? 0,
                    props.LastModified,
                    builder.Uri,
                    documentProcessingStatus,
                    embeddingType);

                static TEnum GetMetadataEnumOrDefault<TEnum>(
                    IDictionary<string, string> metadata,
                    string key,
                    TEnum @default) where TEnum : struct => metadata.TryGetValue(key, out var value)
                        && Enum.TryParse<TEnum>(value, out var status)
                            ? status
                            : @default;
            }
        }
    }

    private static async Task<IResult> OnGetChatSessionsAsync(
        [FromServices] ILogger<ChatHistoryService> logger,
        [FromServices] IChatHistoryService chatHistory,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get chat history sessions");

        var sessions = chatHistory.GetChatHistorySessionsAsync("philbert");
		IEnumerable<ChatSessionListResponse> response = new List<ChatSessionListResponse>();
		await foreach (var session in sessions)
		{
			response = response.Append(new ChatSessionListResponse { SessionId = session.Id, Title = session.Title });
		}

        return TypedResults.Ok(response);
    }

    private static async Task<IResult> OnGetChatSessionAsync(
        [FromRoute] string sessionId,
        [FromServices] ILogger<ChatHistoryService> logger,
        [FromServices] IChatHistoryService chatHistory,
        CancellationToken cancellationToken)
    {
        logger.LogInformation($"Get chat history for session with id: {sessionId}");

		// put try/catch here
        var response = await chatHistory.GetChatHistorySessionAsync(sessionId);

        return TypedResults.Ok(response);
    }

	private static async Task<IResult> OnPostChatSessionAsync(
		[FromRoute] string sessionId,
		ChatHistorySession chatHistorySession,
		[FromServices] ILogger<ChatHistoryService> logger,
		[FromServices] IChatHistoryService chatHistory,
		CancellationToken cancellationToken)
	{
		logger.LogInformation($"Add or update chat history with id: {sessionId}");

		if (!sessionId.Equals(chatHistorySession.Id, StringComparison.InvariantCultureIgnoreCase))
		{
			return Results.BadRequest();
		}
		// put try/catch around this instead of internal to service
		var response = await chatHistory.UpsertChatHistorySessionAsync(chatHistorySession);

		return TypedResults.Ok(response);
	}
}
