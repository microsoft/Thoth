// Copyright (c) Microsoft. All rights reserved.

public interface IChatHistoryService
{
    Task AddChatHistorySessionAsync(ChatHistorySession chatHistory);

    Task<ChatHistorySession> GetChatHistorySessionAsync(string sessionId);

    IAsyncEnumerable<ChatHistorySession> GetChatHistorySessionsAsync(string userId);

    Task DeleteChatHistorySessionAsync(string sessionId);
}
