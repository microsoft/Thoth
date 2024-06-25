// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Cosmos;
using Container = Microsoft.Azure.Cosmos.Container;
using Microsoft.SemanticKernel.ChatCompletion;

public class ChatHistoryService(CosmosClient dbClient) : IChatHistoryService
{
    private static string s_collection = "chathistory";
	private static string s_databaseName = "chatdb";
	private readonly CosmosClient _dbClient = dbClient;

	public async Task AddChatHistorySessionAsync(ChatHistorySession chatHistory)
    {
		var container = await GetContainerAsync();
		await container.CreateItemAsync(chatHistory);
    }

    public async Task DeleteChatHistorySessionAsync(string sessionId)
    {
		var container = await GetContainerAsync();
		await container.DeleteItemAsync<ChatHistorySession>(sessionId, new PartitionKey(sessionId));
    }

    public async Task<ChatHistorySession> GetChatHistorySessionAsync(string sessionId)
    {
		var container = await GetContainerAsync();
		try
		{
			var item = await container.ReadItemAsync<ChatHistorySession>(sessionId, new PartitionKey(sessionId));
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
		var container = await GetContainerAsync();
		var query = new QueryDefinition(
			query: $"SELECT * FROM {s_collection} c WHERE c.UserId = @userid"
		)
		.WithParameter("@userid", userId);

		using FeedIterator<ChatHistorySession> feed = container.GetItemQueryIterator<ChatHistorySession>(query);
		while (feed.HasMoreResults)
		{
			foreach(var session in await feed.ReadNextAsync())
			{
				yield return session;
			}
		}
    }

    private async Task EnsureCollectionAsync()
    {
		await _dbClient.CreateDatabaseIfNotExistsAsync(s_databaseName);
		var database = _dbClient.GetDatabase(s_databaseName);
		await database.CreateContainerIfNotExistsAsync(s_collection, "/Id");
    }

	private async Task<Container> GetContainerAsync()
	{
		await EnsureCollectionAsync();
		return _dbClient.GetDatabase(s_databaseName).GetContainer(s_collection);
	}
}
