// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.WebUtilities;
using System;

namespace SharedWebComponents.Pages;

public sealed partial class Chat
{
    private string _userQuestion = "";
    private UserQuestion _currentQuestion;
    private string _lastReferenceQuestion = "";
    private bool _isReceivingResponse = false;

    private Dictionary<UserQuestion, ChatAppResponseOrError?> _questionAndAnswerMap = [];

    [Inject] public required ISessionStorageService SessionStorage { get; set; }
    [Inject] public required NavigationManager NavigationManager { get; set; }
    [Inject] public required PinnedQueriesService PinnedQueriesService { get; set; }

    [Inject] public required ApiClient ApiClient { get; set; }

    [CascadingParameter(Name = nameof(Settings))]
    public required RequestSettingsOverrides Settings { get; set; }

    [CascadingParameter(Name = nameof(IsReversed))]
    public required bool IsReversed { get; set; }

    private Task OnAskQuestionAsync(string question)
    {
        _userQuestion = question;
        return OnAskClickedAsync();
    }

    [Parameter]
    [SupplyParameterFromQuery(Name = nameof(ChatHistorySession.Id))]
    public string? ChatSessionId { get; set; }
    [Inject]
    internal ChatHistoryService ChatHistoryService { get; set; }

    protected override void OnInitialized()
    {
        //LoadChatHistoryFromQueryParam();
    }

    protected override async Task OnParametersSetAsync()
    {
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

        if (uri.Segments.Contains("chat"))
        {
            LoadChatHistoryFromQueryParam();
            StateHasChanged();
        }
    }

    private void LoadChatHistoryFromQueryParam()
    {
        Console.WriteLine("Loading chat history from query param");
        var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

        if (!QueryHelpers.ParseQuery(uri.Query).TryGetValue(nameof(ChatHistorySession.Id), out var ChatSessionId))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ChatSessionId))
        {
            return;
        }

        Console.WriteLine($"ChatSessionId: {ChatSessionId}");

        if (!int.TryParse(ChatSessionId, out int chatSessionId))
        {

            return;
        }

        Console.WriteLine($"chatSessionId: {chatSessionId}");


        if (ChatHistoryService.TryGetChatHistorySession(chatSessionId, out var chatHistorySession))
        {
            Console.WriteLine($"Replacing ChatHistorySession");
            _questionAndAnswerMap = [];
            foreach (var i in chatHistorySession.QuestionAnswerMap)
            {
                _questionAndAnswerMap[i.Key] = i.Value;
            }
        }
        else
        {
            _questionAndAnswerMap = [];
        }
    }

    private async Task OnAskClickedAsync()
    {
        if (string.IsNullOrWhiteSpace(_userQuestion))
        {
            return;
        }

        _isReceivingResponse = true;
        _lastReferenceQuestion = _userQuestion;
        _currentQuestion = new(_userQuestion, DateTime.Now);
        _questionAndAnswerMap[_currentQuestion] = null;

        try
        {
            var history = _questionAndAnswerMap
                .Where(x => x.Value?.Choices is { Length: > 0 })
                .SelectMany(x => new ChatMessage[] { new ChatMessage("user", x.Key.Question), new ChatMessage("assistant", x.Value!.Choices[0].Message.Content) })
                .ToList();

            history.Add(new ChatMessage("user", _userQuestion));

            var request = new ChatRequest([.. history], Settings.Overrides);
            var result = await ApiClient.ChatConversationAsync(request);

            _questionAndAnswerMap[_currentQuestion] = result.Response;
            if (result.IsSuccessful)
            {
                _userQuestion = "";
                _currentQuestion = default;
            }
        }
        finally
        {
            _isReceivingResponse = false;
        }
    }


    public string PinIcon(string question)
    {
        return PinnedQueriesService.GetPinnedQueries().Any(q => string.Equals(q.Question, question, StringComparison.InvariantCultureIgnoreCase))
            ? Icons.Material.Filled.PushPin
            : Icons.Material.Outlined.PushPin;
    }

    private void OnSaveChat()
    {
        var newChatHistorySession = ChatHistoryService.AddChatHistorySession(_questionAndAnswerMap);

        ChatSessionId = newChatHistorySession.Id.ToString();
        NavigationManager.NavigateTo($"/chat?{nameof(ChatHistorySession.Id)}={newChatHistorySession.Id}");
    }

    private void OnPinQuestion(string question, DateTime askedOn)
    {

        var pinnedq = PinnedQueriesService
            .GetPinnedQueries()
            .FirstOrDefault(q => string.Equals(q.Question, question, StringComparison.InvariantCultureIgnoreCase));

        Console.WriteLine(pinnedq.Question ?? "No questions here :D");

        if (PinnedQueriesService
            .GetPinnedQueries()
            .Any(q => string.Equals(q.Question, question, StringComparison.InvariantCultureIgnoreCase)))
        {
            PinnedQueriesService.DeletePinnedQuery(question);
        }
        else
        {
            var userQuestion = new UserQuestion(question, DateTime.Now);
            PinnedQueriesService.AddPinnedQuery(userQuestion);
        }

        StateHasChanged();
    }

    private void OnClearChat()
    {
        ChatSessionId = null;
        _userQuestion = _lastReferenceQuestion = "";
        _currentQuestion = default;
        _questionAndAnswerMap.Clear();
        NavigationManager.NavigateTo("/chat");
    }
}
