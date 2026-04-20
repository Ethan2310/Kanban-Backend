using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Application.Common.Exceptions;
using Application.Tasks;

using Web.OpenApi;

namespace Web.Endpoints;

public static class TaskEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/tasks")
            .WithTags("Tasks")
            .RequireAuthorization();

        group.MapPost("", async (CreateTaskRequest req, HttpContext http, TaskService taskService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var result = await taskService.CreateTaskAsync(req, currentUserId, ct);
            return Results.Created($"/api/tasks/{result.TaskId}", result);
        })
            .WithName("CreateTask")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Create a new task";
                operation.Description = "Creates a new task and records a status history entry for its initial status.";
                return operation;
            })
            .Produces<CreateTaskResponse>(StatusCodes.Status201Created)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapPatch("/{taskId}", async (int taskId, UpdateTaskRequest req, HttpContext http, TaskService taskService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var result = await taskService.UpdateTaskAsync(taskId, req, currentUserId, ct);
            return Results.Ok(result);
        })
            .WithName("UpdateTask")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Update an existing task";
                operation.Description = "Partially updates a task and records a corresponding status history entry.";
                return operation;
            })
            .Produces<UpdateTaskResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{taskId}", async (int taskId, HttpContext http, TaskService taskService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var result = await taskService.DeleteTaskAsync(new DeleteTaskRequest(taskId), currentUserId, ct);
            return Results.Ok(result);
        })
            .WithName("DeleteTask")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Delete a task";
                operation.Description = "Soft deletes the task and hard deletes all related status history entries in a transaction.";
                return operation;
            })
            .Produces<DeleteTaskResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("", async ([AsParameters] GetTasksRequest req, TaskService taskService, CancellationToken ct) =>
        {
            var result = await taskService.GetTasksAsync(req, ct);
            return Results.Ok(result);
        })
            .WithName("GetTasks")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get a paginated list of tasks";
                operation.Description = "Retrieves active tasks with optional filtering by board, list, status, and assignee.";
                return operation;
            })
            .Produces<GetTasksResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
