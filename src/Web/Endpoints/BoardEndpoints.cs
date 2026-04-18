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
    }
}
