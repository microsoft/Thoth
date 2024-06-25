// Copyright (c) Microsoft. All rights reserved.

using Azure.AI.OpenAI;

var host = new HostBuilder()
    .ConfigureServices(services =>
    {
        var credential = new DefaultAzureCredential();

        static Uri GetUriFromEnvironment(string variable) => Environment.GetEnvironmentVariable(variable) is string value &&
                Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
                uri is not null
                ? uri
                : throw new ArgumentException(
                $"Unable to parse URI from environment variable: {variable}");

        services.AddAzureClients(builder =>
        {
            builder.AddDocumentIntelligenceClient(
                GetUriFromEnvironment("AZURE_DOCUMENT_INTELLIGENCE_SERVICE_ENDPOINT"));
        });

        services.AddSingleton<SearchClient>(_ =>
        {
            return new SearchClient(
                GetUriFromEnvironment("AZURE_SEARCH_SERVICE_ENDPOINT"),
                Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX"),
                credential);
        });

        services.AddSingleton<SearchIndexClient>(_ =>
        {
            return new SearchIndexClient(
                GetUriFromEnvironment("AZURE_SEARCH_SERVICE_ENDPOINT"),
                credential);
        });

        services.AddSingleton<BlobContainerClient>(_ =>
        {
            var blobServiceClient = new BlobServiceClient(
                GetUriFromEnvironment("AZURE_STORAGE_BLOB_ENDPOINT"),
                credential);

            var containerClient = blobServiceClient.GetBlobContainerClient("corpus");

            containerClient.CreateIfNotExists();

            return containerClient;
        });

        services.AddSingleton<BlobServiceClient>(_ =>
        {
            return new BlobServiceClient(
                GetUriFromEnvironment("AZURE_STORAGE_BLOB_ENDPOINT"), credential);
        });

        services.AddSingleton<EmbedServiceFactory>();
        services.AddSingleton<EmbeddingAggregateService>();

        services.AddSingleton<IEmbedService, AzureSearchEmbedService>(provider =>
        {
            var searchIndexName = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX") ?? throw new ArgumentNullException("AZURE_SEARCH_INDEX is null");
            var useAOAI = Environment.GetEnvironmentVariable("USE_AOAI")?.ToLower() == "true";
            var useVision = Environment.GetEnvironmentVariable("USE_VISION")?.ToLower() == "true";

            OpenAIClient? openAIClient = null;
            string? embeddingModelName = null;

            if (useAOAI)
            {
                var openaiEndPoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new ArgumentNullException("AZURE_OPENAI_ENDPOINT is null");
                embeddingModelName = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new ArgumentNullException("AZURE_OPENAI_EMBEDDING_DEPLOYMENT is null");
                openAIClient = new OpenAIClient(new Uri(openaiEndPoint), new DefaultAzureCredential());
            }
            else
            {
                embeddingModelName = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_DEPLOYMENT") ?? throw new ArgumentNullException("OPENAI_EMBEDDING_DEPLOYMENT is null");
                var openaiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new ArgumentNullException("OPENAI_API_KEY is null");
                openAIClient = new OpenAIClient(openaiKey);
            }

            var searchClient = provider.GetRequiredService<SearchClient>();
            var searchIndexClient = provider.GetRequiredService<SearchIndexClient>();
            var corpusContainer = provider.GetRequiredService<BlobContainerClient>();
            var documentClient = provider.GetRequiredService<DocumentIntelligenceClient>();
            var logger = provider.GetRequiredService<ILogger<AzureSearchEmbedService>>();


            return new AzureSearchEmbedService(
            openAIClient: openAIClient,
            embeddingModelName: embeddingModelName,
            searchClient: searchClient,
            searchIndexName: searchIndexName,
            searchIndexClient: searchIndexClient,
            documentIntelligenceClient: documentClient,
            corpusContainerClient: corpusContainer,
            logger: logger);
        });
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();

host.Run();
