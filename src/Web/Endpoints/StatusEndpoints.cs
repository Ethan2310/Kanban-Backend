using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Application.Common.Exceptions;
using Application.Statuses;

using Web.OpenApi;

namespace Web.Endpoints;

public static class StatusEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/statuses")
            .WithTags("Statuses")
            .RequireAuthorization();

        group.MapPost("", async (CreateStatusRequest req, HttpContext http, StatusService statusService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var result = await statusService.CreateStatusAsync(req, currentUserId, ct);
            return Results.Created($"/api/statuses/{result.StatusId}", result);
        })
            .WithName("CreateStatus")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Create a new status";
                operation.Description = "Creates a new status with the specified name, order index, and color. Color must be a hex value in the format #RRGGBB.";
                return operation;
            })
            .Produces<CreateStatusResponse>(StatusCodes.Status201Created)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapPatch("/{statusId}", async (int statusId, UpdateStatusRequest req, HttpContext http, StatusService statusService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var result = await statusService.UpdateStatusAsync(statusId, req, currentUserId, ct);
            return Results.Ok(result);
        })
            .WithName("UpdateStatus")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Update an existing status";
                operation.Description = "Partially updates a status. Only fields included in the request body are applied. Color must be a hex value in the format #RRGGBB.";
                return operation;
            })
            .Produces<UpdateStatusResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{statusId}", async (int statusId, HttpContext http, StatusService statusService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var deleteReq = new DeleteStatusRequest(statusId);
            var result = await statusService.DeleteStatusAsync(deleteReq, currentUserId, ct);
            return Results.Ok(result);
        })
            .WithName("DeleteStatus")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Delete an existing status";
                operation.Description = "Deletes an existing status by setting its IsActive flag to false.";
                return operation;
            })
            .Produces<DeleteStatusResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("", async ([AsParameters] GetStatusesRequest req, StatusService statusService, CancellationToken ct) =>
        {
            var result = await statusService.GetStatusesAsync(req, ct);
            return Results.Ok(result);
        })
            .WithName("GetStatuses")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get a paginated list of statuses";
                operation.Description = "Retrieves a paginated list of statuses with optional filtering and sorting.";
                return operation;
            })
            .Produces<GetStatusesResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

    }
}
