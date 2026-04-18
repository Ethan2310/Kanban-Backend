using Application.Common.Models;

namespace Application.Boards;

public record CreateBoardRequest(
    string Name,
    string? Description);

public record CreateBoardResponse(
    int BoardId,
    string Name,
    string? Description);

public record UpdateBoardRequest(
    int BoardId,
    string Name,
    string? Description);

public record UpdateBoardResponse(
    int BoardId,
    string Name,
    string? Description);

public record DeleteBoardRequest(int BoardID);

public record DeleteBoardResponse(bool Success);

public record GetBoardsRequest(
     int? ProjectId,
     string? Name,
    int PageNumber = PaginationRequestDefaults.PageNumber,
    int PageSize = PaginationRequestDefaults.PageSize);

public record BoardSummaryResponse(
    int BoardId,
    string Name,
    string? Description);

public record GetBoardsResponse(
    IReadOnlyList<BoardSummaryResponse> Boards,
    PaginationMetadata Pagination);
