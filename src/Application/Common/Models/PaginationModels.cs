namespace Application.Common.Models;

public static class PaginationRequestDefaults
{
    public const int PageNumber = 1;
    public const int PageSize = 20;
    public const int MaxPageSize = 100;
}

public record PaginationMetadata(int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling((double)TotalCount / PageSize);
}
