// Copyright (c) Microsoft. All rights reserved.

namespace EmbedFunctions.Services;

public sealed class EmbeddingAggregateService(
    EmbedServiceFactory embedServiceFactory,
    BlobContainerClient corpusClient,
    ILogger<EmbeddingAggregateService> logger)
{
    internal async Task EmbedBlobAsync(Stream blobStream, string blobName)
    {
        try
        {
            var embeddingType = GetEmbeddingType();
            var embedService = embedServiceFactory.GetEmbedService(embeddingType);

            if (Path.GetExtension(blobName) is ".pdf" or ".docx" or ".pptx" or ".xlsx" or "html")
            {
                logger.LogInformation($"Embedding {Path.GetExtension(blobName)}: {blobName}");
                var result = await embedService.EmbedDocumentBlobAsync(blobStream, blobName);

                var status = result switch
                {
                    true => DocumentProcessingStatus.Succeeded,
                    _ => DocumentProcessingStatus.Failed
                };

                await corpusClient.SetMetadataAsync(new Dictionary<string, string>
                {
                    [nameof(DocumentProcessingStatus)] = status.ToString(),
                    [nameof(EmbeddingType)] = embeddingType.ToString()
                });
            }
            else
            {
                throw new NotSupportedException("Unsupported file type.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to embed: {Name}, error: {Message}", blobName, ex.Message);
            throw;
        }
    }

    private static EmbeddingType GetEmbeddingType() => Environment.GetEnvironmentVariable("EMBEDDING_TYPE") is string type &&
            Enum.TryParse<EmbeddingType>(type, out EmbeddingType embeddingType)
            ? embeddingType
            : EmbeddingType.AzureSearch;
}
