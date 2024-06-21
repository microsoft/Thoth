// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.ChatCompletion;

public class ChatHistorySession
{
    public int SessionId { get; set; }
    public ChatHistory ChatHistory { get; set; } = [];
}
