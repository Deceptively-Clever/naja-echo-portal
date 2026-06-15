# Phase 0 Research: Add item quality

All open technical decisions for this feature are resolved below.

## R1 — Where quality is stored

- **Decision**: Store quality on `public.warehouse_inventory` as a required integer column (`quality`).
- **Rationale**: Quality is an attribute of an inventory row (item + owner + location) rather than
  catalog metadata. This aligns with current write/read APIs and avoids adding new entities.
- **Alternatives considered**:
  - Store on `sc.items` (rejected: would incorrectly make quality global per catalog item).
  - Separate quality table (rejected: unnecessary complexity for one bounded integer field).

## R2 — Default and backfill strategy

- **Decision**: Add `quality` as `NOT NULL DEFAULT 500` and backfill existing rows to `500` in the
  migration.
- **Rationale**: Guarantees backward-compatible reads and avoids nullable checks across handlers,
  repositories, and UI schemas.
- **Alternatives considered**:
  - Nullable quality with runtime fallback (rejected: leaks migration concern into all read paths).
  - Backfill without DB default (rejected: weaker data integrity for future direct inserts).

## R3 — Validation boundaries and ownership

- **Decision**: Enforce quality bounds (`1..1000`, integer) at API/application validation boundaries for
  all add flows. Omitted quality is normalized to `500` before persistence.
- **Rationale**: Centralized validation keeps behavior consistent regardless of calling client and
  protects data integrity.
- **Alternatives considered**:
  - Frontend-only validation (rejected: unsafe; API can still receive invalid payloads).
  - DB check only (rejected: less user-friendly error handling and harder testability at API layer).

## R4 — Endpoint and contract scope

- **Decision**: Update both warehouse add surfaces and list/read payloads:
  - `POST /api/warehouse/items` request accepts optional `quality`
  - `POST /api/warehouse/ship-components` request accepts optional `quality`
  - `GET /api/warehouse/items` rows include `quality`
  - `GET /api/warehouse/ship-components` rows include `quality`
- **Rationale**: Ship components are stored in the same inventory table; keeping payload parity avoids
  hidden behavior differences.
- **Alternatives considered**:
  - Update items endpoints only (rejected: inconsistent behavior for warehouse item creation paths).

## R5 — UI treatment

- **Decision**: Add a quality input to warehouse add dialogs, defaulted to `500`, constrained to
  integers within `1..1000`; show quality in inventory/ship-component tables and schemas where rows are
  rendered.
- **Rationale**: User story requires quartermasters to specify quality at add time and preserve it in
  read surfaces.
- **Alternatives considered**:
  - Persist quality without UI display (rejected: weak transparency and discoverability for users).

## R6 — Constitution/API-contract compliance

- **Decision**: This is **not** a UI-only feature; `contracts/openapi.yaml` is updated in this feature
  directory with modified schemas for requests and responses.
- **Rationale**: Constitution Principle I requires contract-first for backend behavior changes.
- **Alternatives considered**:
  - No contract update (rejected: constitution violation).
