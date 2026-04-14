# Database Schema

## Overview

All tables (except `TaskStatusHistory`) inherit from `BaseEntity`. The base fields are not repeated in each table definition — they are implied.

---

## BaseEntity *(inherited by all tables)*

| Field | Type | Notes |
|---|---|---|
| `Id` | INT, PK, AUTO_INCREMENT | Primary key |
| `Guid` | CHAR(36) | UUID, unique, non-null |
| `CreatedById` | INT, FK → `Users.Id` | Who created the record |
| `CreatedOn` | DATETIME | Auto-set on insert |
| `UpdatedById` | INT, FK → `Users.Id` | Who last updated the record |
| `UpdatedOn` | DATETIME | Auto-set on update |
| `IsActive` | BOOLEAN, DEFAULT TRUE | Soft delete flag — never hard delete |

---

## Users

| Field | Type | Notes |
|---|---|---|
| `Email` | VARCHAR(255), UNIQUE | Login username |
| `PasswordHash` | VARCHAR(512) | BCrypt hashed |
| `FirstName` | VARCHAR(100) | |
| `LastName` | VARCHAR(100) | |
| `Role` | ENUM('Admin','User') | Access level |
| `IsVerified` | BOOLEAN, DEFAULT FALSE | Whether the user account has been verified |

---

## Projects

| Field | Type | Notes |
|---|---|---|
| `Name` | VARCHAR(150) | |
| `Description` | TEXT, NULLABLE | |

---

## Boards

| Field | Type | Notes |
|---|---|---|
| `Name` | VARCHAR(150) | |
| `Description` | TEXT, NULLABLE | |

---

## ProjectBoards *(join table — many-to-many)*

| Field | Type | Notes |
|---|---|---|
| `ProjectId` | INT, FK → `Projects.Id` | |
| `BoardId` | INT, FK → `Boards.Id` | |

> Composite PK on `(ProjectId, BoardId)`. Inherits `BaseEntity` so it can be soft-deleted.

---

## Statuses

| Field | Type | Notes |
|---|---|---|
| `Name` | VARCHAR(100) | e.g. "To Do", "In Progress", "Done" |
| `Color` | VARCHAR(7), NULLABLE | Hex color e.g. `#3498db` |
| `OrderIndex` | INT | Display order of columns |

> Statuses are global master data — managed by admins only.

---

## Lists

| Field | Type | Notes |
|---|---|---|
| `Name` | VARCHAR(150) | Column title on the board |
| `BoardId` | INT, FK → `Boards.Id` | Which board this list belongs to |
| `StatusId` | INT, FK → `Statuses.Id` | Tasks in this list share this status |
| `OrderIndex` | INT | Column order on the board, scoped per `BoardId` |

> A `List` is a Kanban column. It belongs to one board and represents one status.

---

## Tasks

| Field | Type | Notes |
|---|---|---|
| `Title` | VARCHAR(255) | |
| `Description` | TEXT, NULLABLE | |
| `BoardId` | INT, FK → `Boards.Id` | Which board the task lives on |
| `ListId` | INT, FK → `Lists.Id` | Which column/list it is currently in |
| `StatusId` | INT, FK → `Statuses.Id` | Mirrors the list's status (denormalised for query ease) |
| `AssignedUserId` | INT, FK → `Users.Id`, NULLABLE | Who the task is assigned to |
| `OrderIndex` | INT | Position within the list, scoped per `ListId` |
| `Priority` | ENUM('Low','Medium','High'), DEFAULT 'Medium' | |
| `DueDate` | DATETIME, NULLABLE | |

> When a task moves to a new `List`, both `ListId` and `StatusId` must be updated together.

---

## UserProjectAccess *(join table — admin-controlled)*

| Field | Type | Notes |
|---|---|---|
| `UserId` | INT, FK → `Users.Id` | |
| `ProjectId` | INT, FK → `Projects.Id` | |

> Controls which users can see which projects. Admins bypass this check and see all projects.

---

## TaskStatusHistory *(append-only audit log)*

| Field | Type | Notes |
|---|---|---|
| `Id` | INT, PK, AUTO_INCREMENT | |
| `TaskId` | INT, FK → `Tasks.Id` | |
| `StatusChangedFrom` | INT, FK → `Statuses.Id`, NULLABLE | Previous status — null on first assignment |
| `StatusChangedTo` | INT, FK → `Statuses.Id` | The new status |
| `ChangedById` | INT, FK → `Users.Id` | Who made the change |
| `ChangedAt` | DATETIME | When the change occurred |

> This table does **not** inherit `BaseEntity`. It is an immutable append-only log — records are never updated or deleted. A row is inserted automatically in the service layer whenever a task's `StatusId` changes.

---

## Relationships

```
Users ─────────────────────────── UserProjectAccess ──── Projects
                                                              │
                                                       ProjectBoards
                                                              │
Users ── Tasks.AssignedUserId     Boards ───────────────── Lists
              │                     │                        │
              │                     └─────── Tasks ──────────┘
              │                                 │
           Statuses ─────────── Lists.StatusId  │
                  └──────────── Tasks.StatusId ─┘
                                     │
                            TaskStatusHistory
```

---

## Key Rules

- **Soft delete only** — set `IsActive = FALSE`, never use `DELETE`
- **Inactive records** must never be returned or selectable — enforced via global EF Core query filters
- **TaskStatusHistory** is insert-only — never modified after creation
- **OrderIndex on Tasks** is scoped per `ListId`
- **OrderIndex on Lists** is scoped per `BoardId`
- When moving a task between lists, always update `ListId` **and** `StatusId` together, then insert a `TaskStatusHistory` record
