namespace Lemon.Application.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageIndex, int PageSize, long Total);
