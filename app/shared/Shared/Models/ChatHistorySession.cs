// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel.ChatCompletion;

public record ChatHistorySession(
	string Id,
	string UserId,
	string Title,
	ChatHistory ChatHistory
);

