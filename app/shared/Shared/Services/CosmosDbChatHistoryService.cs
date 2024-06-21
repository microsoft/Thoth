// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.SemanticKernel.ChatCompletion;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

public class CosmosDbChatHistoryService : IChatHistoryService
{
    private readonly Container _chatContainer;

    /// <summary>
    /// Creates a new instance of the service.
    /// </summary>
    /// <param name="endpoint">Endpoint URI.</param>
    /// <param name="key">Account key.</param>
    /// <param name="databaseName">Name of the database to access.</param>
    /// <param name="chatContainerName">Name of the chat container to access.</param>
    /// <exception cref="ArgumentNullException">Thrown when endpoint, key, databaseName, cacheContainername or chatContainerName is either null or empty.</exception>
    /// <remarks>
    /// This constructor will validate credentials and create a service client instance.
    /// </remarks>
    public CosmosDbChatHistoryService(string endpoint, string key, string databaseName, string chatContainerName)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNullOrEmpty(databaseName);
        ArgumentNullException.ThrowIfNullOrEmpty(chatContainerName);

        CosmosSerializationOptions options = new()
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        };

        CosmosClient client = new CosmosClientBuilder(endpoint, key)
            .WithSerializerOptions(options)
            .Build()!;

        Database database = client.GetDatabase(databaseName)!;
        Container chatContainer = database.GetContainer(chatContainerName)!;

        _chatContainer = chatContainer ??
            throw new ArgumentException("Unable to connect to existing Azure Cosmos DB container or database: Chat Container.");
    }

    public async Task AddChatHistorySession(ChatHistorySession chatHistory)
    {
        PartitionKey partitionKey = new(chatHistory.SessionId);
        await _chatContainer.CreateItemAsync<ChatHistorySession>(
            item: chatHistory,
            partitionKey: partitionKey
        );
    }

    public void DeleteChatHistorySession(int sessionId)
    {
        PartitionKey partitionKey = new(sessionId);

        QueryDefinition query = new QueryDefinition("SELECT VALUE c.id FROM c WHERE c.sessionId = @sessionId")
                .WithParameter("@sessionId", sessionId);

        FeedIterator<string> response = _chatContainer.GetItemQueryIterator<string>(query);

        TransactionalBatch batch = _chatContainer.CreateTransactionalBatch(partitionKey);
        while (response.HasMoreResults)
        {
            FeedResponse<string> results = await response.ReadNextAsync();
            foreach (var itemId in results)
            {
                batch.DeleteItem(
                    id: itemId
                );
            }
        }
        await batch.ExecuteAsync();
    }

    public ChatHistorySession GetChatHistorySession(int sessionId)
    {
        QueryDefinition query = new QueryDefinition("SELECT DISTINCT * FROM c WHERE c.sessionId = @sessionId")
            .WithParameter("@sessionId", sessionId);

        FeedIterator<ChatHistorySession> response = _chatContainer.GetItemQueryIterator<ChatHistorySession>(query);

        List<Session> output = new();
        while (response.HasMoreResults)
        {
            FeedResponse<Session> results = await response.ReadNextAsync();
            output.AddRange(results);
        }
        return output;
    }

    public async Task<IEnumerable<ChatHistorySession>> GetChatHistorySessions()
    {
        QueryDefinition query = new QueryDefinition("SELECT DISTINCT * FROM c");

        FeedIterator<ChatHistorySession> response = _chatContainer.GetItemQueryIterator<ChatHistorySession>(query);

        List<ChatHistorySession> output = new();
        while (response.HasMoreResults)
        {
            FeedResponse<ChatHistorySession> results = await response.ReadNextAsync();
            output.AddRange(results);
        }
        return output;
    }
}
