# Data Model: Admin Users Page

**Feature**: `017-admin-users-page` | **Date**: 2026-06-21

> **No schema change.** This feature introduces **no new tables, columns, indexes, or EF Core
> migration.** Every persistent entity below already exists. The model here documents the existing
> shapes the feature reads/writes and the **read DTOs** assembled for the API response.

## Existing persistent entities (unchanged)

### ApplicationUser → `AspNetUsers` (feature 002)

| Field | Column | Notes |
|-------|--------|-------|
| `Id` (Guid) | `id` | PK |
| `DisplayName` (string, ≤64) | `display_name` | Discord display name shown as "auth name" |
| `DiscordUsername` (string, ≤32) | `discord_username` | |
| (Identity base fields) | … | `UserName`, `Email`, etc. |

Roles via the standard Identity join: `AspNetUserRoles` (`user_id`, `role_id`) → `AspNetRoles`
(`id`, `name`). Seeded role names: `Admin`, `Quartermaster` (`RoleSeeder`).

### Character → `characters` (feature 015)

| Field | Column | Notes |
|-------|--------|-------|
| `Id` (Guid) | `id` | PK |
| `OwnerUserId` (Guid) | `owner_user_id` | FK → `AspNetUsers.id`, `OnDelete.Cascade`, indexed |
| `Name` (string, ≤100) | `name` | Scraped RSI display name |
| `Handle` (string, ≤100) | `handle` | Unique (functional index `ux_characters_handle_lower`) |
| `CreatedAt` (DateTimeOffset) | `created_at` | |

The admin "add character" action inserts a `Character` whose `OwnerUserId` is the **target member**.
The existing case-insensitive unique handle index enforces FR-006 at the database level (defence in
depth behind the `HandleExistsAsync` pre-check).

## Read DTOs (Application layer — new, in-memory only)

Assembled by the users-list query handler; serialized by the API contract (see `contracts/openapi.yaml`).

### `AdminUserDto`

| Field | Type | Source | Notes |
|-------|------|--------|-------|
| `Id` | Guid | `AspNetUsers.id` | |
| `AuthName` | string | `AspNetUsers.display_name` | Discord auth name (FR-002) |
| `Roles` | `IReadOnlyList<string>` | `AspNetRoles.name` | **Raw** role names; may be empty (FR-010) |
| `Characters` | `IReadOnlyList<AdminUserCharacterDto>` | `characters` | May be empty (FR-003 #3, FR-010) |

### `AdminUserCharacterDto`

| Field | Type | Source |
|-------|------|--------|
| `Id` | Guid | `characters.id` |
| `Name` | string | `characters.name` |
| `Handle` | string | `characters.handle` |

> Friendly role labels (FR-011) are **not** part of this DTO — they are applied client-side
> (research Decision 5).

## Command (Application layer — new, no persistence shape change)

### `AddCharacterForUserCommand`

| Field | Type | Validation |
|-------|------|------------|
| `TargetUserId` | Guid | Must reference an existing `AspNetUsers` row → else `UserNotFoundException` (404) |
| `Handle` | string | Trimmed; non-empty (400); not already claimed (409); RSI-verifiable (404/502/422) |

Produces an `AdminUserCharacterDto` (the created character) on success.

## State / validation rules

- **Add Character** preconditions, evaluated in order (research Decision 3):
  1. `Handle` non-empty after trim — else 400.
  2. `TargetUserId` exists — else 404 (`UserNotFoundException`).
  3. `HandleExistsAsync(handle)` false — else 409 (`HandleAlreadyClaimedException`).
  4. RSI fetch resolves to `RsiCitizenPage` — else 404 (`RsiProfileNotFound`) / 502 (`RsiUnreachable`).
  5. `RsiCitizenPage.DisplayName` non-blank — else 422 (`CharacterNameUnavailableException`, FR-009).
  6. Persist `Character { OwnerUserId = TargetUserId, Name = DisplayName.Trim()[..100], Handle, CreatedAt = now }`.
- **List** returns all users regardless of role/character count; empty collections are valid (FR-010).
