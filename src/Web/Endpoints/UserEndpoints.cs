using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Application.Common.Exceptions;
using Application.Common.Models;
using Application.Users;

using Microsoft.AspNetCore.Http;

using Web.OpenApi;

namespace Web.Endpoints;

public static class UserEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapDelete("/{userId}",
            async (int userId, HttpContext http, AuthService auth, CancellationToken ct) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

                if (!int.TryParse(userIdClaim, out var currentUserId))
                    throw new UnauthorizedException("Invalid or missing user identity.");

                await auth.DeleteUserAsync(currentUserId, userId, ct);
                return Results.NoContent();
            })
            .WithName("DeleteUser")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Delete a user";
                operation.Description = "Only administrators can delete users. The userId is specified in the path and the caller is resolved from the JWT token.";
                return operation;
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        group.MapGet("/search", async (
            string? firstName,
            string? lastName,
            string? email,
            HttpContext http,
            AuthService auth,
            CancellationToken ct,
            int pageNumber = PaginationRequestDefaults.PageNumber,
            int pageSize = PaginationRequestDefaults.PageSize) =>
        {
            var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? http.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(userIdClaim, out var currentUserId))
                throw new UnauthorizedException("Invalid or missing user identity.");

            var request = new GetUsersRequest(firstName, lastName, email, pageNumber, pageSize);
            var result = await auth.GetUsersAsync(request, currentUserId, ct);
            return Results.Ok(result);
        })
        .WithName("SearchUsers")
        .WithOpenApi(operation =>
        {
            operation.Summary = "Search for users";
            operation.Description = "Search for users by first name, last name, or email with pagination support. Only administrators can call this endpoint, validated from the JWT token.";
            return operation;
        })
        .Produces<GetUsersResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
        .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);
    }

}
