using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Application.Common.Exceptions;
using Application.Lists;

using Web.OpenApi;

namespace Web.Endpoints;

public static class ListEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/lists")
            .WithTags("Lists")
            .RequireAuthorization();

        group.MapPost("", async (CreateListRequest req, HttpContext http, ListService listService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var result = await listService.CreateListAsync(req, currentUserId, ct);
            return Results.Created($"/api/lists/{result.ListId}", result);
        })
            .WithName("CreateList")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Create a new list";
                operation.Description = "Creates a new list for a board. The status must exist and the list order index is derived from the selected status.";
                return operation;
            })
            .Produces<CreateListResponse>(StatusCodes.Status201Created)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapPatch("/{listId}", async (int listId, UpdateListRequest req, HttpContext http, ListService listService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var result = await listService.UpdateListAsync(listId, req, currentUserId, ct);
            return Results.Ok(result);
        })
            .WithName("UpdateList")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Update an existing list";
                operation.Description = "Partially updates a list. If statusId is provided, the status must exist and list order index is recalculated from that status.";
                return operation;
            })
            .Produces<UpdateListResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{listId}", async (int listId, ListService listService, CancellationToken ct) =>
        {
            var result = await listService.DeleteListAsync(new DeleteListRequest(listId), ct);
            return Results.Ok(result);
        })
            .WithName("DeleteList")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Delete a list";
                operation.Description = "Deletes a list only when it has no active tasks associated with it.";
                return operation;
            })
            .Produces<DeleteListResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("", async ([AsParameters] GetListsRequest req, ListService listService, CancellationToken ct) =>
        {
            var result = await listService.GetListsAsync(req, ct);
            return Results.Ok(result);
        })
            .WithName("GetLists")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Get a paginated list of lists";
                operation.Description = "Retrieves lists with optional filtering by board and name.";
                return operation;
            })
            .Produces<GetListsResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
