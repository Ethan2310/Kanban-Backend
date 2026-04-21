using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Application.Common.Exceptions;
using Application.Common.Models;
using Application.Projects;

using Web.OpenApi;

namespace Web.Endpoints;

public static class ProjectUserAccessEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/projects/{projectId}/users")
            .WithTags("Project User Access")
            .RequireAuthorization();

        group.MapPost("", async (int projectId, AddUserToProjectRequestBody body, HttpContext http, ProjectService projectService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var request = new AddUserToProjectRequest(projectId, body.UserId);
            var result = await projectService.AddUserToProjectAsync(request, currentUserId, ct);
            return Results.Ok(result);
        })
            .WithName("AddUserToProject")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Add user access to a project";
                operation.Description = "Creates a user-project access entry for the specified project and user.";
                return operation;
            })
            .Produces<AddUserToProjectResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("", async (int projectId, int? pageNumber, int? pageSize, HttpContext http, ProjectService projectService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var request = new GetUsersInProjectRequest(
                projectId,
                pageNumber ?? PaginationRequestDefaults.PageNumber,
                pageSize ?? PaginationRequestDefaults.PageSize);

            var result = await projectService.GetUsersInProjectAsync(request, currentUserId, ct);
            return Results.Ok(result);
        })
            .WithName("GetUsersInProject")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get users in a project";
                operation.Description = "Retrieves a paginated list of users who have access to the specified project.";
                return operation;
            })
            .Produces<GetUsersInProjectResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{userId}", async (int projectId, int userId, HttpContext http, ProjectService projectService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var request = new RemoveUserFromProjectRequest(projectId, userId);
            var result = await projectService.RemoveUserFromProjectAsync(request, currentUserId, ct);
            return Results.Ok(result);
        })
            .WithName("RemoveUserFromProject")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Remove user access from a project";
                operation.Description = "Revokes a user's access to the specified project by deactivating the user-project access entry.";
                return operation;
            })
            .Produces<RemoveUserFromProjectResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}

public record AddUserToProjectRequestBody(int UserId);
