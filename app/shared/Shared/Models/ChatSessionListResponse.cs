// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record ChatSessionListResponse(
	string Id,
	string UserId,
	string Title,
	DateTime LastUpdated
);
