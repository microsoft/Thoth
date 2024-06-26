// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Services;

public sealed class ApiClient(HttpClient httpClient)
{
	public async Task<bool> ShowLogoutButtonAsync()
	{
		var response = await httpClient.GetAsync("api/enableLogout");
		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<bool>();
	}

	public async IAsyncEnumerable<DocumentResponse> GetDocumentsAsync(
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var response = await httpClient.GetAsync("api/documents", cancellationToken);

		if (response.IsSuccessStatusCode)
		{
			var options = SerializerOptions.Default;

			using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

			await foreach (var document in
				JsonSerializer.DeserializeAsyncEnumerable<DocumentResponse>(stream, options, cancellationToken))
			{
				if (document is null)
				{
					continue;
				}

				yield return document;
			}
		}
	}

	public Task<AnswerResult<ChatRequest>> ChatConversationAsync(ChatRequest request) => PostRequestAsync(request, "api/chat");

	private async Task<AnswerResult<TRequest>> PostRequestAsync<TRequest>(
		TRequest request, string apiRoute) where TRequest : ApproachRequest
	{
		var result = new AnswerResult<TRequest>(
			IsSuccessful: false,
			Response: null,
			Approach: request.Approach,
			Request: request);

		var json = JsonSerializer.Serialize(
			request,
			SerializerOptions.Default);

		using var body = new StringContent(
			json, Encoding.UTF8, "application/json");

		var response = await httpClient.PostAsync(apiRoute, body);

		if (response.IsSuccessStatusCode)
		{
			var answer = await response.Content.ReadFromJsonAsync<ChatAppResponseOrError>();
			return result with
			{
				IsSuccessful = answer is not null,
				Response = answer,
			};
		}
		else
		{
			var errorTitle = $"HTTP {(int)response.StatusCode} : {response.ReasonPhrase ?? "☹️ Unknown error..."}";
			var answer = new ChatAppResponseOrError(
				Array.Empty<ResponseChoice>(),
				errorTitle);

			return result with
			{
				IsSuccessful = false,
				Response = answer
			};
		}
	}

	public async Task<ChatHistorySession> UpsertChatHistorySessionAsync(ChatHistorySession chatHistorySession)
	{
		chatHistorySession.Id = string.IsNullOrWhiteSpace(chatHistorySession.Id) ? Guid.NewGuid().ToString() : chatHistorySession.Id;

		var response = await httpClient.PostAsJsonAsync($"api/chatsessions/{chatHistorySession.Id}", chatHistorySession);

		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<ChatHistorySession>();
	}

	public async Task<ChatHistorySession?> GetChatHistorySessionAsync(string sessionId)
	{
		var response = await httpClient.GetAsync($"api/chatsessions/{sessionId}");

		try
		{
			return await response.Content.ReadFromJsonAsync<ChatHistorySession>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error: {ex.Message}");
			return null;
		}
	}

	public async Task<IEnumerable<ChatSessionListResponse>> GetChatHistorySessionsAsync()
	{
		var response = await httpClient.GetAsync("api/chatsessions");

		return await response.Content.ReadFromJsonAsync<IEnumerable<ChatSessionListResponse>>();
	}

	public async Task DeleteChatHistorySessionAsync(string sessionId)
	{
		await httpClient.DeleteAsync($"api/chatsessions/{sessionId}");
	}
}
