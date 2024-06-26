﻿// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Shared;

public sealed partial class MainLayout
{
    [Inject]
    private NavigationManager _navigationManager { get; set; }
    [Inject]
    private ApiClient _apiClient{ get; set; }
    private readonly MudTheme _theme = new();
    private bool _drawerOpen = true;
    private bool _chatHistoryDrawer = false;
    private bool _settingsOpen = false;
    private SettingsPanel? _settingsPanel;
    private MudListItem? _selectedItem = null;

	public IEnumerable<ChatHistorySessionUI> ChatHistorySessions { get; set; } = [];


	private bool _isDarkTheme
    {
        get => LocalStorage.GetItem<bool>(StorageKeys.PrefersDarkTheme);
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersDarkTheme, value);
    }

    private bool _isReversed
    {
        get => LocalStorage.GetItem<bool?>(StorageKeys.PrefersReversedConversationSorting) ?? false;
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersReversedConversationSorting, value);
    }

    private bool _isRightToLeft =>
        Thread.CurrentThread.CurrentUICulture is { TextInfo.IsRightToLeft: true };

    [Inject] public required NavigationManager Nav { get; set; }
    [Inject] public required ILocalStorageService LocalStorage { get; set; }
    [Inject] public required IDialogService Dialog { get; set; }

    private bool SettingsDisabled => new Uri(Nav.Uri).Segments.LastOrDefault() switch
    {
        "ask" or "chat" => false,
        _ => true
    };

    private bool SortDisabled => new Uri(Nav.Uri).Segments.LastOrDefault() switch
    {
        "voicechat" or "chat" => false,
        _ => true
    };

    private void OnMenuClicked() => _drawerOpen = !_drawerOpen;

    private void OnThemeChanged() => _isDarkTheme = !_isDarkTheme;

    private void OnIsReversedChanged() => _isReversed = !_isReversed;

    private void OnChatHistoryClicked() => _chatHistoryDrawer = !_chatHistoryDrawer;

    protected void OnItemClick(EventArgs e, string chatId)
    {
        _navigationManager.NavigateTo($"/chat?{nameof(ChatHistorySessionUI.Id)}={chatId}");
    }

    protected async Task OnDeleteSessionClickAsync(EventArgs e, string chatId)
    {
        await _apiClient.DeleteChatHistorySessionAsync(chatId);
    }

	protected override async Task OnInitializedAsync() => _apiClient.OnChange += OnChangeHandlerAsync;

	public void Dispose() => _apiClient.OnChange -= OnChangeHandlerAsync;

	private async Task OnChangeHandlerAsync()
    {
        await InvokeAsync(StateHasChanged);
		ChatHistorySessions = await _apiClient.GetChatHistorySessionsAsync();
    }
}

public record Chat(string Name, string Id, DateTime TimeStamp);
