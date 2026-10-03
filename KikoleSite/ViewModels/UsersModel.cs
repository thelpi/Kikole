using System;
using System.Collections.Generic;

namespace KikoleSite.ViewModels;

public class UsersModel
{
    public const int PageSize = 25;

    public required UserListQuery Query { get; init; }

    public required IReadOnlyList<UserAdminRow> Rows { get; init; }

    public int TotalCount { get; init; }

    public string? Feedback { get; init; }

    public string? Error { get; init; }

    public int TotalPages => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);
}
