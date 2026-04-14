# Backend Plan

## Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 8 (LTS) |
| ORM / DB translation | Entity Framework Core 8 |
| Database | MySQL 8.0.32+ |
| EF MySQL driver | Pomelo.EntityFrameworkCore.MySql |
| API style | ASP.NET Core Minimal APIs |
| Validation | FluentValidation |
| Auth | JWT Bearer (custom — no ASP.NET Core Identity) |
| API docs | Scalar / OpenAPI |
| Testing | xUnit, WebApplicationFactory + SQLite (integration-first) |

---

## Architecture: Clean Architecture

We are implementing this architecture from scratch — not using any starter template — using Jason Taylor's architecture as the reference model for how the layers and responsibilities are organised.

The solution is split into four concentric layers. The one rule that governs everything:

> **Dependencies point inward. Outer layers depend on inner layers. Inner layers never depend on outer layers.**

```
┌────────────────────────────────────┐
│         Presentation (Web)         │  ← depends on Application + Infrastructure
│  ┌──────────────────────────────┐  │
│  │      Infrastructure          │  │  ← depends on Application
│  │  ┌────────────────────────┐  │  │
│  │  │      Application       │  │  │  ← depends on Domain only
│  │  │  ┌──────────────────┐  │  │  │
│  │  │  │      Domain      │  │  │  │  ← no dependencies
│  │  │  └──────────────────┘  │  │  │
│  │  └────────────────────────┘  │  │
│  └──────────────────────────────┘  │
└────────────────────────────────────┘
```

---

## Layer Breakdown (mapped to this project)

### 1. Domain — `src/Domain`

The heart of the system. Plain C# classes — no EF Core, no HTTP, no NuGet packages.

**Entities** (objects that have an identity and change over time):
- `BaseEntity` — mirrors the DB base: `Id`, `Guid`, `CreatedById`, `CreatedOn`, `UpdatedById`, `UpdatedOn`, `IsActive`
- `User`, `Project`, `Board`, `List`, `Task`, `Status`
- `ProjectBoard`, `UserProjectAccess` — join entities (still have identity via BaseEntity)
- `TaskStatusHistory` — exception: does **not** inherit BaseEntity (append-only, immutable)

**Enumerations** (strongly typed, not raw strings):
- `UserRole` — `Admin`, `User`
- `TaskPriority` — `Low`, `Medium`, `High`

**Value Objects** (immutable, no identity):
- `HexColor` — wraps the `#RRGGBB` string for `Status.Color`, enforces format on construction

**Key rule:** No class in this layer has `using Microsoft.*` or `using Pomelo.*` anywhere.

---

### 2. Application — `src/Application`

Orchestrates use cases. Knows about Domain. Knows nothing about EF Core, MySQL, or HTTP.

**Structure — vertical slices by feature:**
```
Application/
  Common/
    Interfaces/     ← IApplicationDbContext
    Exceptions/     ← NotFoundException, ValidationException
  Tasks/
    TaskService.cs
    TaskDtos.cs
    TaskValidator.cs
  Boards/
    BoardService.cs
    BoardDtos.cs
  Lists/
    ListService.cs
    ListDtos.cs
  Projects/
    ProjectService.cs
    ProjectDtos.cs
  Statuses/
    StatusService.cs
  Users/
    UserService.cs
    AuthService.cs
  DependencyInjection.cs
```

**Services** — one service class per feature, injected directly into endpoints:
```csharp
// Example shape — TaskService.MoveTaskAsync
public class TaskService
{
    private readonly IApplicationDbContext _context;

    public TaskService(IApplicationDbContext context) => _context = context;

    public async System.Threading.Tasks.Task MoveTaskAsync(
        int taskId, int targetListId, int targetStatusId, int previousStatusId, CancellationToken ct)
    {
        var task = await _context.Tasks.FindAsync(taskId, ct)
            ?? throw new NotFoundException(nameof(Task), taskId);

        // business rule: always update both fields in one transaction
        task.ListId   = targetListId;
        task.StatusId = targetStatusId;

        // insert history record directly — no event bus needed
        _context.TaskStatusHistories.Add(new TaskStatusHistory
        {
            TaskId            = taskId,
            StatusChangedFrom = previousStatusId,
            StatusChangedTo   = targetStatusId,
            ChangedById       = task.UpdatedById,
            ChangedAt         = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(ct);
    }
}
```

