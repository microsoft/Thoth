// Copyright (c) Microsoft. All rights reserved.

using Shared.Models;

public interface IChatHistoryService
{
	Task<ChatHistorySession> UpsertChatHistorySessionAsync(ChatHistorySession chatHistory);

    Task<ChatHistorySession> GetChatHistorySessionAsync(string sessionId);

    IAsyncEnumerable<ChatHistorySession> GetChatHistorySessionsAsync(string userId);

    Task DeleteChatHistorySessionAsync(string sessionId);
}
