using Application.Users;

namespace Web.Endpoints;

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest req, AuthService auth, CancellationToken ct) =>
        {
            var result = await auth.RegisterAsync(req, ct);
            return Results.Created($"/api/users/{result.UserId}", result);
        })
        .WithName("Register")
        .AllowAnonymous();

        group.MapPost("/login", async (LoginRequest req, AuthService auth, CancellationToken ct) =>
        {
            var result = await auth.LoginAsync(req, ct);
            return Results.Ok(result);
        })
        .WithName("Login")
        .AllowAnonymous();
    }
}
