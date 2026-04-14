ENV_FILE := .env.local
INFRA_PROJECT := src/Infrastructure
WEB_PROJECT := src/Web

include $(ENV_FILE)
export

# Apply all pending migrations to the database
migrate:
	dotnet ef database update --project $(INFRA_PROJECT) --startup-project $(WEB_PROJECT)

# Create a new migration: make migration name=YourMigrationName
migration:
	@[ "$(name)" ] || { echo "Usage: make migration name=YourMigrationName"; exit 1; }
	dotnet ef migrations add $(name) --project $(INFRA_PROJECT) --startup-project $(WEB_PROJECT)

# Remove the last migration (only if not yet applied to the DB)
migration-undo:
	dotnet ef migrations remove --project $(INFRA_PROJECT) --startup-project $(WEB_PROJECT)

build:
	dotnet build

run:
	dotnet run --project $(WEB_PROJECT)

.PHONY: migrate migration migration-undo build run
