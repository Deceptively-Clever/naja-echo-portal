# Phase 0 Research: Warehouse Ship Components

All open questions from the Technical Context are resolved below. No `NEEDS CLARIFICATION` remain.

## R1 — UEX item id lookup key for `items_attributes`

- **Decision**: Use `Item.UexId` (`sc.items.uex_id`, `int`) as the `id_item` query parameter for
  `https://api.uexcorp.uk/2.0/items_attributes?id_item={uexItemId}`. If a Systems item has a missing
  or non-positive `UexId`, skip the fetch and leave component attributes Unknown.
- **Rationale**: `UexId` is the UEX-native item identifier already imported and indexed
  (`ix_items_uex_id`). The existing `UexItemClient.FetchItemsByCategoryAsync` uses the same UEX id
  space (`id_category`). No new identifier mapping is needed.
- **Alternatives considered**: `Item.Uuid` (UEX uses numeric `id_item`, not the uuid, for this
  endpoint — rejected); a separate "source id" column (none exists; `UexId` already serves this).

## R2 — Two-table design (raw cache + typed projection)

- **Decision**: Persist `sc.item_attributes` (one row per UEX attribute per item, all attributes
  including undisplayed ones such as Volume/Mass) and `sc.ship_component_attributes` (one row per
  item: `class` text, `size` int, `grade` text, `attributes_fetched_at`). The read query joins
  inventory → item → `ship_component_attributes`.
- **Rationale**: Matches the approved design. The raw cache preserves the full UEX payload for future
  attributes without re-fetching; the typed projection gives an indexed, queryable, sort/filterable
  shape for the page. Separating them keeps the read query simple (single typed join) while retaining
  source fidelity (Constitution IV: each table earns its place with genuinely distinct data).
- **Alternatives considered**: Storing Class/Size/Grade columns on `warehouse_inventory` (rejected —
  spec forbids it; attributes are item-level, not inventory-row-level, and would duplicate across
  rows for the same item). A single denormalized attribute table only (rejected — loses typed
  int Size and forces string parsing on every read/sort).

## R3 — Raw-attribute uniqueness key

- **Decision**: Unique index on (`item_id`, `uex_category_attribute_id`). Also store
  `uex_attribute_id` (the per-row UEX attribute id) as a nullable column.
- **Rationale**: The design prefers a stable UEX attribute id "if appropriate; otherwise item +
  category attribute id." `id_category_attribute` is the stable definition of *which attribute*
  (e.g. the "Class" attribute of a category), so item + category-attribute id is the natural
  "one value per attribute per item" key and is robust to UEX reusing/rotating per-row
  `id_attribute` values. The per-row `uex_attribute_id` is retained for traceability.
- **Alternatives considered**: Unique on `uex_attribute_id` alone (rejected — risk if UEX row ids are
  not globally stable across re-fetch; also nullable in some payloads). Unique on
  (`item_id`, `attribute_name`) (rejected — name is display text, less stable than the numeric
  category-attribute id).

## R4 — Projection mapping (Class / Size / Grade)

- **Decision**: Build `sc.ship_component_attributes` from the raw rows by matching
  `attribute_name == "Class"` → `class` (text), `"Size"` → `size` (parse text→int;
  `int.TryParse` failure → null), `"Grade"` → `grade` (text). Name match is case-insensitive,
  trimmed. Upsert (insert or update) keyed by `item_id`; set `attributes_fetched_at` to the fetch
  time. All raw attributes are stored regardless of whether they map to a displayed field.
- **Rationale**: Directly implements the spec's logical mapping and the "store raw Size as text,
  parse to int for projection, null on failure" rule. Case-insensitive trimmed matching guards
  against UEX whitespace/casing drift.
- **Alternatives considered**: Matching by `uex_category_attribute_id` constants (rejected — those
  ids are not documented/stable across categories; the spec specifies name-based mapping).

## R5 — Lazy fetch + cache trigger and failure handling

- **Decision**: On `POST /api/warehouse/ship-components` (add) for a Systems item, the
  `AddShipComponentHandler`: (1) validates the item exists and is Systems; (2) checks whether raw
  attributes already exist for the item; (3) if absent and `UexId` is usable, calls
  `IUexItemAttributeClient.FetchItemAttributesAsync`, stores all raw rows, and upserts the typed
  projection; (4) if present, skips the UEX call entirely; (5) proceeds to add/increment the
  inventory row using the **existing** `AddOrIncrementAsync`. Any UEX/fetch/parse failure is caught,
  logged via Serilog, and does **not** abort inventory creation — Class/Size/Grade remain null.
