// Copyright (c) Microsoft. All rights reserved.

using ClientApp.Services;

namespace ClientApp.Components;

public sealed partial class Examples
{
    [Inject]
	public ApiClient ApiClient { get; set; }
    [Parameter, EditorRequired] public required string Message { get; set; }
    [Parameter, EditorRequired] public EventCallback<string> OnExampleClicked { get; set; }

    public PinnedQuery[] PinnedQueries { get; set; } = [];

    protected async override Task OnInitializedAsync()
    {
		var items = await ApiClient.GetPinnedQueriesAsync();
        PinnedQueries = items.ToArray();
    }

    private async void OnChangeHandlerAsync()
    {
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnClickedAsync(string exampleText)
    {
        if (OnExampleClicked.HasDelegate)
        {
            await OnExampleClicked.InvokeAsync(exampleText);
        }
    }
}
