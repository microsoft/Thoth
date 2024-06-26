// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;
public class PinnedQuery
{
	public string Id { get; set; } = string.Empty;
	public string UserId { get; set; } = string.Empty;
	public UserQuestion Query { get; set; } = new();

	public PinnedQuery() { }

	public PinnedQuery(string id, string userId, UserQuestion query)
	{
		Id = id;
		UserId = userId;
		Query = query;
	}
}
