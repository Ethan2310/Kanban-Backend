using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Application.Common.Exceptions;
using Application.Projects;

using Web.OpenApi;

namespace Web.Endpoints;

public static class ProjectEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        group.MapPost("", async (CreateProjectRequest req, HttpContext http, ProjectService projectService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var result = await projectService.CreateProjectAsync(req, currentUserId, ct);
            return Results.Created($"/api/projects/{result.ProjectId}", result);
        })
            .WithName("CreateProject")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Create a new project";
                operation.Description = "Creates a new project with the specified name and description.";
                return operation;
            })
            .Produces<CreateProjectResponse>(StatusCodes.Status201Created)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapPatch("/{projectId}", async (int projectId, UpdateProjectRequest req, HttpContext http, ProjectService projectService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var result = await projectService.UpdateProjectAsync(projectId, req, currentUserId, ct);
            return Results.Ok(result);
        })
            .WithName("UpdateProject")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Update an existing project";
                operation.Description = "Partially updates a project. Only fields included in the request body are applied.";
                return operation;
            })
            .Produces<UpdateProjectResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{projectId}", async (int projectId, HttpContext http, ProjectService projectService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var deleteReq = new DeleteProjectRequest(projectId);
            await projectService.DeleteProjectAsync(deleteReq, currentUserId, ct);
            return Results.NoContent();
        })
            .WithName("DeleteProject")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Delete a project";
                operation.Description = "Deletes an existing project.";
                return operation;
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("", async ([AsParameters] GetProjectsRequest req, HttpContext http, ProjectService projectService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var result = await projectService.GetProjectsAsync(req, currentUserId, ct);
            return Results.Ok(result);
        })
            .WithName("GetProjects")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get a list of projects";
                operation.Description = "Retrieves a paginated list of projects, with optional filtering by user ID, board ID, and project name.";
                return operation;
            })
            .Produces<GetProjectsResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
