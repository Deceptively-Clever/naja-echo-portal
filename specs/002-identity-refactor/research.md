# Research: Identity-Backed Authentication Refactor

**Feature**: `002-identity-refactor` | **Date**: 2026-06-12

This document resolves the technical unknowns for replacing the hand-rolled `UserProfile`
identity with ASP.NET Core Identity while keeping Discord as the only external login provider.
The current implementation (feature `001-discord-auth`) is the starting point; every decision
below is framed as a delta from it.

---

## D1. Local application user model

**Decision**: Introduce a minimal custom `ApplicationUser : IdentityUser<Guid>` in Infrastructure
with two added columns: `DisplayName` and `DiscordUsername`. Use `Guid` as the key type.

**Rationale**:
- The spec requires the current-user response to expose application user ID, display name, and
  Discord username (per clarification 2026-06-12). `IdentityUser<Guid>` already supplies the ID;
  `DisplayName` and `DiscordUsername` are the only non-standard fields, so a 2-property subclass
  is the smallest model that satisfies the contract (Constitution IV — Simplicity).
- `Guid` keys match the existing `UserProfile.Id` shape and the frontend's `z.string().uuid()`
  schema, avoiding a frontend type change.
- Identity's `Email`/`UserName` columns remain available but are not required by this feature;
  `UserName` is set to the Discord provider key to keep it unique and non-null.

**Alternatives considered**:
- *Plain `IdentityUser` (string key)*: rejected — would change the ID type exposed to the
  frontend and break the existing UUID contract for no benefit.
- *Keep `UserProfile` as a separate domain entity linked 1:1 to an `IdentityUser`*: rejected —
  duplicates identity state across two tables, reintroduces the custom repository the refactor is
  meant to retire, and adds a join with no current requirement (YAGNI).

---

## D2. Where the find-or-create-link orchestration lives

**Decision**: Define an Application-owned port `IExternalLoginService` with framework-free inputs
and outputs. Implement it in Infrastructure as `DiscordExternalLoginService` using
`UserManager<ApplicationUser>`. The Application `SignInWithDiscordHandler` calls the port.

```
Application (port + DTOs)            Infrastructure (implementation)
  IExternalLoginService          ->    DiscordExternalLoginService
    FindOrCreateAsync(DiscordProfile) -> LocalUser    (uses UserManager)
    GetByIdAsync(Guid)               -> LocalUser?
  DiscordProfile  (Domain value object, already exists)
  LocalUser       (Application DTO: Id, DisplayName, DiscordUsername)
```

**Rationale**:
- Constitution VI forbids ASP.NET Core Identity dependencies in Domain and discourages
  framework-heavy Identity logic in Application unless behind a port. `UserManager`/`SignInManager`
  are framework types, so they stay in Infrastructure behind `IExternalLoginService`.
- The Application layer keeps the use-case shape (a handler per feature folder) and depends only
  on the port and plain DTOs — preserving the inward-pointing dependency direction.
- The existing `DiscordProfile` value object is already framework-free and lives in Domain; it is
  reused as the port input unchanged.

**Alternatives considered**:
- *Put `UserManager` calls directly in the Application handler*: rejected — pulls
  `Microsoft.AspNetCore.Identity` into the Application project, violating Constitution VI.
- *Do everything in the API callback endpoint*: rejected — auth orchestration is a use case, not
  an HTTP concern; keeping it behind a port keeps it unit-testable without the HTTP pipeline.

---

## D3. External login flow pattern (OAuth → Identity sign-in)

**Decision**: Use the idiomatic Identity external-login pattern. Discord's handler signs the user
into the Identity **external** cookie scheme (`IdentityConstants.ExternalScheme`). A dedicated
callback endpoint reads the external login via `SignInManager.GetExternalLoginInfoAsync()`,
invokes `IExternalLoginService.FindOrCreateAsync`, issues the application cookie via
`SignInManager.SignInAsync(user)`, clears the external cookie, then redirects to the dashboard root.

**Rationale**:
- This is the supported ASP.NET Core Identity path for external providers; it reuses Identity's
  `AspNetUserLogins` table for provider linkage (`AddLoginAsync` / `FindByLoginAsync`) instead of
  hand-managing the provider key.
- A real callback endpoint matches the `/api/auth/discord/callback` already present in the contract
  and is straightforward to exercise in integration tests by stubbing the external login info.
- Replaces the current `OnTicketReceived` principal-rewrite, which bypassed Identity entirely.

