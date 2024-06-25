// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Cosmos;
using Container = Microsoft.Azure.Cosmos.Container;
using Microsoft.SemanticKernel.ChatCompletion;

public class ChatHistoryService(Container container) : IChatHistoryService
{	
	private readonly Container _container = container;

	public async Task<ChatHistorySession> UpsertChatHistorySessionAsync(ChatHistorySession chatHistory)
    {		
		var response = await _container.UpsertItemAsync<ChatHistorySession>(chatHistory);
		if (response.StatusCode == System.Net.HttpStatusCode.OK)
			return response.Resource;

		return new ChatHistorySession(chatHistory.Id, "", "", new ChatHistory("Failed"));
    }

    public async Task DeleteChatHistorySessionAsync(string sessionId)
    {
		await _container.DeleteItemAsync<ChatHistorySession>(sessionId, new PartitionKey(sessionId));
    }

    public async Task<ChatHistorySession> GetChatHistorySessionAsync(string sessionId)
    {
		try
		{
			var item = await _container.ReadItemAsync<ChatHistorySession>(sessionId, new PartitionKey(sessionId));
			return item.Resource;
		}
		catch (CosmosException cex)
		{
			var errorHistory = new ChatHistory(cex.Message);
			return new ChatHistorySession(sessionId, "", "", errorHistory);
		}
    }

    public async IAsyncEnumerable<ChatHistorySession> GetChatHistorySessionsAsync(string userId)
    {
		var query = new QueryDefinition(
			query: $"SELECT * FROM {_container.Id} c WHERE c.userId = @userid"
		)
		.WithParameter("@userid", userId);

		using FeedIterator<ChatHistorySession> feed = _container.GetItemQueryIterator<ChatHistorySession>(query);
		while (feed.HasMoreResults)
		{
			foreach(var session in await feed.ReadNextAsync())
			{
				yield return session;
			}
		}
    }    
}
