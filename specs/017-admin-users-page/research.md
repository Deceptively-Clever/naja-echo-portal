# Research: Admin Users Page

**Feature**: `017-admin-users-page` | **Date**: 2026-06-21

All open questions in the spec were resolved during the 2026-06-21 clarification session (see
`spec.md` → Clarifications). This document records the **technical** decisions reached by inspecting
the existing codebase, since this feature is dominated by reuse of features 002 (identity) and 015
(character registration / RSI scraping).

## Decision 1 — No new database schema or migration

- **Decision**: Add **no** tables, columns, or EF Core migration.
- **Rationale**: Every entity the feature touches already exists:
  - `AspNetUsers` (`ApplicationUser` with `DisplayName`, `DiscordUsername`) — feature 002.
  - `AspNetRoles` / `AspNetUserRoles` seeded with `Admin` and `Quartermaster` — `RoleSeeder`.
  - `characters` (`Domain/Characters/Character.cs`) with `owner_user_id` FK → `AspNetUsers`,
    a functional unique index `ux_characters_handle_lower`, and `ix_characters_owner_user_id` —
    feature 015.
  The admin "add character" action creates an ordinary `Character` row whose `OwnerUserId` is the
  **target** member rather than the caller. No structural change is needed.
- **Alternatives considered**: A dedicated read-model/view for the users table — rejected (YAGNI;
  a single joined query suffices for the small roster, per the spec's no-pagination assumption).

## Decision 2 — Reuse `IRsiCitizenClient`; new admin handler skips token verification

- **Decision**: Reuse `IRsiCitizenClient.FetchCitizenAsync(handle)` verbatim. Write a **new**
  `AddCharacterForUserHandler` that does **not** require a `PendingCharacterRegistration` token and
  does **not** check the RSI page for an ownership token.
- **Rationale**: `VerifyCharacterHandler` (self-registration, feature 015) enforces token presence
  (`content.Contains(pending.Token)`) to prove the *caller* owns the RSI account. The spec
  (Assumptions; User Story 2) states the admin flow skips token generation and ownership
  verification — admin authority is the sole authorization. So the handler reuses the RSI fetch,
  the handle-already-claimed check (`ICharacterRepository.HandleExistsAsync`), and the persistence
  call (`ICharacterRepository.AddAsync`), but omits the pending-token machinery entirely.
- **RSI client return shape** (verified): `FetchCitizenAsync` returns `object` resolving to one of
  `RsiCitizenPage(Content, DisplayName)`, `RsiProfileNotFound`, or `RsiUnreachable`. The admin
  handler maps these to FR-007 (not found), FR-008 (unreachable), and — for `RsiCitizenPage` with a
  **null/blank `DisplayName`** — FR-009 (name could not be retrieved). This is the key behavioural
  difference from self-registration, where a blank moniker falls back to the handle as the name.

## Decision 3 — Four distinct Add-Character failure modes

- **Decision**: Map the four spec failure modes to distinct HTTP problem responses, mirroring the
  status-code conventions already used by `CharacterEndpoints.VerifyCharacter`:
  | Condition | Spec | Exception | HTTP |
  |-----------|------|-----------|------|
  | Empty/whitespace handle | FR (US2 #5) | (validated at edge) | 400 |
  | Handle already registered to any user | FR-006 | `HandleAlreadyClaimedException` (reused) | 409 |
  | RSI citizen page not found | FR-007 | `RsiProfileNotFoundException` (reused) | 404 |
  | RSI unreachable / unusable response | FR-008 | `RsiUnreachableException` (reused) | 502 |
  | RSI 200 but no name extractable | FR-009 | `CharacterNameUnavailableException` (**new**) | 422 |
  | Target user does not exist | (integrity) | `UserNotFoundException` (**new**) | 404 |
  | RSI handle with invalid characters per RSI rules | (edge case) | falls through to `RsiProfileNotFoundException` | 404 |

  The invalid-character-handle edge case requires no additional input validation beyond the contract's
  `minLength: 1` constraint: the RSI API will return 404 / an unusable response for any handle it
  cannot resolve, and the existing FR-007 / FR-008 paths already surface the appropriate error to the
  admin. No separate exception or task is needed.

- **Rationale**: Reuses the existing exception types from `Features/Characters/VerifyCharacter`
  where the semantics match exactly; adds only the two genuinely new conditions. Distinct statuses
  let the frontend show the precise message each FR mandates.

## Decision 4 — Users list via one joined read query

- **Decision**: Add a read method that returns every `AspNetUsers` row with its roles and its
  characters in one round trip, projected to an application DTO. Follow the raw-SQL precedent in
  `MaterialInventoryRepository` (joins `AspNetUsers` / `AspNetUserRoles` / `AspNetRoles`).
- **Rationale**: The roster is small (spec assumption — load all, no pagination). A single query
  with `LEFT JOIN`s to roles and characters avoids N+1 and returns empty role/character sets as
  empty collections (FR-010). EF `Include` is also viable but the identity join-table is not mapped
  as a navigation here, so the established raw-SQL grouping pattern is the lower-friction choice.
- **Alternatives considered**: `UserManager.GetRolesAsync` per user (N+1) — rejected for the same
  reason feature 014 chose the raw join.

## Decision 5 — Friendly role names are a frontend display concern

- **Decision**: The backend returns **raw** role names (`Admin`, `Quartermaster`). The frontend
  maps them to friendly labels via a small static map in the admin feature
  (`Admin` → "Administrator", `Quartermaster` → "Quartermaster"), satisfying FR-011.
- **Rationale**: The API already exposes raw role strings everywhere (`CurrentUserResponse.Roles`);
  friendly naming is pure presentation. Keeping the map client-side avoids API churn and matches the
  spec assumption ("a static map within the application"). Unknown roles fall back to the raw value.
- **Alternatives considered**: Returning friendly names from the API — rejected (couples a display
  concern to the contract; would duplicate the existing raw-role exposure).

## Decision 6 — Client-side filtering, single combined input

- **Decision**: Load the full users list once via TanStack Query and filter **client-side** with a
  single text input that matches across auth name **or** any character name/handle **or** any role
  (friendly name), per FR-003. No server-side search endpoint.
- **Rationale**: Spec assumption — roster small enough to load and filter in-browser without
  pagination (SC-001, SC-002). Mirrors the in-memory filtering already used by the warehouse
  filter components.

## Decision 7 — Route path and navigation

- **Decision**: Mount the page at **`/dashboard/admin/users`** under the existing `AdminRoute`
  guard, and add a single data-driven `navItems` entry (`access: 'admin'`, `group: 'Admin'`).
- **Rationale**: The spec refers to the section as `/admin/users`, but every existing admin route is
  nested under `/dashboard/admin/...` inside `AdminRoute` (e.g. `/dashboard/admin/data-import`).
  Aligning keeps one admin-guard nesting and one nav model (constitution: data-driven navigation,
  single source of truth). The conceptual section name in the spec is preserved; only the concrete
  path is normalized. Access control is enforced both client-side (`AdminRoute` +
  `access: 'admin'`) and server-side (`AuthorizationPolicies.Admin`) — the server gate is
  authoritative (FR-001, SC-005).

## Decision 8 — No new dependencies

- **Decision**: Introduce no new backend or frontend package.
- **Rationale**: RSI fetch (`IRsiCitizenClient` + `AngleSharp`), identity/roles, TanStack Query,
  React Hook Form + Zod, and the shadcn `dialog`/`table`/`input` primitives all already exist.
