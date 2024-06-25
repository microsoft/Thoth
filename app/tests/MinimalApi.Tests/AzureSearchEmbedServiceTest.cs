// Copyright (c) Microsoft. All rights reserved.

using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using FluentAssertions;

namespace MinimalApi.Tests;
public class AzureSearchEmbedServiceTest
{
    [EnvironmentVariablesFact(
        "AZURE_SEARCH_SERVICE_ENDPOINT",
        "AZURE_OPENAI_ENDPOINT",
        "AZURE_OPENAI_EMBEDDING_DEPLOYMENT",
        "AZURE_STORAGE_BLOB_ENDPOINT")]
    public async Task EnsureSearchIndexWithoutImageEmbeddingsAsync()
    {
        var indexName = nameof(EnsureSearchIndexWithoutImageEmbeddingsAsync).ToLower();
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException();
        var embeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new InvalidOperationException();
        var azureSearchEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobEndpoint = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_ENDPOINT") ?? throw new InvalidOperationException();
        var blobContainer = "test";

        var azureCredential = new DefaultAzureCredential();
        var openAIClient = new OpenAIClient(new Uri(openAIEndpoint), azureCredential);
        var searchClient = new SearchClient(new Uri(azureSearchEndpoint), indexName, azureCredential);
        var searchIndexClient = new SearchIndexClient(new Uri(azureSearchEndpoint), azureCredential);
        var documentAnalysisClient = new DocumentAnalysisClient(new Uri(azureSearchEndpoint), azureCredential);
        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), azureCredential);

        var service = new AzureSearchEmbedService(
            openAIClient: openAIClient,
            embeddingModelName: embeddingDeployment,
            searchClient: searchClient,
            searchIndexName: indexName,
            searchIndexClient: searchIndexClient,
            documentAnalysisClient: documentAnalysisClient,
            corpusContainerClient: blobServiceClient.GetBlobContainerClient(blobContainer),
            logger: null);

        try
        {
            // check if index exists
            var existsAction = async () => await searchIndexClient.GetIndexAsync(indexName);
            await existsAction.Should().ThrowAsync<RequestFailedException>();
            await service.EnsureSearchIndexAsync(indexName);

            var response = await searchIndexClient.GetIndexAsync(indexName);
            var index = response.Value;
            index.Name.Should().Be(indexName);
            index.Fields.Count.Should().Be(6);
            index.Fields.Select(f => f.Name).Should().BeEquivalentTo(["id", "content", "category", "sourcepage", "sourcefile", "embedding"]);

            // embedding's dimension should be 1536
            var embeddingField = index.Fields.Single(f => f.Name == "embedding");
            embeddingField.IsSearchable.Should().BeTrue();
            embeddingField.VectorSearchDimensions.Should().Be(1536);
        }
        finally
        {
            await searchIndexClient.DeleteIndexAsync(indexName);
        }
    }   

    [EnvironmentVariablesFact(
        "AZURE_SEARCH_SERVICE_ENDPOINT",
        "AZURE_OPENAI_ENDPOINT",
        "AZURE_OPENAI_EMBEDDING_DEPLOYMENT",
        "AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT",
        "AZURE_STORAGE_BLOB_ENDPOINT")]
    public async Task GetDocumentTextTestAsync()
    {
        var indexName = nameof(GetDocumentTextTestAsync).ToLower();
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException();
        var embeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new InvalidOperationException();
        var azureSearchEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobEndpoint = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_ENDPOINT") ?? throw new InvalidOperationException();
        var azureFormRecognizerEndpoint = Environment.GetEnvironmentVariable("AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobContainer = "test";

        var azureCredential = new DefaultAzureCredential();
        var openAIClient = new OpenAIClient(new Uri(openAIEndpoint), azureCredential);
        var searchClient = new SearchClient(new Uri(azureSearchEndpoint), indexName, azureCredential);
        var searchIndexClient = new SearchIndexClient(new Uri(azureSearchEndpoint), azureCredential);
        var documentAnalysisClient = new DocumentAnalysisClient(new Uri(azureFormRecognizerEndpoint), azureCredential);
        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), azureCredential);

        var service = new AzureSearchEmbedService(
            openAIClient: openAIClient,
            embeddingModelName: embeddingDeployment,
            searchClient: searchClient,
            searchIndexName: indexName,
            searchIndexClient: searchIndexClient,
            documentAnalysisClient: documentAnalysisClient,
            corpusContainerClient: blobServiceClient.GetBlobContainerClient(blobContainer),
            logger: null);

        try
        {
            await service.EnsureSearchIndexAsync(indexName);
            var benefitOptionsPDFName = "Benefit_Options.pdf";
            var benefitOptionsPDFPath = Path.Combine("data", benefitOptionsPDFName);
            using var stream = File.OpenRead(benefitOptionsPDFPath);
            var pages = await service.GetDocumentTextAsync(stream, benefitOptionsPDFName);
            pages.Count.Should().Be(4);
        }
        finally
        {
            await searchIndexClient.DeleteIndexAsync(indexName);
        }
    }

    [EnvironmentVariablesFact(
        "AZURE_SEARCH_SERVICE_ENDPOINT",
        "AZURE_OPENAI_ENDPOINT",
        "AZURE_OPENAI_EMBEDDING_DEPLOYMENT",
        "AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT",
        "AZURE_STORAGE_BLOB_ENDPOINT")]
    public async Task EmbedBlobWithoutImageEmbeddingTestAsync()
    {
        var indexName = nameof(EmbedBlobWithoutImageEmbeddingTestAsync).ToLower();
        var openAIEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException();
        var embeddingDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new InvalidOperationException();
        var azureSearchEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobEndpoint = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_ENDPOINT") ?? throw new InvalidOperationException();
        var azureFormRecognizerEndpoint = Environment.GetEnvironmentVariable("AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT") ?? throw new InvalidOperationException();
        var blobContainer = nameof(EmbedBlobWithoutImageEmbeddingTestAsync).ToLower();

        var azureCredential = new DefaultAzureCredential();
        var openAIClient = new OpenAIClient(new Uri(openAIEndpoint), azureCredential);
        var searchClient = new SearchClient(new Uri(azureSearchEndpoint), indexName, azureCredential);
        var searchIndexClient = new SearchIndexClient(new Uri(azureSearchEndpoint), azureCredential);
        var documentAnalysisClient = new DocumentAnalysisClient(new Uri(azureFormRecognizerEndpoint), azureCredential);
        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), azureCredential);
        var containerClient = blobServiceClient.GetBlobContainerClient(blobContainer);
        await containerClient.CreateIfNotExistsAsync();

        var service = new AzureSearchEmbedService(
            openAIClient: openAIClient,
            embeddingModelName: embeddingDeployment,
            searchClient: searchClient,
            searchIndexName: indexName,
            searchIndexClient: searchIndexClient,
            documentAnalysisClient: documentAnalysisClient,
            corpusContainerClient: containerClient,
            logger: null);

        try
        {
            await service.EnsureSearchIndexAsync(indexName);
            var benefitOptionsPDFName = "Benefit_Options.pdf";
            var benefitOptionsPDFPath = Path.Combine("data", benefitOptionsPDFName);
            using var stream = File.OpenRead(benefitOptionsPDFPath);
            var isSucceed = await service.EmbedPDFBlobAsync(stream, benefitOptionsPDFName);
            isSucceed.Should().BeTrue();

            // check if the document page is uploaded to blob
            var blobs = containerClient.GetBlobsAsync();
            var blobNames = await blobs.AsPages().

            List<string> blobNames = [];
            await foreach(var blob in blobs)
            {
                blobNames.Add(blob.Name);
            }
            
            var blobNames = blobs.Select(b => b.Name).ToListAsync();
            blobNames.Result.Count.Should().Be(4);
            blobNames.Result.Should().BeEquivalentTo([ "Benefit_Options-0.txt", "Benefit_Options-1.txt", "Benefit_Options-2.txt", "Benefit_Options-3.txt" ]);
        }
        finally
        {
            // clean up
            await searchIndexClient.DeleteIndexAsync(indexName);
            await blobServiceClient.DeleteBlobContainerAsync(blobContainer);
        }
    }
    
}