**Alternatives considered**:
- *Keep `OnTicketReceived` and call `SignInManager.SignInAsync` inside it*: workable but couples
  sign-in to the OAuth event, is harder to test in isolation, and obscures the linkage step.
  Rejected in favor of the explicit, testable callback.

**Preserved behavior**: OAuth `state`/correlation validation continues to be handled by the
Discord handler's correlation cookie — this refactor does not reimplement it (spec assumption).

---

## D4. Cookie & session policy

**Decision**:
- Application cookie scheme: `IdentityConstants.ApplicationScheme`.
- Cookie name: `__Host-najaecho.auth` in production, `najaecho.auth` in development (the `__Host-`
  prefix requires `Secure` + path `/` + no `Domain`, which holds in production but not over plain
  HTTP in dev).
- `HttpOnly = true`; `SameSite = Lax`; `SecurePolicy = Always` in production, `None` in dev.
- `ExpireTimeSpan = 24 hours` (the idle timeout), `SlidingExpiration = true` (renews on activity).
- Absolute 7-day cap enforced in `OnValidatePrincipal`: compare `properties.IssuedUtc` to now and
  reject sessions older than 7 days regardless of sliding renewal.

**Rationale**:
- Maps the spec clarification (7-day sliding window, 24-hour idle timeout) onto cookie auth:
  `ExpireTimeSpan` + `SlidingExpiration` gives the 24h idle behavior; sliding alone has no absolute
  ceiling, so the 7-day cap is added explicitly in the validation event.
- Satisfies Constitution security rules (HttpOnly, Secure-in-prod, SameSite=Lax, `__Host-` prefix
  in prod) and the spec's security requirements.

**Alternatives considered**:
- *Persisted server-side sessions (ticket store)*: rejected — adds storage and a moving part with
  no current requirement; the absolute-cap check in `OnValidatePrincipal` meets the policy without
  it (YAGNI). Noted as a future option if server-side revocation becomes a requirement.

---

## D5. Discord tokens & scopes

**Decision**: Set `SaveTokens = false`. Minimize scope to `identify` only (drop `email`). No
Discord access/refresh tokens are stored anywhere.

**Rationale**:
- The feature never calls Discord APIs on the user's behalf, so storing tokens (currently
  `SaveTokens = true`, which persists them in the auth cookie) is unnecessary attack surface. The
  spec forbids token storage unless a current requirement is documented; there is none.
- Constitution and spec require scope minimization. `identify` yields the Discord user ID,
  username, and global name — everything the current-user response needs. `email` was carried by
  feature 001 but is not required by this feature's data model.

**Impact**: The `Email` column on the user becomes unused for now; it remains available (Identity
provides it) for future features without a migration.

---

## D6. `/api/auth/me` response shape (200 discriminated)

**Decision**: `/api/auth/me` becomes `AllowAnonymous` and always returns `200 OK`. Body is a
discriminated union: `{ "authenticated": true, "user": { id, displayName, discordUsername } }`
when a session exists, or `{ "authenticated": false }` when it does not. `401` is reserved for
other protected action endpoints.

**Rationale**:
- Directly implements the spec clarification. The current endpoint returns `401` when
  unauthenticated, which conflicts with the clarified contract and forces the frontend to treat a
  normal "logged out" state as an error.
- The frontend auth guard parses `authenticated` instead of catching a 401, simplifying the query
  hook (no error-as-state handling).

**Impact**:
- Removes the `avatarUrl` field from the response (the spec clarification fixed the field set to
  ID + display name + Discord username, no avatar). The frontend `UserBadge` already renders a
  fallback initial when no avatar is present, so it degrades cleanly. This is an intentional,
  spec-driven change, not an omission.
- Updates the existing test `Me_Returns401_WhenUnauthenticated` to expect `200` +
  `{ authenticated: false }`.

---

## D7. Persistence & migration strategy

**Decision**: Convert `AppDbContext` to `IdentityDbContext<ApplicationUser, IdentityRole<Guid>,
Guid>`. Add one forward-only migration `AddIdentitySchema` that creates the Identity tables
(`asp_net_users`, `asp_net_roles`, `asp_net_user_logins`, `asp_net_user_claims`,
`asp_net_user_roles`, `asp_net_user_tokens`, `asp_net_role_claims`) and **drops** the now-unused
`user_profiles` table. Snake-case naming convention is retained.

**Rationale**:
- The spec assumption states there is no production data to preserve and no requirement to migrate
  pre-existing custom user records. With no data, dropping `user_profiles` is safe.
