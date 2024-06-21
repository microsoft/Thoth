// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.ChatCompletion;

public interface IChatHistoryService
{
    Task AddChatHistorySession(ChatHistorySession chatHistory);

    Task<ChatHistorySession> GetChatHistorySession(int sessionId);

    Task<IEnumerable<ChatHistorySession>> GetChatHistorySessions();

    Task DeleteChatHistorySession(int sessionId);
}
