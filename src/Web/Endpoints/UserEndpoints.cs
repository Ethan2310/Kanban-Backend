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
            async (int userId, int adminId, AuthService auth, CancellationToken ct) =>
            {
                var result = await auth.DeleteUserAsync(adminId, userId, ct);
                return Results.NoContent();
            })
            .WithName("DeleteUser")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Delete a user";
                operation.Description = "Only administrators can delete users. The userId is specified in the path, and adminId must be provided as a query parameter.";
                return operation;
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorResponse>(StatusCodes.Status403Forbidden)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}
