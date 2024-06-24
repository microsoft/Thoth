// Copyright (c) Microsoft. All rights reserved.

namespace PrepareDocs;

internal record class AppOptions(
    string? Category,
    bool SkipBlobs,
    string? StorageServiceBlobEndpoint,
    string? Container,
    string? TenantId,
    string? SearchServiceEndpoint,
    string? AzureOpenAIServiceEndpoint,
    string? SearchIndexName,
    string? EmbeddingModelName,
    bool Remove,
    bool RemoveAll,
    string? FormRecognizerServiceEndpoint,
    string? ComputerVisionServiceEndpoint,
    bool Verbose,
    IConsole Console,
    int BatchSize = 25,
    int WaitTime = 30) : AppConsole(Console);

internal record class AppConsole(IConsole Console);