**`IApplicationDbContext`** — the Application layer's contract for database access. Infrastructure must implement this:
```csharp
public interface IApplicationDbContext
{
    DbSet<Task>              Tasks               { get; }
    DbSet<Board>             Boards              { get; }
    DbSet<List>              Lists               { get; }
    DbSet<TaskStatusHistory> TaskStatusHistories { get; }
    // ... all other DbSets
    Task<int> SaveChangesAsync(CancellationToken ct);
}
```

**Validators** — FluentValidation rules called explicitly in the service before any DB work:
```csharp
public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.BoardId).GreaterThan(0);
    }
}
```

---

### 3. Infrastructure — `src/Infrastructure`

Implements everything the Application layer defined as interfaces. This is the only layer that talks to MySQL.

**`ApplicationDbContext`** — the EF Core DbContext:
```csharp
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<Task>   Tasks   { get; set; }
    public DbSet<Board>  Boards  { get; set; }
    // ...

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // auto-discovers all IEntityTypeConfiguration<T> in this assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

**Entity Configurations** — one file per entity, keeps `OnModelCreating` clean:
```csharp
// Configurations/TaskConfiguration.cs
public class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).HasMaxLength(255).IsRequired();
        builder.HasOne(t => t.AssignedUser).WithMany().HasForeignKey(t => t.AssignedUserId);
        // global soft-delete filter — Application layer never needs to remember this
        builder.HasQueryFilter(t => t.IsActive);
    }
}
```

**Global Query Filters** — applied per entity in the configuration. Every query automatically excludes `IsActive = false` records. This enforces the schema rule "inactive records must never be returned" at the EF level.

**MySQL / Pomelo setup:**
```csharp
// In Infrastructure DI registration
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 32)),
        mySql => mySql.MigrationsAssembly("Infrastructure")
    ));
```

---

### 4. Presentation — `src/Web`

Entry point. Wires everything together. The only layer allowed to know about both Application and Infrastructure (for DI registration).

**Minimal API Endpoints** — thin, no business logic. Services are injected directly:
```csharp
// Tasks endpoints example
app.MapPost("/api/tasks", async (CreateTaskRequest req, TaskService tasks, CancellationToken ct) =>
    Results.Ok(await tasks.CreateAsync(req, ct)));

app.MapPut("/api/tasks/{id}/move", async (int id, MoveTaskRequest req, TaskService tasks, CancellationToken ct) =>
    Results.Ok(await tasks.MoveTaskAsync(id, req.TargetListId, req.TargetStatusId, req.PreviousStatusId, ct)));
```

**Middleware:**
- JWT auth middleware — validates Bearer tokens on protected routes
- Global error handler — maps `ValidationException` → 400, `NotFoundException` → 404, unhandled → 500

**OpenAPI / Scalar** — auto-generated API docs served at `/scalar`

---

## Key Business Rules — Where They Live

| Rule | Layer | Mechanism |
|---|---|---|
| `IsActive = false` records never returned | Infrastructure | EF global query filters per entity |
| Task move must update `ListId` + `StatusId` together | Application | `TaskService.MoveTaskAsync` — single `SaveChangesAsync` call |
| `TaskStatusHistory` inserted on every status change | Application | `TaskService.MoveTaskAsync` inserts history row in the same transaction |
| `OrderIndex` on Tasks scoped per `ListId` | Application | `TaskService` computes next index for the target list |
| `OrderIndex` on Lists scoped per `BoardId` | Application | `ListService` computes next index for the board |
| Admins see all projects; Users see only their assigned projects | Application | `ProjectService.GetAccessibleProjectsAsync` branches on `currentUser.Role` |
| Soft delete only — never `DELETE` | Application | Delete methods set `IsActive = false`, never `_context.Remove()` |
| Input validation | Application | FluentValidation called in service method before DB work |

---

## Implementation Phases

### Phase 1 — Solution Scaffold

Everything is created manually using the .NET CLI. No starter template is used.

**1. Create the solution and projects**
```bash
# Create solution
dotnet new sln -n ThinkNinjaKanban

# Create the four projects
dotnet new classlib -n Domain        -o src/Domain
dotnet new classlib -n Application   -o src/Application
dotnet new classlib -n Infrastructure -o src/Infrastructure
dotnet new webapi   -n Web           -o src/Web

