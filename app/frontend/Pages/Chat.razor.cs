// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.WebUtilities;

namespace ClientApp.Pages;

public sealed partial class Chat
{
	private string _userQuestion = "";
	private UserQuestion _currentQuestion;
	private string _lastReferenceQuestion = "";
	private bool _isReceivingResponse = false;

	private ChatHistorySession _chatHistorySession = new();

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

	protected override async Task OnParametersSetAsync()
	{
		var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

		if (uri.Segments.Contains("chat"))
		{
			await LoadChatHistoryFromQueryParamAsync();
			StateHasChanged();
		}
	}

	private async Task LoadChatHistoryFromQueryParamAsync()
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

		_chatHistorySession = (await ApiClient.GetChatHistorySessionAsync(ChatSessionId)) ?? new();
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
		var currentInteraction = new ChatHistoryQA(_currentQuestion, null);
		_chatHistorySession.ChatHistory.Add(currentInteraction);

		try
		{
			var history = _chatHistorySession.ChatHistory
				.Where(x => x.Response?.Choices is { Length: > 0 })
				.SelectMany(x => new ChatMessage[] {
					new ChatMessage("user", x.Question.Question, 0),
					new ChatMessage("assistant", x.Response!.Choices[0].Message.Content, x.Response!.Choices[0].Message.TotalTokens) })
				.ToList();

			history.Add(new ChatMessage("user", _userQuestion, 0));

			var request = new ChatRequest([.. history], Settings.Overrides);
			var result = await ApiClient.ChatConversationAsync(request);

			currentInteraction.Response = result.Response;
			if (result.IsSuccessful)
			{
				_userQuestion = "";
				_currentQuestion = default;
			}

			_chatHistorySession = await ApiClient.UpsertChatHistorySessionAsync(_chatHistorySession);

			ChatSessionId = _chatHistorySession.Id;
			NavigationManager.NavigateTo($"/chat?{nameof(ChatHistorySession.Id)}={ChatSessionId}");
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
		_chatHistorySession = new();
		NavigationManager.NavigateTo("/chat");
	}
}
