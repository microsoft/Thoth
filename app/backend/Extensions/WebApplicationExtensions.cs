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

		api.MapDelete("chatsessions/{sessionId}", OnDeleteChatSessionAsync);

		// Get pinned queries
		api.MapGet("pinnedqueries", OnGetPinnedQueriesAsync);

		// Add a pinned query
		api.MapPost("pinnedqueries", OnPostPinnedQueryAsync);

		// Remove pinned query by id
		api.MapDelete("pinnedqueries/{id}", OnDeletePinnedQueryAsync);

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
		try
		{
			if (request is { History.Length: > 0 })
			{
				var response = await chatService.ReplyAsync(
					request.History, request.Overrides, cancellationToken);

				return TypedResults.Ok(response);
			}

			return Results.BadRequest();
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			throw;
		}

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

	// CHAT HISTORY

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
		[FromServices] OpenAIClient openAIClient,
		[FromServices] IConfiguration config,
		CancellationToken cancellationToken)
	{
		logger.LogInformation($"Add or update chat history with id: {sessionId}");

		chatHistorySession.UserId = context.GetUserName();

		// Make sure sessionId from route matches sessionId in the body
		if (!sessionId.Equals(chatHistorySession.Id, StringComparison.InvariantCultureIgnoreCase))
		{
			return Results.BadRequest();
		}

		// make sure the user is authorized to update the chat history
		if (string.IsNullOrWhiteSpace(chatHistorySession.UserId))
		{
			return Results.Unauthorized();
		}

		// generate a 3 word summary to be the title
		if (string.IsNullOrWhiteSpace(chatHistorySession.Title))
		{
			var deploymentId = config["AZURE_OPENAI_CHATGPT_DEPLOYMENT"];

			var chatOptions = new ChatCompletionsOptions
			{
				DeploymentName = deploymentId,
				Messages =
					{
					new ChatRequestSystemMessage("""
                        Given the following chat session, generate a 3 word summary
                        """)
				}
			};

			var firstQuestionAsked = chatHistorySession.ChatHistory.FirstOrDefault(m => string.IsNullOrWhiteSpace(m.Response.Error))?.Question.Question;

			chatOptions.Messages.Add(new ChatRequestUserMessage(firstQuestionAsked));

			var response = await openAIClient.GetChatCompletionsAsync(chatOptions, cancellationToken);

			chatHistorySession.Title = response.Value.Choices[0].Message.Content;
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
	private static async Task<IResult> OnDeleteChatSessionAsync([FromRoute] string sessionId,
		HttpContext context,
		[FromServices] ILogger<IChatHistoryService> logger,
		[FromServices] IChatHistoryService chatHistory,
		CancellationToken cancellationToken)
	{
		// make sure the user is authorized to update the chat history
		if (string.IsNullOrWhiteSpace(context.GetUserName()))
			return Results.Unauthorized();

		await chatHistory.DeleteChatHistorySessionAsync(sessionId);

		return Results.NoContent();
	}

	// PINNED QUERIES
	private static async Task<IResult> OnGetPinnedQueriesAsync(
		HttpContext context,
		[FromServices] ILogger<PinnedQueryService> logger,
		[FromServices] PinnedQueryService service)
	{
		var username = context.GetUserName();
		var results = await service.GetPinnedQueriesAsync(username).ToListAsync();

		return TypedResults.Ok(results);
	}

	private static async Task<IResult> OnDeletePinnedQueryAsync(
		HttpContext context,
		[FromRoute] string id,
		[FromServices] ILogger<PinnedQueryService> logger,
		[FromServices] PinnedQueryService service)
	{
		var username = context.GetUserName();
		var queryToDelete = await service.GetPinnedQueryAsync(id);
		if (queryToDelete.UserId.Equals(username))
		{
			await service.DeletePinnedQueryAsync(id);
			return Results.NoContent();
		}
		else
		{
			return Results.NotFound();
		}
	}

	private static async Task<IResult> OnPostPinnedQueryAsync(
		HttpContext context,		
		PinnedQuery query,
		[FromServices] ILogger<PinnedQueryService> logger,
		[FromServices] PinnedQueryService service)
	{
		var username = context.GetUserName();
		if (string.IsNullOrEmpty(query.UserId))
		{
			query.UserId = username;
		}
		if (!username.Equals(query.UserId))
		{
			return Results.Unauthorized();
		}

		var response = await service.AddPinnedQueryAsync(query);
		return TypedResults.Created($"/api/pinnedqueries/{query.Id}", response);
	}

	private static string GetUserName(this HttpContext context)
	{
		var header = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"];
		var userName = !string.IsNullOrEmpty(header) ? header.ToString() : "localuser";
		return userName;
	}
}
