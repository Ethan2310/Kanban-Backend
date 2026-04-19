using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Application.Boards;
using Application.Common.Exceptions;

using Microsoft.AspNetCore.Http;

using Web.OpenApi;

namespace Web.Endpoints;

public static class BoardEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/boards")
            .WithTags("Boards")
            .RequireAuthorization();

        group.MapPost("", async (CreateBoardRequest req, HttpContext http, BoardService boardService, CancellationToken ct) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

                if (!int.TryParse(userIdClaim, out var currentUserId))
                    throw new UnauthorizedException("Invalid or missing user identity.");

                var result = await boardService.CreateBoardAsync(req, currentUserId, ct);
                return Results.Created($"/api/boards/{result.BoardId}", result);
            })
            .WithName("CreateBoard")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Create a new board";
                operation.Description = "Creates a new board with the specified name and description.";
                return operation;
            })
            .Produces<CreateBoardResponse>(StatusCodes.Status201Created)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapPatch("/{boardId}", async (int boardId, UpdateBoardRequest req, HttpContext http, BoardService boardService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var result = await boardService.UpdateBoardAsync(boardId, req, currentUserId, ct);
            return Results.Ok(result);
        })
        .WithName("UpdateBoard")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Update an existing board";
            operation.Description = "Partially updates a board. Only fields included in the request body are applied.";
            return operation;
        })
        .Produces<UpdateBoardResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);


        group.MapDelete("/{boardId}", async (int boardId, HttpContext http, BoardService boardService, CancellationToken ct) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var deleteReq = new DeleteBoardRequest(boardId);
            var result = await boardService.DeleteBoardAsync(deleteReq, currentUserId, ct);
            return Results.Ok(result);
        })
        .WithName("DeleteBoard")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Delete a board";
            operation.Description = "Deletes an existing board by its ID.";
            return operation;
        })
        .Produces<DeleteBoardResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("", async ([AsParameters] GetBoardsRequest req, HttpContext http, BoardService boardService, CancellationToken ct) =>
        {
            var result = await boardService.GetBoardsAsync(req, ct);
            return Results.Ok(result);
        })
        .WithName("GetBoards")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Get a list of boards";
            operation.Description = "Retrieves a paginated list of boards, optionally filtered by project ID and name.";
            return operation;
        })
        .Produces<GetBoardsResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
        .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