# Create test projects
dotnet new xunit -n Application.UnitTests        -o tests/Application.UnitTests
dotnet new xunit -n Application.IntegrationTests -o tests/Application.IntegrationTests

# Add all projects to the solution
dotnet sln add src/Domain src/Application src/Infrastructure src/Web
dotnet sln add tests/Application.UnitTests tests/Application.IntegrationTests
```

**2. Wire up project references (enforces the dependency rule)**
```bash
# Application depends on Domain
dotnet add src/Application reference src/Domain

# Infrastructure depends on Application
dotnet add src/Infrastructure reference src/Application

# Web depends on Application (to send commands/queries) and Infrastructure (for DI registration only)
dotnet add src/Web reference src/Application
dotnet add src/Web reference src/Infrastructure

# Test projects reference what they test
dotnet add tests/Application.UnitTests reference src/Application
dotnet add tests/Application.IntegrationTests reference src/Web
```

**3. Install NuGet packages**
```bash
# Application layer
dotnet add src/Application package FluentValidation
dotnet add src/Application package FluentValidation.DependencyInjectionExtensions
dotnet add src/Application package Microsoft.Extensions.Logging.Abstractions
dotnet add src/Application package BCrypt.Net-Next

# Infrastructure layer
dotnet add src/Infrastructure package Microsoft.EntityFrameworkCore
dotnet add src/Infrastructure package Pomelo.EntityFrameworkCore.MySql
dotnet add src/Infrastructure package Microsoft.EntityFrameworkCore.Design

# Web layer
dotnet add src/Web package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/Web package Microsoft.AspNetCore.OpenApi
dotnet add src/Web package Scalar.AspNetCore

# Test project (integration only)
dotnet add tests/Application.IntegrationTests package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/Application.IntegrationTests package FluentAssertions
dotnet add tests/Application.IntegrationTests package Microsoft.EntityFrameworkCore.Sqlite
```

**4. Clean up default boilerplate**
- Delete the placeholder `Class1.cs` from `Domain`, `Application`, and `Infrastructure`
- Delete the default `WeatherForecast.cs` and its controller from `Web`
- In `Web/Program.cs`, strip everything back to a minimal working stub

**5. Add DI registration extension methods** — each layer registers its own services so `Program.cs` stays clean:
```csharp
// Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<TaskService>();
        services.AddScoped<BoardService>();
        services.AddScoped<ListService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<StatusService>();
        services.AddScoped<UserService>();
        services.AddScoped<AuthService>();
        return services;
    }
}

// Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 32)),
                mySql => mySql.MigrationsAssembly("Infrastructure")));
        services.AddScoped<IApplicationDbContext>(p => p.GetRequiredService<ApplicationDbContext>());
        return services;
    }
}

// Web/Program.cs — wire it all together
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

**6. Add common exceptions** in `Application/Common/Exceptions/`:
- `ValidationException` — thrown by service methods when FluentValidation fails; carries field-level errors
- `NotFoundException` — thrown when a requested entity does not exist

Add a global error handling middleware in `Web/Middleware/ExceptionHandlingMiddleware.cs` that maps these to HTTP responses:
- `ValidationException` → 400 with field-level error body
- `NotFoundException` → 404
- Unhandled exception → 500

**7. Verify the build is clean before moving on**
```bash
dotnet build
```

### Phase 2 — Domain
- Define `BaseEntity` (Id, Guid, CreatedById, CreatedOn, UpdatedById, UpdatedOn, IsActive)
- Define all entities: `User`, `Project`, `Board`, `List`, `Task`, `Status`, `ProjectBoard`, `UserProjectAccess`, `TaskStatusHistory`
- Define enumerations: `UserRole`, `TaskPriority`
- Define value object: `HexColor`

### Phase 3 — Infrastructure (Database)
- Implement `ApplicationDbContext` with `IApplicationDbContext`
- Write entity configurations for all tables (one `IEntityTypeConfiguration<T>` file per entity)
- Apply global query filters (`IsActive`) to all BaseEntity-derived configs
- Override `SaveChangesAsync` to auto-set `CreatedOn` / `UpdatedOn` timestamps (no event dispatch)
- Configure Pomelo MySQL connection
- Generate and apply the initial EF Core migration

