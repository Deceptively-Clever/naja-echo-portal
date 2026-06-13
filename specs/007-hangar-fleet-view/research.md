# Phase 0 Research: Hangar

All Technical Context unknowns are resolved below. Each item: Decision / Rationale /
Alternatives considered.

## R1. Catalog source of truth and ownership key

- **Decision**: Use the existing `sc.ships` table (feature 006) as the sole catalog. **Do not
  create a new ship catalog table.** Hangar ownership references **`sc.ships.id` (`Guid`)**, the
  catalog primary key.
- **Rationale**: Inspected `ShipConfiguration` — `HasKey(s => s.Id)`, `id` column is the PK;
  `uex_id` is a unique business key but is mutable feed identity and `int`-typed. `Id` is the
  stable internal surrogate already used by `GetShipById`. Referencing it keeps a clean FK and
  avoids leaking UEX numbering into ownership. Satisfies the spec requirement to verify the
  actual key column before defining the ownership relationship.
- **Alternatives considered**: FK on `uex_id` (rejected: it is a source-system identifier, not
  the table's PK, and would couple ownership to import semantics); a new catalog table (rejected:
  explicitly forbidden and would duplicate 006).

## R2. Where `url_photo`, `scu`, `crew` live and how to read them

- **Decision**: `name` and `company_name` are promoted columns read directly. `url_photo`,
  `scu`, and `crew` are **keys inside `raw_data` (`jsonb`)** and are extracted server-side using
  Npgsql JSON operators: `raw_data->>'url_photo'`, `raw_data->>'scu'`, `raw_data->>'crew'`.
  Extraction happens in the read query/repository so the API returns flat card DTO fields.
- **Rationale**: 006 stores the verbatim UEX feed (all 64 fields) in `raw_data`; only
  name/company/uuid/name_full/status are promoted. `crew` is stored as a string without coercion
  (006 research line 81). `url_photo` and `scu` are likewise raw feed keys. Doing the extraction
  in SQL keeps the mapping in one place and keeps the wire DTO clean.
- **Type mapping on the wire**: `urlPhoto: string | null`, `crew: string | null` (kept as the
  stored string), `scu: number | null` (cast `(raw_data->>'scu')::numeric` with null-safe
  handling of missing/empty values). The frontend formats `scu`/`crew` as supporting metadata.
- **Alternatives considered**: Returning the entire `raw_data` blob to the client (as
  `GetShipById` does) and extracting in the browser — rejected for Hangar because cards need only
  three fields and the card/search payloads should stay small for infinite scroll; extraction
  belongs server-side. Promoting `url_photo`/`scu`/`crew` to real columns via a 006 migration —
  rejected as out of scope (006 deliberately keeps them in `raw_data`).

## R3. Reading jsonb fields with EF Core / Npgsql

- **Decision**: Implement Hangar read queries in `HangarRepository` (Infrastructure) using a
  projection that reaches into `raw_data` via Npgsql's JSON support. Project to flat read models
  (`ShipCard`, grouped org card, search row). Use `EF.Functions`/`->>'key'` translation or a
  `FromSql` projection where the translation is clearer; keep `Application` handlers persistence-
  agnostic behind `IHangarRepository`.
- **Rationale**: `Ship.RawData` is mapped as `JsonDocument`/`jsonb`. `JsonDocument` navigation is
  not LINQ-translatable, so the extraction must be expressed with provider JSON operators or raw
  SQL at the Infrastructure boundary. Returning ready-made read models keeps the Application layer
  free of EF concerns (Clean Architecture).
- **Alternatives considered**: Mapping `raw_data` to a strongly-typed POCO — rejected; 006 needs
  the verbatim document and changing the mapping risks that feature. Loading entities and
  extracting in C# memory — acceptable for small per-member hangars but wasteful for org-wide
  grouping; SQL-side extraction scales better.

## R4. Org Hangar as a derived aggregate

- **Decision**: Org Hangar is a **query**, not a stored table. Group `HangarEntry` rows by
  `ship_id`, join `sc.ships` for card fields, aggregate `COUNT(DISTINCT user_id)` for the owner
  count and an owner list (`user_id`, `DisplayName`). No "org ship" entity.
- **Rationale**: Spec is explicit (FR-005, clarification): every Org Hangar entry originates from
  a member's personal hangar; there is no independent org store. A derived query keeps add/remove
  immediately consistent (FR-020, FR-036, FR-037) with no synchronization code.
