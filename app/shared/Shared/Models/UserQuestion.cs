// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public readonly record struct UserQuestion(
    string Question,
    DateTime AskedOn);
