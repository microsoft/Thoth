// Copyright (c) Microsoft. All rights reserved.

using SharedWebComponents.Services;

namespace SharedWebComponents.Components;

public sealed partial class Examples
{
    [Inject]
    public PinnedQueriesService PinnedQueriesService { get; set; }
    [Parameter, EditorRequired] public required string Message { get; set; }
    [Parameter, EditorRequired] public EventCallback<string> OnExampleClicked { get; set; }

    public UserQuestion[] PinnedQueries { get; set; } = new UserQuestion[3];

    protected override void OnInitialized()
    {
        PinnedQueriesService.OnChange += OnChangeHandlerAsync;
        PinnedQueries = PinnedQueriesService.GetPinnedQueries().ToArray();
    }

    public void Dispose()
    {
        PinnedQueriesService.OnChange -= OnChangeHandlerAsync;
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