- Identity's `AspNetUserLogins` replaces the custom `discord_user_id` unique index for provider
  linkage; `AspNetUserRoles`/`AspNetRoleClaims` satisfy FR-013 (future-ready roles/claims) at zero
  extra modeling cost because `IdentityDbContext` includes them by default.

**Forward-only / destructive note**: Dropping `user_profiles` is a destructive step. Per the
Development Workflow, destructive migrations require explicit approval recorded in the PR. Because
the table holds no production data (created only in the `001` initial migration on a not-yet-shipped
schema), this is low risk and will be called out in the PR description for sign-off. No `Down`
reliance is assumed (migrations are forward-only).

**Alternatives considered**:
- *Keep `user_profiles` alongside Identity tables*: rejected — leaves a dead table and the retired
  repository in the codebase, contradicting the "Identity is the source of truth" goal.

---

## D8. Frontend types from the OpenAPI contract

**Decision**: Generate TypeScript types for the auth/session responses from the updated OpenAPI
contract using `openapi-typescript` (dev dependency), output to `src/lib/api/schema.d.ts`. Keep a
Zod schema in the auth feature **only** as a runtime validator at the fetch boundary, typed to
conform to the generated type. Centralize auth query keys in a typed key factory.

**Rationale**:
- Constitution (Frontend Conventions) requires request/response types to be generated from the
  OpenAPI contract and forbids hand-written duplicate DTOs. The current `currentUserSchema.ts` is a
  hand-written DTO and must be reconciled: the generated type becomes the source of truth for the
  shape; Zod remains for runtime parsing (a validation concern, not a duplicate DTO).
- TanStack Query is already in use; adding a typed key factory (`authKeys`) satisfies the
  "centralized, typed key factories" rule and replaces the inline `['auth','me']` literals.

**Alternatives considered**:
- *Keep hand-written Zod schema as the only type*: rejected — violates the generated-types rule.
- *Drop Zod entirely and trust generated types*: rejected — loses runtime validation at the
  network boundary; the discriminated `authenticated` union benefits from a runtime check.

---

## D9. CORS, credentials, and dev-vs-prod cookie risks

**Decision**: Keep the existing named CORS policy (`Frontend` origin, `AllowCredentials`). Frontend
fetches must send `credentials: 'include'` (already done in `apiClient`). Document the dev-vs-prod
cookie matrix in quickstart.

**Identified risks & mitigations**:
- **`__Host-` prefix over HTTP in dev**: the prefix mandates `Secure`, which browsers reject on
  plain `http://localhost`. Mitigation: use the unprefixed `najaecho.auth` name in development,
  `__Host-najaecho.auth` only in production (D4).
- **Discord correlation cookie SameSite on callback**: the OAuth correlation cookie must survive
  the top-level redirect back from `discord.com`. `SameSite=Lax` permits it for top-level GET
  navigations (the callback is a GET), so the existing Lax setting is correct; `SameSite=None`
  would require `Secure` and is unnecessary. Documented as a validation step.
- **SPA cross-origin credentials**: dev runs the SPA on `:5173` and the API behind the proxy;
  `AllowCredentials` + explicit origin (not `*`) is required and already configured.
- **`X-Forwarded-Proto`**: already trusted via `UseForwardedHeaders` so `Secure` cookies are
  issued correctly behind the reverse proxy. Retained.

---

## Summary of deltas from feature 001

| Area | 001 (current) | 002 (this refactor) |
|------|---------------|---------------------|
| User store | custom `UserProfile` + `IUserRepository` | `ApplicationUser : IdentityUser<Guid>` via Identity |
| Provider linkage | `discord_user_id` unique index | `AspNetUserLogins` (`FindByLoginAsync`/`AddLoginAsync`) |
| Sign-in | `OnTicketReceived` principal rewrite | `SignInManager` external scheme + callback endpoint |
| Orchestration | `SignInWithDiscordHandler` + repo | `SignInWithDiscordHandler` + `IExternalLoginService` port |
| `/api/auth/me` (unauth) | `401` problem+json | `200` `{ authenticated: false }` |
| Response fields | id, displayName, avatarUrl | id, displayName, discordUsername |
| Tokens | `SaveTokens = true` | `SaveTokens = false` |
| Scopes | `identify`, `email` | `identify` |
| Session | 14-day sliding | 24h idle sliding, 7-day absolute cap |
| Cookie name | `najaecho.auth` | `__Host-najaecho.auth` (prod) / `najaecho.auth` (dev) |
| Roles/claims | none | Identity role tables present (not enforced) |
| FE types | hand-written Zod DTO | generated from OpenAPI + Zod runtime guard |
