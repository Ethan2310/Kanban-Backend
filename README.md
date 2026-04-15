# ThinkNinja Kanban — Backend

REST API backend for the ThinkNinja Kanban application. Built with .NET 8, ASP.NET Core Minimal APIs, Entity Framework Core 8, and MySQL.

## Architecture

Clean Architecture with four layers: Domain, Application, Infrastructure, and Web (Presentation). Dependencies point inward — outer layers depend on inner layers, never the reverse.

## Documentation

- **Project & API documentation:** [ThinkNinja Docs](https://app.affine.pro/workspace/512856ec-1b1f-4f5e-87a8-2e7b80ad816d/ZtZCbRcNnKslsey6EQVxM)
- **Database design documentation:** [DB Schema & Design](https://app.affine.pro/workspace/512856ec-1b1f-4f5e-87a8-2e7b80ad816d/H7d0HuHjHK)

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 8 (LTS) |
| ORM | Entity Framework Core 8 |
| Database | MySQL 8.0.32+ |
| EF MySQL driver | Pomelo.EntityFrameworkCore.MySql |
| API style | ASP.NET Core Minimal APIs |
| Validation | FluentValidation |
| Auth | JWT Bearer |
| API docs | Scalar / OpenAPI |
| Testing | xUnit + WebApplicationFactory + SQLite |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL 8.0.32+

### First-time setup

After cloning, run this once to enable the pre-commit formatting hook:

```bash
git config core.hooksPath .githooks
```

This will automatically run `dotnet format` before every commit so formatting issues are fixed locally before they reach CI.

### Run locally

```bash
# Apply any pending database migrations
make migrate

# Start the API (restores dependencies then runs the server)
make run
```

API docs are served at `http://localhost:5081/scalar/v1` when running locally.

## Running Migrations

### Apply pending migrations

```bash
make migrate
```

### Create a new migration (after changing an entity)

```bash
make migration name=YourMigrationName
```

This generates a new file in `src/Infrastructure/Migrations/`. Before applying, open the generated `<timestamp>_YourMigrationName.cs` file and review the `Up()` method — pay close attention to any `DropColumn` or `DropTable` calls, as these are destructive and cannot be undone once applied against live data.

Once satisfied, apply it:

```bash
make migrate
```

## API Docs

With the server running, the following are available:

| URL | Description |
|---|---|
| `http://localhost:5081/scalar/v1` | Interactive Scalar UI — browse and call endpoints in the browser |
| `http://localhost:5081/swagger/v1/swagger.json` | Raw OpenAPI JSON spec |

To download the spec to a local file:

```bash
make openapi-spec
```

This saves the spec to `openapi.json` in the project root.

## Postman

### Import endpoints automatically

1. Start the server (`make run`)
2. In Postman: **Import → Link** → paste `http://localhost:5081/swagger/v1/swagger.json`
3. Postman generates a full collection from the spec — re-import whenever new endpoints are added

### Set up a local environment

1. In Postman: **Environments → Add** → name it `Local`
2. Add variable: `baseUrl` = `http://localhost:5081`
3. All requests should use `{{baseUrl}}/api/...` as the URL
4. When deployed later, create a `Production` environment with the live URL and switch with one click

### Auto-capture the JWT token after login

On the `POST /api/auth/login` request, add the following to the **Tests** tab:

```js
const token = pm.response.json().token;
pm.environment.set("authToken", token);
```

Then on the Collection → **Authorization** tab, set type to `Bearer Token` with value `{{authToken}}`. All requests in the collection will inherit it — log in once and all protected endpoints are authorized automatically.
