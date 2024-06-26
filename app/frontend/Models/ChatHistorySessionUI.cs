// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Models;
public record struct ChatHistorySessionUI(
	string Id,
	string UserId,
	string Title,
	DateTime LastUpdated,
	int TotalTokens,
	Dictionary<UserQuestion, ChatAppResponseOrError?> ChatHistory
	);