### Phase 4 — Application (Services)
Write one service class per feature. Each service takes `IApplicationDbContext` via constructor injection. Implement in this order (each builds on the previous):

1. **Statuses** — `StatusService.GetAllAsync` — read-only master data, good warm-up
2. **Auth / Users** — `AuthService.RegisterAsync` (BCrypt hash + save User), `AuthService.LoginAsync` (verify hash, return JWT); `UserService.GetCurrentUserAsync`
3. **Projects** — `ProjectService`: CRUD + `GetAccessibleProjectsAsync` (all records for admins; filtered by `UserProjectAccess` for regular users)
4. **Boards** — `BoardService`: CRUD + `LinkToProjectAsync`
5. **Lists** — `ListService`: CRUD + `ReorderAsync` (updates `OrderIndex` scoped per board)
6. **Tasks** — `TaskService`: `CreateAsync`, `UpdateAsync`, `MoveTaskAsync` (updates `ListId` + `StatusId` + inserts `TaskStatusHistory` in one transaction), `SoftDeleteAsync` (sets `IsActive = false`), `ReorderAsync`
7. **TaskStatusHistory** — `TaskHistoryService.GetByTaskAsync` — query only; writes are handled by `TaskService.MoveTaskAsync`

### Phase 5 — Presentation (API)
- Scaffold Minimal API endpoint groups per feature (one static class per feature, e.g. `TaskEndpoints.cs` with a `Map(WebApplication app)` method)
- Register the global error handling middleware added in Phase 1
- Configure OpenAPI / Scalar
- **Auth strategy:** do not use ASP.NET Core Identity. `AuthService` handles BCrypt verification and JWT generation (~50 lines). For local development, run `dotnet user-jwts create` to generate a valid token instantly — this means you can build and test all other endpoints on day 1 without the login flow being complete.
- Add JWT Bearer authentication + two authorization policies: `AdminOnly` (Role = Admin) and `AuthenticatedUser` (any valid token)

### Phase 6 — Testing
No unit tests. All tests are integration tests using `WebApplicationFactory`.

**Setup:** Override the MySQL DbContext registration in the test `WebApplicationFactory` to use SQLite. Tests run without a real database server — no Docker or MySQL install required on CI.

**Key scenarios to cover:**
- Task move → verify `TaskStatusHistory` row was inserted and `StatusId` on the task updated
- Soft delete → verify the deleted record is excluded from all subsequent GET responses
- Admin project access → verify an admin receives all projects
- User project access → verify a regular user only receives projects they are assigned to
- Validation → verify a 400 with field errors is returned when required fields are missing

---

## NuGet Packages by Layer

| Layer | Package |
|---|---|
| Application | `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`, `BCrypt.Net-Next`, `Microsoft.Extensions.Logging.Abstractions` |
| Infrastructure | `Microsoft.EntityFrameworkCore`, `Pomelo.EntityFrameworkCore.MySql`, `Microsoft.EntityFrameworkCore.Design` |
| Web | `Microsoft.AspNetCore.Authentication.JwtBearer`, `Scalar.AspNetCore`, `Microsoft.AspNetCore.OpenApi` |
| Tests | `xunit`, `Microsoft.AspNetCore.Mvc.Testing`, `FluentAssertions`, `Microsoft.EntityFrameworkCore.Sqlite` |

---

## Folder / Project Structure

```
src/
  Domain/
    Entities/
    Enumerations/
    ValueObjects/
    Common/             ← BaseEntity lives here
  Application/
    Common/
      Interfaces/       ← IApplicationDbContext
      Exceptions/       ← NotFoundException, ValidationException
    Tasks/              ← TaskService.cs, TaskDtos.cs, TaskValidator.cs
    Boards/
    Lists/
    Projects/
    Statuses/
    Users/              ← UserService.cs, AuthService.cs
    DependencyInjection.cs
  Infrastructure/
    Persistence/
      Configurations/   ← one IEntityTypeConfiguration<T> file per entity
      ApplicationDbContext.cs
    DependencyInjection.cs
  Web/
    Endpoints/          ← one static class per feature
    Middleware/         ← ExceptionHandlingMiddleware.cs
    Program.cs
tests/
  Application.IntegrationTests/
```
