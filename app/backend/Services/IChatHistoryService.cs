// Copyright (c) Microsoft. All rights reserved.

public interface IChatHistoryService
{
    Task AddChatHistorySession(ChatHistorySession chatHistory);

    Task<ChatHistorySession> GetChatHistorySession(string sessionId);

    Task<IEnumerable<ChatHistorySession>> GetChatHistorySessions();

    Task DeleteChatHistorySession(string sessionId);
}
