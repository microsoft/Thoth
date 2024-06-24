// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;

namespace ClientApp.Services;
public class ChatHistoryService
{
    private const string CHAT_HISTORY_SESSIONS_KEY = "chatHistorySessions";

    // todo: save across browser sessions
    private readonly Dictionary<string, ChatHistorySession> _chatHistorySessions = new();

    public event Action OnChange;
    public List<EventCallback> listeners { get; private set; } = new List<EventCallback>();


    private void NotifyStateChanged() => OnChange?.Invoke();

    public ChatHistorySession AddChatHistorySession(Dictionary<UserQuestion, ChatAppResponseOrError?> questionAnswerMap)
    {
        var sessionId = Guid.NewGuid().ToString();
        // todo: generate sessionName, sessionStartTime, sessionEndTime
        var sessionName = $"Session {sessionId}";
        var chatHistorySession = new ChatHistorySession(sessionId, sessionName, DateTime.Now, DateTime.Now, questionAnswerMap);
        _chatHistorySessions.Add(sessionId, chatHistorySession);
        NotifyStateChanged();
        return chatHistorySession;
    }

    public bool TryGetChatHistorySession(string sessionId, out ChatHistorySession chatHistorySession)
    {
        return _chatHistorySessions.TryGetValue(sessionId, out chatHistorySession);
    }

    public IEnumerable<ChatHistorySession> GetChatHistorySessions()
    {
        return _chatHistorySessions.Values;
    }

    public void DeleteChatHistorySession(string sessionId)
    {
        _chatHistorySessions.Remove(sessionId);
        NotifyStateChanged();
    }
}
