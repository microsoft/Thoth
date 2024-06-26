// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Cosmos;

namespace MinimalApi.Services;

public class PinnedQueryService(Container container)
{
	private readonly Container _container = container;

	public async IAsyncEnumerable<PinnedQuery> GetPinnedQueriesAsync(string userId)
	{
		var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
			.WithParameter("@userId", userId);

		using FeedIterator<PinnedQuery> feed = _container.GetItemQueryIterator<PinnedQuery>(query);
		while (feed.HasMoreResults)
		{
			foreach (var item in await feed.ReadNextAsync())
			{
				yield return item;
			}
		}
	}

	public async Task<PinnedQuery> GetPinnedQueryAsync(string id) => await _container.ReadItemAsync<PinnedQuery>(id.ToString(), new PartitionKey(id));

	public async Task<PinnedQuery> AddPinnedQueryAsync(PinnedQuery pinnedQuery)
	{
		var response = await _container.CreateItemAsync(pinnedQuery, new PartitionKey(pinnedQuery.Id));
		return response.Resource;
	}

	public async Task DeletePinnedQueryAsync(string id) => await _container.DeleteItemAsync<PinnedQuery>(id.ToString(), new PartitionKey(id));
}