- **Alternatives considered**: Materialized aggregate table — rejected (YAGNI; introduces
  invalidation complexity for no current scale need).

## R5. Meaning of "organization" in v1

- **Decision**: "Organization members" = **all authenticated `ApplicationUser`s** in this single
  deployment. Org Hangar aggregates across all users; the member filter lists all users who own
  ≥1 ship.
- **Rationale**: The codebase has no Organization entity — only `ApplicationUser : IdentityUser`.
  The portal is the Naja Echo org's tooling (one deployment = one org). Spec assumptions confirm
  org membership comes from the existing identity system and Org Hangar is a derived view.
- **Alternatives considered**: Introducing an Organization/membership model now — rejected (YAGNI,
  no multi-org requirement). Recorded as an explicit assumption so a future multi-org change is a
  deliberate scope addition, not a silent gap.

## R6. Duplicate-ownership prevention

- **Decision**: Enforce at two layers: (1) a **unique constraint** on `(user_id, ship_id)` in the
  ownership table; (2) an Application-layer check in `AddShipToHangar` that returns a conflict
  before insert. The add endpoint returns **409 Conflict** when the ship is already owned; search
  results carry an `alreadyOwned` flag so the dialog disables them up front (FR-018/019).
- **Rationale**: The unique index is the durable guarantee even under concurrent requests; the
  pre-check gives a clean, fast user-facing error and avoids relying on DB exception parsing for
  the happy-path UX.
- **Alternatives considered**: App-check only (rejected: race conditions); DB-constraint only
  (rejected: surfaces as raw exception, harder to message cleanly).

## R7. Card background image and runtime load failure

- **Decision**: Card uses `urlPhoto` as a CSS background image when present and non-empty;
  otherwise a bundled **default card background**. On runtime image load failure, the card
  **silently falls back** to the default background (handled in the frontend via an `onError`/load
  probe), with no error indicator. Cards always render a readability scrim/overlay so the
  top-left name and any metadata stay legible over any image (FR-008/009/010, clarification).
- **Rationale**: Spec mandates silent fallback and readability regardless of image. A foreground
  gradient/scrim guarantees contrast without per-image tuning.
- **Alternatives considered**: `<img>` element with placeholder swap — viable, but a CSS
  background with an overlay gives simpler full-bleed card art and consistent text contrast.

## R8. Infinite scroll and list shaping

- **Decision**: Server endpoints are **page-based** (`page`, `pageSize`) like the existing
  `/api/admin/ships` list, carrying `search`/filter query params. The frontend uses TanStack
  Query `useInfiniteQuery`, advancing pages on an `IntersectionObserver` sentinel; changing
  search/filter resets to page 1 (FR-029/030, US4).
- **Rationale**: Reuses the established paging contract shape (`PagedShipsResponse`), minimizing
  new patterns. Page-based paging is adequate at this scale; cursor paging is unnecessary.
- **Alternatives considered**: Cursor/keyset paging — rejected (YAGNI at this scale; page-based
  matches existing code).

## R9. Member filter data source

- **Decision**: A dedicated read `GET /api/hangar/org/members` returns only members who own ≥1
  ship (`userId`, `displayName`). Selecting a member or toggling "My Ships" is applied via query
  params on the Org Hangar list endpoint; selecting a specific member clears "My Ships"
  (FR-024/025/026) — enforced in the frontend view state.
- **Rationale**: A small explicit endpoint keeps the filter list correct (only owning members,
  per clarification) without overloading the gallery payload.
- **Alternatives considered**: Deriving the member list client-side from loaded cards — rejected:
  infinite scroll means not all owners are loaded, so the filter would be incomplete.

## R10. Soft-deleted catalog ships

- **Decision**: Add Ship search returns only `Active` catalog ships. My/Org Hangar reads join
  `sc.ships` and surface the entry's stored card fields even if the catalog row later goes
  `SoftDeleted` (the member still owns it); these still render with whatever fields exist. Removal
  of a hangar entry is always permitted.
- **Rationale**: A member's ownership record should not vanish because the catalog import dropped
  a row; but you should not be able to *add* a ship that is no longer active. Keeps My Hangar
  stable while keeping Add Ship clean.
- **Alternatives considered**: Hiding owned-but-soft-deleted ships — rejected (loses member data
  silently, contradicts FR-004).
