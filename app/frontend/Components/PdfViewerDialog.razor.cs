// Copyright (c) Microsoft. All rights reserved.

using System.Security.Policy;
using Microsoft.JSInterop;

namespace ClientApp.Components;

public sealed partial class PdfViewerDialog
{
    private readonly IJSRuntime _runtime;

    public PdfViewerDialog(IJSRuntime jsRuntime)
    {
        _runtime = jsRuntime;
    }

    private bool _isLoading = true;
    private string _pdfViewerVisibilityStyle => _isLoading ? "display:none;" : "display:default;";

    [Parameter] public required string FileName { get; set; }
    [Parameter] public required string BaseUrl { get; set; }

    [CascadingParameter] public required MudDialogInstance Dialog { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await JavaScriptModule.RegisterIFrameLoadedAsync(
            "#pdf-viewer",
            () =>
            {
                _isLoading = false;
                StateHasChanged();
            });
    }


    private async Task SendToEmailAsync()
    {
        // does filename need to be encoded?
        await _runtime.InvokeVoidAsync("open", "navigate", $"mailto:?subject={FileName}&amp;body=Check out this document {BaseUrl}.");
    }

    private async Task SendToTeamsAsync()
    {
       await _runtime.InvokeVoidAsync("sendToTeams", FileName, BaseUrl);
    }

    private void OnCloseClick() => Dialog.Close(DialogResult.Ok(true));
}
