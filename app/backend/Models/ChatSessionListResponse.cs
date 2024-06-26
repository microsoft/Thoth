// Copyright (c) Microsoft. All rights reserved.

namespace MinimalApi.Models;

public record ChatSessionListResponse(
	string Id,
	string UserId,
	string Title,
	DateTime LastUpdated
);
