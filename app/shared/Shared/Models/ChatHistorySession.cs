// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record ChatHistorySession(
	string Id,
	string UserId,
	string Title,
	int TotalTokens,
	List<ChatHistoryQA> ChatHistory
);

public record ChatHistoryQA(
	UserQuestion Question,
	ChatAppResponseOrError Response
);
