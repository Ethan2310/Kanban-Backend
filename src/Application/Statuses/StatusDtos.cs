using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Application.Common.Models;

namespace Application.Statuses;

public record CreateStatusRequest(
    string Name,
    int OrderIndex,
    [property: StringLength(7, MinimumLength = 7)]
    [property: Description("Hex color in the format #RRGGBB.")]
    string Color);

public record CreateStatusResponse(
    int StatusId,
    string Name,
    int OrderIndex,
    [property: Description("Hex color in the format #RRGGBB.")]
    string Color);

public record UpdateStatusRequest(
    string? Name,
    int? OrderIndex,
    [property: StringLength(7, MinimumLength = 7)]
    [property: Description("Hex color in the format #RRGGBB.")]
    string? Color);

public record UpdateStatusResponse(
    int StatusId,
    string Name,
    int OrderIndex,
    [property: Description("Hex color in the format #RRGGBB.")]
    string Color);

public record DeleteStatusRequest(int StatusId);
public record DeleteStatusResponse(bool Success);

public record GetStatusesRequest(
    string? Name,
    int PageNumber = PaginationRequestDefaults.PageNumber,
    int PageSize = PaginationRequestDefaults.PageSize);

public record StatusSummaryResponse(
    int StatusId,
    string Name,
    int OrderIndex,
    [property: Description("Hex color in the format #RRGGBB.")]
    string Color);

public record GetStatusesResponse(
    IReadOnlyList<StatusSummaryResponse> Statuses,
    PaginationMetadata Pagination);
