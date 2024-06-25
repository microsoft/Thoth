// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Services;

public interface IPdfViewer
{
    ValueTask ShowDocumentAsync(string name, string baseUrl);
}
