# Adding a New Endpoint

This document walks through every file you need to touch to add a new endpoint, using the auth endpoints (`POST /api/auth/register`, `POST /api/auth/login`) as the reference implementation.

---

## Overview

Endpoints are split across three layers. Each layer has a single responsibility:

| Layer | Folder | Responsibility |
|---|---|---|
| Application | `src/Application/<Feature>/` | DTOs, validators, service logic |
| Web | `src/Web/Endpoints/` | HTTP route definition only — no business logic |
| Web | `src/Web/Program.cs` | Register the endpoint group |

The Application layer registers its own services in `src/Application/DependencyInjection.cs`. Validators are picked up automatically — no manual registration needed.

---

## Step 1 — DTOs (`<Feature>Dtos.cs`)

Create request and response records in `src/Application/<Feature>/`.

```csharp
// src/Application/Users/AuthDtos.cs
namespace Application.Users;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role,
    int? AddedById);

public record RegisterResponse(
    int UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role);
```

**Rules:**
- Use `record` types — they are immutable and give value equality for free.
- Request records are what the endpoint receives from the client (request body).
- Response records are what the endpoint returns. Never return a domain entity directly.
- Keep all DTOs for one feature in a single file.

---

## Step 2 — Validators (`<Feature>Validator.cs`)

Create one `AbstractValidator<TRequest>` per request record in `src/Application/<Feature>/`.

```csharp
// src/Application/Users/AuthValidator.cs
using FluentValidation;

namespace Application.Users;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100);
    }
}
```

**Rules:**
- Validators are **auto-discovered** by `AddValidatorsFromAssembly` in `DependencyInjection.cs` — you do not register them manually.
- Inject `IValidator<TRequest>` into the service constructor and call `ValidateAsync` as the first thing in every service method.
- Throw `ValidationException(validation.Errors)` if validation fails — the middleware maps this to a `400 Bad Request`.

---

## Step 3 — Service (`<Feature>Service.cs`)

Write the business logic in `src/Application/<Feature>/`. The service takes `IApplicationDbContext` (and any other interfaces) via constructor injection.

```csharp
// src/Application/Users/AuthService.cs
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ValidationException = Application.Common.Exceptions.ValidationException;

namespace Application.Users;

public class AuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<RegisterRequest> _registerValidator;

    public AuthService(IApplicationDbContext context, IValidator<RegisterRequest> registerValidator)
    {
        _context = context;
        _registerValidator = registerValidator;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        // 1. Validate input
        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // 2. Business rules
        var emailTaken = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == request.Email, ct);

        if (emailTaken)
            throw new ConflictException("A user with this email address already exists.");

        // 3. Persist and return
        // ...
    }
}
```

**Rules:**
- No HTTP concepts here (`HttpContext`, `IResult`, status codes). The service knows nothing about HTTP.
- Always validate before any DB work.
- Use the exceptions from `Application/Common/Exceptions/` to signal failure — the global middleware handles the HTTP mapping:

| Exception | HTTP Status |
|---|---|
| `ValidationException` | 400 Bad Request |
| `UnauthorizedException` | 401 Unauthorized |
| `NotFoundException` | 404 Not Found |
| `ConflictException` | 409 Conflict |
| Unhandled exception | 500 Internal Server Error |

- Soft-delete query filters are active by default on every `DbSet`. If you need to query inactive records (e.g. checking for duplicate emails across all users), use `.IgnoreQueryFilters()`.
- Never call `_context.Remove()`. Soft delete by setting `entity.IsActive = false`.

---

## Step 4 — Register the Service

Add a `services.AddScoped<YourService>()` line in `src/Application/DependencyInjection.cs`.

```csharp
// src/Application/DependencyInjection.cs
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly()); // picks up all validators automatically
    services.AddScoped<TaskService>();
    services.AddScoped<AuthService>();   // ← add your service here
    // ...
    return services;
}
```

---

## Step 5 — Endpoint Group (`<Feature>Endpoints.cs`)

Create a static class in `src/Web/Endpoints/`. The endpoint handler should be a single line — just call the service and return the result.

```csharp
// src/Web/Endpoints/AuthEndpoints.cs
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
```

**Rules:**
- No business logic in the endpoint handler — one line per handler.
- Inject the service and `CancellationToken` directly as handler parameters. ASP.NET Core resolves them automatically.
- Use `.AllowAnonymous()` only for public routes (login, register). Protected routes use `.RequireAuthorization()`.
- Use the correct HTTP result for the operation:

| Operation | Return |
|---|---|
| Fetch resource | `Results.Ok(result)` |
| Create resource | `Results.Created("/api/resource/{id}", result)` |
| No content | `Results.NoContent()` |

- Give every endpoint a unique `.WithName("...")` — this is used by OpenAPI.
- Group related endpoints under the same `MapGroup` prefix.

---

## Step 6 — Register the Endpoint Group

Call `YourEndpoints.Map(app)` in `src/Web/Program.cs` after `app.UseAuthorization()`.

```csharp
// src/Web/Program.cs
app.UseAuthentication();
app.UseAuthorization();

AuthEndpoints.Map(app);      // ← add your endpoint group here
YourFeatureEndpoints.Map(app);
```

---

## Checklist

- [ ] DTOs defined in `src/Application/<Feature>/<Feature>Dtos.cs`
- [ ] Validators defined in `src/Application/<Feature>/<Feature>Validator.cs`
- [ ] Service implemented in `src/Application/<Feature>/<Feature>Service.cs`
- [ ] Service registered in `src/Application/DependencyInjection.cs`
- [ ] Endpoint group created in `src/Web/Endpoints/<Feature>Endpoints.cs`
- [ ] Endpoint group mapped in `src/Web/Program.cs`
- [ ] `dotnet build` passes with 0 errors
