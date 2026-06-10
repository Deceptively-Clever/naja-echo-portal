# Phase 1 Data Model: Discord Authentication

**Feature**: 001-discord-auth | **Date**: 2026-06-08

## Entities

### UserProfile

Represents a local application user, one-to-one with a Discord account.

| Field             | Type         | Required | Notes                                                          |
|-------------------|--------------|----------|----------------------------------------------------------------|
| `Id`              | `Guid`       | yes      | Primary key. Generated server-side. Stable internal identifier. |
| `DiscordUserId`   | `string(20)` | yes      | Unique. Discord snowflake (numeric string). Indexed for lookup. |
| `DisplayName`     | `string(64)` | yes      | Discord global display name, falling back to username.          |
| `AvatarRef`       | `string(64)` | no       | Discord avatar hash or full URL. Null if user has no avatar.    |
| `Email`           | `string(254)`| no       | Only set when `email` scope granted AND Discord marks verified. |
| `CreatedAtUtc`    | `timestamptz`| yes      | Set on first insert. Never updated.                             |
| `LastLoginAtUtc`  | `timestamptz`| yes      | Updated on every successful login.                              |
| `LastUpdatedAtUtc`| `timestamptz`| yes      | Updated when any non-`LastLogin` field changes.                 |

**Constraints**:
- Unique index on `DiscordUserId` — enforces FR-007 (no duplicate local users for same Discord account).
- All timestamps stored as UTC (`timestamptz`); domain layer uses `DateTimeOffset`.
- `Email` MUST NOT be inferred from any source other than Discord's verified email.

**State transitions**:

```text
(none)
  │
  │ first successful Discord login
  ▼
ACTIVE  ──┐
  ▲       │ subsequent successful login → updates LastLoginAtUtc; updates DisplayName/AvatarRef/Email
  └───────┘                                  if changed → updates LastUpdatedAtUtc
```

No soft-delete in v1. No status enum — presence of the row means "active."

**Domain rules**:
- `UserProfile.RecordLogin(DiscordProfile current, IClock clock)` is the single mutation entry
  point. It updates `LastLoginAtUtc`, applies changes to `DisplayName` / `AvatarRef` / `Email`
  when current values differ, and bumps `LastUpdatedAtUtc` only if non-login fields changed.
- `UserProfile.CreateFromDiscord(DiscordProfile profile, IClock clock)` is the factory used on
  first login.

### DiscordProfile (value object — Application layer)

Snapshot of the fields read from Discord's `/users/@me` after a successful token exchange.

| Field         | Type     | Required | Source                            |
|---------------|----------|----------|------------------------------------|
| `Id`          | `string` | yes      | Discord `id` (snowflake).          |
| `Username`    | `string` | yes      | Discord `username`.                |
| `GlobalName`  | `string` | no       | Discord `global_name` (display).   |
| `Avatar`      | `string` | no       | Discord `avatar` hash.             |
| `Email`       | `string` | no       | Discord `email` (only with scope). |
| `Verified`    | `bool`   | no       | Discord `verified` flag.           |

Derived rule: `DisplayName = GlobalName ?? Username`.
Derived rule: `Email` is admissible only when present **and** `Verified == true`.

### AuthSession

Represented by ASP.NET Core's cookie authentication. No bespoke entity persisted in v1.

- Cookie name: `__Host-najaecho.auth` (prod) / `najaecho.auth` (dev).
- Claims stored: `sub` (UserProfile.Id as string), `name` (DisplayName).
- Lifetime: 14 days sliding.
- `HttpOnly=true`, `Secure=true` (prod), `SameSite=Lax`.

Discord access/refresh tokens are NOT persisted (per research §4) and NOT placed in the cookie or
session claims.

### AuthorizationAttempt

Handled by the ASP.NET OAuth handler's correlation cookie. No bespoke storage:
- `state` and PKCE verifier (if enabled) live in a short-lived `.AspNetCore.Correlation.Discord`
  cookie scoped to `/api/auth/discord/callback`.
- Validated and discarded by the OAuth handler before the application sees the callback.

## Schema (PostgreSQL via EF Core)

```sql
CREATE TABLE user_profiles (
    id                  UUID PRIMARY KEY,
    discord_user_id     VARCHAR(20) NOT NULL,
    display_name        VARCHAR(64) NOT NULL,
    avatar_ref          VARCHAR(64) NULL,
    email               VARCHAR(254) NULL,
    created_at_utc      TIMESTAMPTZ NOT NULL,
    last_login_at_utc   TIMESTAMPTZ NOT NULL,
    last_updated_at_utc TIMESTAMPTZ NOT NULL,
    CONSTRAINT uq_user_profiles_discord_user_id UNIQUE (discord_user_id)
);

CREATE INDEX ix_user_profiles_discord_user_id ON user_profiles (discord_user_id);
```

Naming convention: snake_case columns (Npgsql `UseSnakeCaseNamingConvention`).

## Repository contract

```csharp
public interface IUserRepository
{
    Task<UserProfile?> FindByDiscordUserIdAsync(string discordUserId, CancellationToken ct);
    Task AddAsync(UserProfile user, CancellationToken ct);
    Task<UserProfile?> FindByIdAsync(Guid id, CancellationToken ct);
}
```

`IUnitOfWork.SaveChangesAsync(CancellationToken)` commits pending changes after a successful
login flow.
