// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Models;
public readonly record struct ChatHistorySession(
    int Id,
    string Name,
    DateTime StartTime,
    DateTime EndTime,
    Dictionary<UserQuestion, ChatAppResponseOrError?> QuestionAnswerMap
    );

