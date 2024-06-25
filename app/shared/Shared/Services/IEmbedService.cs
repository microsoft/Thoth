// Copyright (c) Microsoft. All rights reserved.

public interface IEmbedService
{
    /// <summary>
    /// Embeds the given pdf blob into the embedding service.
    /// </summary>
    /// <param name="blobStream">The stream from the blob to embed.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <returns>
    /// An asynchronous operation that yields <c>true</c>
    /// when successfully embedded, otherwise <c>false</c>.
    /// </returns>
    Task<bool> EmbedDocumentBlobAsync(
        Stream blobStream,
        string blobName);    

    Task CreateSearchIndexAsync(string searchIndexName, CancellationToken ct = default);

    Task EnsureSearchIndexAsync(string searchIndexName, CancellationToken ct = default);
}
