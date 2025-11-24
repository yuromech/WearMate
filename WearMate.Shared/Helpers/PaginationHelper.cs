using WearMate.Shared.DTOs.Common;

namespace WearMate.Shared.Helpers;

/// <summary>
/// Helper for pagination
/// </summary>
public static class PaginationHelper
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Validate and normalize page number
    /// </summary>
    public static int ValidatePage(int page)
    {
        return page < 1 ? 1 : page;
    }

    /// <summary>
    /// Validate and normalize page size
    /// </summary>
    public static int ValidatePageSize(int pageSize)
    {
        if (pageSize < 1) return DefaultPageSize;
        if (pageSize > MaxPageSize) return MaxPageSize;
        return pageSize;
    }

    /// <summary>
    /// Calculate offset for SQL query
    /// </summary>
    public static int CalculateOffset(int page, int pageSize)
    {
        return (page - 1) * pageSize;
    }

    /// <summary>
    /// Create paginated result
    /// </summary>
    public static PaginatedResult<T> CreateResult<T>(
        List<T> items,
        int totalCount,
        int page,
        int pageSize)
    {
        return new PaginatedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = ValidatePage(page),
            PageSize = ValidatePageSize(pageSize)
        };
    }

    /// <summary>
    /// Create empty paginated result
    /// </summary>
    public static PaginatedResult<T> CreateEmpty<T>(int page = 1, int pageSize = DefaultPageSize)
    {
        return new PaginatedResult<T>
        {
            Items = new List<T>(),
            TotalCount = 0,
            Page = ValidatePage(page),
            PageSize = ValidatePageSize(pageSize)
        };
    }
}