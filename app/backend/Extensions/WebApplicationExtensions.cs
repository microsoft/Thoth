// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapApi(this WebApplication app)
    {
        var api = app.MapGroup("api");

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
		HttpContext context,
		[FromServices] ILogger<IChatHistoryService> logger,
        [FromServices] IChatHistoryService chatHistory,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Get chat history sessions");

		var username = context.GetUserName();

		try
		{
			var sessions = chatHistory.GetChatHistorySessionsAsync(username);
			List<ChatSessionListResponse> result = [];
			await foreach (var item in sessions)
			{
				var lastUpdated = item.ChatHistory.OrderByDescending(h => h.Question.AskedOn).FirstOrDefault()?.Question.AskedOn;
				var chatSession = new ChatSessionListResponse(
					item.Id,
					item.UserId,
					item.Title,
					lastUpdated ?? DateTime.Now
				);
				result.Add(chatSession);
			}

			return TypedResults.Ok(result);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error fetching chat history sessions.");
			return Results.BadRequest(ex.Message);
		}
    }

    private static async Task<IResult> OnGetChatSessionAsync(
		HttpContext context,
        [FromRoute] string sessionId,
        [FromServices] ILogger<IChatHistoryService> logger,
        [FromServices] IChatHistoryService chatHistory,
        CancellationToken cancellationToken)
    {
        logger.LogInformation($"Get chat history for session with id: {sessionId}");

		var username = context.GetUserName();
		
		try
		{
			var response = await chatHistory.GetChatHistorySessionAsync(sessionId);
			// if user id does not match fetched history, return 404
			if (!response.UserId.Equals(username))
				return Results.NotFound();

			return TypedResults.Ok(response);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error fetching chat history session.");
			return Results.BadRequest(ex.Message);
		}
    }

	private static async Task<IResult> OnPostChatSessionAsync(
		[FromRoute] string sessionId,
		HttpContext context,
		ChatHistorySession chatHistorySession,
		[FromServices] ILogger<IChatHistoryService> logger,
		[FromServices] IChatHistoryService chatHistory,
		CancellationToken cancellationToken)
	{
		logger.LogInformation($"Add or update chat history with id: {sessionId}");

		var username = context.GetUserName();

		// Make sure sessionId from route matches sessionId in the body
		if (!sessionId.Equals(chatHistorySession.Id, StringComparison.InvariantCultureIgnoreCase))
		{
			return Results.BadRequest();
		}

		// make sure the user is authorized to update the chat history
		if (!chatHistorySession.UserId.Equals(username))
		{
			return Results.Unauthorized();
		}

		try
		{
			var response = await chatHistory.UpsertChatHistorySessionAsync(chatHistorySession);
			return TypedResults.Ok(response);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "An error occurred while saving chat history");
			return Results.BadRequest(ex.Message);
		}
	}

	private static string GetUserName(this HttpContext context)
	{
		var header = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
		var userName = !string.IsNullOrEmpty(header) ? header.ToString() : "localuser";
		return userName;
	}
}
