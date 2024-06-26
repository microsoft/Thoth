// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Cosmos;
using Container = Microsoft.Azure.Cosmos.Container;
using Shared.Models;
using System.Net;

namespace MinimalApi.Services;

public class CosmosChatHistoryService(Container container) : IChatHistoryService
{
	private readonly Container _container = container;

	public async Task<ChatHistorySession> UpsertChatHistorySessionAsync(ChatHistorySession chatHistory)
	{
		var response = await _container.UpsertItemAsync<ChatHistorySession>(chatHistory);

		if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK)
			return response.Resource;

		throw new CosmosChatHistoryServiceException($"Error upserting history item in CosmosClient. Status Code: {response.StatusCode}");
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
			if (cex.StatusCode == System.Net.HttpStatusCode.NotFound)
				return new ChatHistorySession(sessionId, "", "", 0, []);

			throw new CosmosChatHistoryServiceException("Error fetching history item from CosmosClient", cex);
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
			foreach (var session in await feed.ReadNextAsync())
			{
				yield return session;
			}
		}
	}
}

public class CosmosChatHistoryServiceException : Exception
{
	public CosmosChatHistoryServiceException() : base() { }
	public CosmosChatHistoryServiceException(string message) : base(message) { }
	public CosmosChatHistoryServiceException(string message, Exception innerException) : base(message, innerException) { }
}
