using Application.Common.Models;

namespace Application.Lists;

public record CreateListRequest(string Name, int BoardId, int StatusId);

public record CreateListResponse(int ListId, string Name, int BoardId, int StatusId, int OrderIndex);

public record UpdateListRequest(string? Name, int? StatusId);

public record UpdateListResponse(int ListId, string Name, int BoardId, int StatusId, int OrderIndex);

public record DeleteListRequest(int ListId);

public record DeleteListResponse(bool Success);

public record GetListsRequest(
    int? BoardId,
    string? Name,
    int PageNumber = PaginationRequestDefaults.PageNumber,
    int PageSize = PaginationRequestDefaults.PageSize);

public record ListSummaryResponse(int ListId, string Name, int BoardId, int StatusId, int OrderIndex);

public record GetListsResponse(
    IReadOnlyList<ListSummaryResponse> Lists,
    PaginationMetadata Pagination);
