﻿// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Services;
public class ChatHistoryService
{
    private const string CHAT_HISTORY_SESSIONS_KEY = "chatHistorySessions";

    // todo: save across browser sessions
    private readonly Dictionary<int, ChatHistorySession> _chatHistorySessions = new();

    public void AddChatHistorySession(Dictionary<UserQuestion, ChatAppResponseOrError?> questionAnswerMap)
    {
        var sessionId = _chatHistorySessions.Keys.Any() ? _chatHistorySessions.Keys.Max() + 1 : 1;
        // todo: generate sessionName, sessionStartTime, sessionEndTime
        var sessionName = $"Session {sessionId}";
        var chatHistorySession = new ChatHistorySession(sessionId, sessionName, DateTime.Now, DateTime.Now, questionAnswerMap);
        _chatHistorySessions.Add(sessionId, chatHistorySession);
    }

    public ChatHistorySession GetChatHistorySession(int sessionId)
    {
        return _chatHistorySessions[sessionId];
    }

    public IEnumerable<ChatHistorySession> GetChatHistorySessions()
    {
        return _chatHistorySessions.Values;
    }

    public void DeleteChatHistorySession(int sessionId)
    {
        _chatHistorySessions.Remove(sessionId);
    }
}
