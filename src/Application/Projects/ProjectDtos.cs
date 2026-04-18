using Application.Common.Models;

public record CreateProjectRequest(
    string Name,
    string? Description);

public record CreateProjectResponse(
    int ProjectId,
    string Name,
    string? Description);

public record UpdateProjectRequest(
    int ProjectId,
    string Name,
    string? Description);

public record UpdateProjectResponse(
    int ProjectId,
    string Name,
    string? Description);

public record DeleteProjectRequest(int ProjectId);
public record DeleteProjectResponse(bool Success);

public record GetProjectsRequest(
    int? UserId,
    int? BoardId,
    string? Name,
    int PageNumber = PaginationRequestDefaults.PageNumber,
    int PageSize = PaginationRequestDefaults.PageSize);

public record ProjectSummaryResponse(
    int ProjectId,
    string Name,
    string? Description);

public record GetProjectsResponse(
    IReadOnlyList<ProjectSummaryResponse> Projects,
    PaginationMetadata Pagination);
