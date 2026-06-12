# Data Model: Identity-Backed Authentication Refactor

**Feature**: `002-identity-refactor` | **Date**: 2026-06-12

Persistence moves from the custom `user_profiles` table to ASP.NET Core Identity's schema.
Identity owns the local application user, external login linkage, and the (future-ready) role and
claim tables. All tables use the existing snake-case naming convention.

---

## Entities

### ApplicationUser (Infrastructure — `ApplicationUser : IdentityUser<Guid>`)

The local, application-owned identity record. Source of truth for a signed-in person.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `Guid` (PK) | Identity-provided. Exposed to the frontend as the user's UUID. |
| `UserName` | `string` | Set to the Discord provider key (Discord user ID) to guarantee uniqueness; not surfaced to the client. |
| `NormalizedUserName` | `string` | Identity-managed. |
| `DisplayName` | `string` (≤64) | From Discord `global_name`, falling back to `username` when null (spec clarification). |
| `DiscordUsername` | `string` (≤32) | Discord unique handle (`username`). Surfaced in the current-user response. |
| `Email` / `EmailConfirmed` | `string?` / `bool` | Identity-provided; unused this feature (scope minimized to `identify`). Available for future use. |
| `SecurityStamp` / `ConcurrencyStamp` | `string` | Identity-managed. |
| *(other `IdentityUser` columns)* | — | `PhoneNumber`, `LockoutEnd`, `AccessFailedCount`, etc. — Identity defaults, unused. |

**Validation rules**:
- `DisplayName` required, non-empty, ≤64 chars (matches prior `user_profiles.display_name`).
- `DiscordUsername` required, ≤32 chars (Discord's max handle length).
- Creation is only ever triggered by a successful Discord external login (no manual provisioning).

**Lifecycle**:
1. **Created** on first Discord login (no existing `AspNetUserLogins` row for the provider key).
2. **Updated** on subsequent logins if `DisplayName` or `DiscordUsername` changed at Discord.
3. Never deleted by this feature (no account-deletion flow in scope).

---

### External Login Link (Identity — `AspNetUserLogins`)

Standard Identity table tying an `ApplicationUser` to an external provider. Replaces the custom
`discord_user_id` unique index.

| Field | Type | Notes |
|-------|------|-------|
| `LoginProvider` | `string` (PK) | `"Discord"`. |
| `ProviderKey` | `string` (PK) | The Discord user ID. |
| `ProviderDisplayName` | `string?` | `"Discord"`. |
| `UserId` | `Guid` (FK → ApplicationUser) | The linked local user. |

**Uniqueness rule**: `(LoginProvider, ProviderKey)` is the composite primary key — the database
guarantees one local user per Discord identity, preventing duplicate accounts (FR-004). Lookups
use `UserManager.FindByLoginAsync("Discord", providerKey)`.

---

### Roles & Claims (Identity — future-ready, not enforced)

Included automatically by `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` to
satisfy FR-013. No roles are created and no role-based authorization is enforced in this feature.

| Table | Purpose |
|-------|---------|
| `AspNetRoles` | Role definitions (empty initially). |
| `AspNetUserRoles` | User↔role assignments (empty initially). |
| `AspNetRoleClaims` | Claims attached to roles. |
| `AspNetUserClaims` | Claims attached directly to users. |
| `AspNetUserTokens` | External/auth tokens — **left empty**; `SaveTokens = false` so no Discord tokens are written here. |

---

## Application-layer DTOs (framework-free, cross the port boundary)

These keep Identity types out of the Application and API layers.

### `DiscordProfile` (Domain value object — reused unchanged from feature 001)

`Id`, `Username`, `GlobalName?`, `Avatar?`, `Email?`, `Verified`; computed `DisplayName =>
GlobalName ?? Username`. Input to `IExternalLoginService.FindOrCreateAsync`.

### `LocalUser` (Application DTO — port output)

| Field | Type |
|-------|------|
| `Id` | `Guid` |
| `DisplayName` | `string` |
| `DiscordUsername` | `string` |

Returned by `IExternalLoginService.FindOrCreateAsync` and `GetByIdAsync`; mapped by the API to the
`CurrentUser` contract schema.

---

## Port: `IExternalLoginService` (Application)

```
LocalUser   FindOrCreateAsync(DiscordProfile profile, CancellationToken ct)
LocalUser?  GetByIdAsync(Guid userId, CancellationToken ct)
```

- `FindOrCreateAsync`: look up `AspNetUserLogins` by `("Discord", profile.Id)`; if found, load the
  user and refresh `DisplayName`/`DiscordUsername` if changed; if not found, create the
  `ApplicationUser`, persist it, and add the external login link. Returns the resulting `LocalUser`.
- `GetByIdAsync`: load the user by primary key for the current-user endpoint; null if absent.

Implemented in Infrastructure by `DiscordExternalLoginService` using `UserManager<ApplicationUser>`.

---

## Removed / retired

| Item | Disposition |
|------|-------------|
| `user_profiles` table | Dropped in the `AddIdentitySchema` migration (no production data). |
| `UserProfile` domain entity | Retired; identity state now lives in `ApplicationUser`. |
| `IUserRepository` / `UserRepository` | Removed; `UserManager` replaces it for auth. |
| `IUnitOfWork` / `EfUnitOfWork` | Removed for the auth feature; `UserManager` persists internally. Re-introduce later only if a non-Identity feature needs an explicit unit of work. |
| `uq_user_profiles_discord_user_id` index | Replaced by the `AspNetUserLogins` composite key. |
