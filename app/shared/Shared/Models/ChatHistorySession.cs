// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public class ChatHistorySession
{
	public ChatHistorySession() { }
	public ChatHistorySession(
	string Id,
	string UserId,
	string Title,
	int TotalTokens,
	List<ChatHistoryQA> ChatHistory)
	{
		Id = Id;
		UserId = UserId;
		Title = Title;
		TotalTokens = TotalTokens;
		ChatHistory = ChatHistory;
	}
	public string Id { get; set; } = Guid.NewGuid().ToString(); // should default be null?
	public string UserId { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public int TotalTokens { get; set; } = 0;
	public List<ChatHistoryQA> ChatHistory { get; set; } = [];
}

public class ChatHistoryQA
{

	public ChatHistoryQA() { }

	public ChatHistoryQA(UserQuestion question, ChatAppResponseOrError response)
	{
		Question = question;
		Response = response;
	}

	public UserQuestion Question { get; set; }
	public ChatAppResponseOrError Response { get; set; }
}
