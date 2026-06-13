# Phase 0 Research: Hangar JSON Import

All Technical Context items were resolved from direct codebase inspection (feature 007 is already
implemented on this branch's parent). No external/unknown technology choices remained.

## R1 — How the file reaches the backend

- **Decision**: The browser reads the selected file as text, parses it as JSON, validates the
  shape with Zod, then `POST`s the parsed records as an `application/json` body to
  `POST /api/hangar/mine/import` with shape `{ "items": [ … ] }`. The backend match result is
  authoritative.
- **Rationale**: The existing `apiFetch` helper always sends `Content-Type: application/json` and
  `JSON.stringify`s the body (see `hangarApi.addShip`). Records are small (tens to hundreds), so
  there is no benefit to streaming a multipart upload. Client-side parse gives instant feedback
  for invalid files (FR-011) without a server round-trip, while the server re-validates and
  performs authoritative name matching (never trust the client).
- **Alternatives considered**: `multipart/form-data` file upload parsed server-side — rejected:
  `apiFetch` would need a bypass for the content type, adds server file-handling code, and yields
  no benefit at this scale. Top-level JSON array as the request body — rejected in favour of the
  `{ items: [...] }` envelope so the contract can evolve (add fields/metadata) and because some
  OpenAPI tooling handles object roots more cleanly than array roots.

## R2 — Name matching against the catalog

- **Decision**: For each record, the **effective name** is `ship_name` when present and non-blank,
  otherwise `name`. Match the effective name against `sc.ships.name` **case-insensitively**
  (`ILIKE`/`LOWER(...)` equality, not wildcard) where `status = 'Active'`. Records carrying an
  `unidentified` field are **skipped before matching** (they are, by definition, ships the export
  tool could not identify). Records whose effective name matches no Active catalog ship are
  skipped as **unmatched**.
- **Rationale**: Matches FR-006/FR-007 and the Assumptions (case-insensitive matching; catalog
  pre-populated with compatible names). `status = Active` mirrors every other hangar query in
  `HangarRepository`, so soft-deleted catalog ships are never imported.
- **Alternatives considered**: Fuzzy/`ILIKE '%name%'` matching — rejected: would mis-match
  variants (e.g. "F8C Lightning" vs "F8C Lightning Executive Edition") and silently import the
  wrong ship. Matching on `ship_code`/`manufacturer_code` — rejected: the catalog's matchable key
  is `name`; the spec mandates name matching and `ship_code` is not stored as a promoted column.

## R3 — Atomic replace-all

- **Decision**: In a single transaction (`db.Database.BeginTransactionAsync`), delete **all**
  `sc.hangar_entries` for the user, insert the de-duplicated matched set, `SaveChangesAsync`, then
  `CommitAsync`. On any failure the transaction rolls back and the hangar is unchanged.
- **Rationale**: Satisfies FR-008 (remove existing before insert) and FR-012 (atomic). Mirrors the
  established transactional bulk pattern in `ShipRepository.BulkUpsertAsync`. An empty `items`
  array (or all-unmatched) results in an empty hangar — the delete still runs (edge case in spec).
- **Alternatives considered**: Delete + insert without an explicit transaction — rejected: a
  failure mid-operation could leave the hangar empty or partial, violating FR-012. Merge/append
  instead of replace — rejected: out of scope per spec Assumptions.

## R4 — Duplicate handling vs. the existing unique constraint (resolves spec FR-009 conflict)

- **Decision**: **De-duplicate** matched catalog ships. Multiple import records that resolve to
  the same catalog ship produce exactly **one** hangar entry. The summary counts distinct imported
  ships.
- **Rationale**: The existing `ux_hangar_entries_user_ship` unique index on `(user_id, ship_id)`
  (feature 007) makes it physically impossible for a member to own the same catalog ship twice.
  Because matching is by name → a single `ship_id`, duplicate-named records map to the same ship.
  Inserting them unguarded would violate the constraint and abort the transaction. De-duplication
  is the only behaviour consistent with the current schema.
- **Spec impact**: This **deviates from FR-009** ("duplicate ship names produce multiple hangar
  entries"). FR-009 should be revised to "duplicate matched ships collapse into a single hangar
  entry." Flagged in `plan.md`; recommend updating the spec before `/speckit-tasks` or recording
  the deviation as accepted.
- **Alternatives considered**: Drop the unique constraint and allow quantity-per-ship — rejected:
  large schema/behaviour change to feature 007, out of scope, and the rest of the hangar UI assumes
  one card per owned ship. A `quantity` column — rejected for the same YAGNI reason.

## R5 — Import summary semantics (FR-010)

- **Decision**: Return `{ totalRecords, importedShips, unmatchedRecords, unmatchedShipNames }`:
  - `totalRecords` — count of records received.
  - `importedShips` — distinct Active catalog ships matched and now in the hangar (final size).
  - `unmatchedRecords` — records skipped because they were `unidentified` or matched no Active
    catalog ship.
  - `unmatchedShipNames` — distinct effective names that did not match (for user display), capped
    to a reasonable length for the response.
- **Rationale**: Directly satisfies FR-010 and SC-003 (clear imported/skipped summary every time).
  Duplicates collapse into `importedShips` silently (R4); they are neither "imported again" nor
  "unmatched", so they are intentionally not surfaced as skips to avoid confusing the user.
- **Alternatives considered**: Reporting a separate `duplicateCount` — deferred (YAGNI); can be
  added later without a breaking contract change.

## R6 — Validation, limits, and error handling

- **Decision**: Client-side Zod validation rejects non-JSON and non-conforming files before any
  API call (FR-011, SC-004), surfacing an inline error. The server independently validates the
  request body shape and returns `400 ProblemDetails` on malformed payloads. Enforce a **5 MB**
  file-size cap client-side (assumption) with a clear message. Unknown/extra fields in records are
  ignored (lenient parse) so future HangarXPLOR fields don't break import.
- **Rationale**: Defence in depth; the hangar is never mutated by an invalid file. Lenient
  field handling matches the real export which carries many fields we don't use.
- **Alternatives considered**: Strict rejection of unknown fields — rejected: real exports include
  optional/extra keys (`lti`, `pledge_*`, `unidentified`, etc.); strictness would reject valid
  files.

## R7 — Cache invalidation after import

- **Decision**: On success, invalidate the TanStack Query keys for **My Hangar**, **Org Hangar**,
  **owning members**, and **catalog search** (the `alreadyOwned` flag changes), using the existing
  `hangarQueryKeys` factory.
- **Rationale**: Replace-all changes all of these derived views; invalidation triggers refetch so
  the UI reflects the new hangar immediately (acceptance scenario: hangar shows imported ships).
- **Alternatives considered**: Optimistic cache rewrite — rejected: the authoritative matched set
  is only known after the server responds; a refetch is simpler and correct.
