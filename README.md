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
# Restore dependencies
dotnet restore

# Apply database migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Web

# Run the API
dotnet run --project src/Web
```

API docs are served at `/scalar` when running locally.
