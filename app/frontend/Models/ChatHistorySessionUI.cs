// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Models;
public readonly record struct ChatHistorySessionUI(
	string Id,
	string Name,
	DateTime StartTime,
	DateTime EndTime,
	Dictionary<UserQuestion, ChatAppResponseOrError?> QuestionAnswerMap
	);