- **Rationale**: Implements the spec's exact 8-step ordering and the non-blocking requirement.
  Lazy fetch keeps UEX off the read path and avoids redundant calls (Constitution IV/V).
- **Alternatives considered**: Fetch on read (rejected — couples list rendering to an external API,
  violates SC-006 responsiveness). Background/scheduled refresh (rejected — out of scope; YAGNI).
  Fetch on every add (rejected — spec says do not re-query when cached).

## R6 — Systems scoping, sort, and Unknown-last ordering

- **Decision**: The list query filters `sc.items.section = 'Systems'` (case-insensitive compare to be
  safe) and joins `ship_component_attributes`. Default ORDER BY: `i.name`, then `i.category` (Type),
  then `size NULLS LAST`, then `class NULLS LAST`, then `grade NULLS LAST` — all ascending. The DTO
  returns nullable Class/Size/Grade; the API serializes null and the **frontend** renders "Unknown".
- **Rationale**: PostgreSQL `NULLS LAST` cleanly satisfies "sort Unknown last where practical"
  without a sentinel string. Keeping "Unknown" purely presentational avoids polluting stored/filter
  values (FR-021).
- **Alternatives considered**: Storing the literal "Unknown" (rejected — spec forbids treating
  Unknown as a stored value). Client-side sort (rejected — server is authoritative for ordering).

## R7 — Filter options + explicit Unknown filtering

- **Decision**: `GET /ship-components/filters` returns distinct Type/Class/Size/Grade/Owner/Location
  values drawn **only** from current Ship Component (Systems) inventory, plus boolean flags
  `unknownClass`/`unknownSize`/`unknownGrade` (true when at least one Systems inventory row has a
  null value for that attribute). The list endpoint accepts repeated query params per field for OR
  semantics, AND across fields; a sentinel query flag (e.g. `class=__unknown__` or
  `unknownClass=true`) lets the client request rows whose attribute is null without "Unknown" being a
  stored value. Empty filters are ignored.
- **Rationale**: Implements FR-016..FR-022 precisely. Deriving options from current inventory (not a
  static list) matches the spec assumption. The Unknown flag drives the dropdown option; the sentinel
  drives the query.
- **Alternatives considered**: Reusing 011's `GET /items/filters` (rejected — that draws Type/Subtype
  from all `item_categories`, not Systems-only inventory, and has no Class/Size/Grade/Location/Unknown
  concept).

## R8 — Reuse of write endpoints and frontend dialogs

- **Decision**: Reuse `PUT /api/warehouse/items/{id}/quantity` and `DELETE /api/warehouse/items/{id}`
  (011) for edit/delete unchanged — a Ship Component row *is* a `warehouse_inventory` row. Add gets a
  new endpoint (`POST /ship-components`) because of the Systems guard + lazy fetch. The frontend
  reuses `AddInventoryDialog` (with a Systems-scoped catalog search and derived, non-editable
  Name/Type/Class/Size/Grade preview), `EditQuantityControl`, and `RemoveInventoryButton`.
- **Rationale**: Maximizes reuse (Constitution IV); edit/delete semantics are identical, so new
  endpoints would be duplication. Only add differs behaviourally.
- **Alternatives considered**: Brand-new quantity/remove endpoints under `/ship-components` (rejected
  — pure duplication).

## R9 — Frontend feature placement and nav

- **Decision**: Ship Components UI lives in the existing `features/warehouse/` folder (same feature
  area as Items), sharing `useIsQuartermaster` and the add/edit/remove components. Nav adds
  **Ship Components** and a placeholder **Materials** entry to the existing **Warehouse** group in
  `navItems.ts`; `AppRouter` adds `/warehouse/ship-components` and a `/warehouse/materials`
  placeholder route. Generated types come from a new `gen:api:ship-components` script.
- **Rationale**: They are one feature area; co-location reduces duplication and matches Constitution
  VI (feature-owned logic, data-driven nav, thin routes). Materials is nav-only per scope.
- **Alternatives considered**: A separate `features/ship-components/` folder (rejected — would force
  promoting shared warehouse components prematurely; same domain).
