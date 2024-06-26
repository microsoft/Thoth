﻿// Copyright (c) Microsoft. All rights reserved.
using Azure.Core;
using Microsoft.Azure.Cosmos;

namespace MinimalApi.Extensions;

internal static class ServiceCollectionExtensions
{
    private static readonly DefaultAzureCredential s_azureCredential = new();	

    internal static IServiceCollection AddAzureServices(this IServiceCollection services)
    {
        services.AddSingleton<BlobServiceClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageAccountEndpoint = config["AZURE_STORAGE_BLOB_ENDPOINT"];
            ArgumentNullException.ThrowIfNullOrEmpty(azureStorageAccountEndpoint);

            var blobServiceClient = new BlobServiceClient(
                new Uri(azureStorageAccountEndpoint), s_azureCredential);

            return blobServiceClient;
        });

        services.AddSingleton<BlobContainerClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureStorageContainer = config["AZURE_STORAGE_CONTAINER"];
            return sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(azureStorageContainer);
        });

        services.AddSingleton<ISearchService, AzureSearchService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureSearchServiceEndpoint = config["AZURE_SEARCH_SERVICE_ENDPOINT"];
            ArgumentNullException.ThrowIfNullOrEmpty(azureSearchServiceEndpoint);

            var azureSearchIndex = config["AZURE_SEARCH_INDEX"];
            ArgumentNullException.ThrowIfNullOrEmpty(azureSearchIndex);

            var searchClient = new SearchClient(
                               new Uri(azureSearchServiceEndpoint), azureSearchIndex, s_azureCredential);

            return new AzureSearchService(searchClient);
        });

        services.AddSingleton<DocumentAnalysisClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var azureOpenAiServiceEndpoint = config["AZURE_OPENAI_ENDPOINT"] ?? throw new ArgumentNullException();

            var documentAnalysisClient = new DocumentAnalysisClient(
                new Uri(azureOpenAiServiceEndpoint), s_azureCredential);
            return documentAnalysisClient;
        });

        services.AddSingleton<OpenAIClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var useAOAI = config["USE_AOAI"] == "true";
            if (useAOAI)
            {
                var azureOpenAiServiceEndpoint = config["AZURE_OPENAI_ENDPOINT"];
                ArgumentNullException.ThrowIfNullOrEmpty(azureOpenAiServiceEndpoint);

                var openAIClient = new OpenAIClient(new Uri(azureOpenAiServiceEndpoint), s_azureCredential);

                return openAIClient;
            }
            else
            {
                var openAIApiKey = config["OpenAIApiKey"];
                ArgumentNullException.ThrowIfNullOrEmpty(openAIApiKey);

                var openAIClient = new OpenAIClient(openAIApiKey);
                return openAIClient;
            }
        });
		services.AddSingleton<CosmosClient>(sp =>
		{
			var config = sp.GetRequiredService<IConfiguration>();
			CosmosClientOptions options = new CosmosClientOptions
			{
				SerializerOptions = new CosmosSerializationOptions()
				{
					PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
				}
			};
			return new CosmosClient(config["COSMOS_HISTORY_ENDPOINT"], s_azureCredential, options);
		});
        services.AddSingleton<AzureBlobStorageService>();
        services.AddSingleton<IChatHistoryService>(sp =>
        {
			CosmosClient cosmosClient = sp.GetRequiredService<CosmosClient>();
			return new CosmosChatHistoryService(cosmosClient.GetDatabase("chatdb").GetContainer("chathistory"));
        });
		services.AddSingleton<PinnedQueryService>(sp =>
		{
			CosmosClient cosmosClient = sp.GetRequiredService<CosmosClient>();
			return new PinnedQueryService(cosmosClient.GetDatabase("chatdb").GetContainer("pinnedqueries"));
		});
        services.AddSingleton<ReadRetrieveReadChatService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var openAIClient = sp.GetRequiredService<OpenAIClient>();
            var searchClient = sp.GetRequiredService<ISearchService>();
            return new ReadRetrieveReadChatService(searchClient, openAIClient, config, tokenCredential: s_azureCredential);
        });

        return services;
    }

    internal static IServiceCollection AddCrossOriginResourceSharing(this IServiceCollection services)
    {
        services.AddCors(
            options =>
                options.AddDefaultPolicy(
                    policy =>
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod()));

        return services;
    }
}
