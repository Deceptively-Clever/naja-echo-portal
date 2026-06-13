# Phase 1 Data Model: Hangar JSON Import

This feature introduces **no new persistent entities and no schema change**. It reuses feature
007's `sc.hangar_entries` and the read-only `sc.ships` catalog. The model below documents the
transient request/response shapes and the existing storage they operate on.

## Persistent storage (existing — unchanged)

### `sc.hangar_entries` (reused, written via replace-all)

| Column     | Type              | Notes                                              |
|------------|-------------------|----------------------------------------------------|
| `id`       | `uuid` (PK)       | `Guid.NewGuid()` per inserted entry                |
| `user_id`  | `uuid`            | Owning member (`ApplicationUser.Id`)               |
| `ship_id`  | `uuid` (FK→`sc.ships.id`, Restrict) | Matched catalog ship          |
| `added_at` | `timestamptz`     | Set to import time for every inserted entry        |

- Unique index `ux_hangar_entries_user_ship` on `(user_id, ship_id)` — **one ship per member**.
  Drives de-duplication (research R4). **No migration required.**

### `sc.ships` (read-only here)

- Matched by `name` (case-insensitive) where `status = Active`. Catalog is never modified by import.

## Transient types

### `ImportShipRecord` (Application) / `ImportShipRecordDto` (API request element)

One element of the uploaded array. **Lenient**: unknown fields are ignored.

| Field          | Type      | Required | Notes                                                        |
|----------------|-----------|----------|--------------------------------------------------------------|
| `name`         | string    | yes      | Display/fallback name. Present on every record in the export.|
| `shipName`     | string?   | no       | `ship_name` in the file. Preferred match key when non-blank. |
| `unidentified` | string?   | no       | When present, the record is skipped before matching (R2).    |

- **Effective name** (matching key) = `shipName` if non-blank, else `name`.
- All other export keys (`ship_code`, `manufacturer_*`, `lti`, `warbond`, `entity_type`,
  `pledge_*`, `lookup`) are accepted and ignored.

### `ImportHangarRequest` (API request body)

| Field   | Type                    | Required | Notes                                  |
|---------|-------------------------|----------|----------------------------------------|
| `items` | `ImportShipRecordDto[]` | yes      | The parsed export array. May be empty. |

- Empty `items` → hangar is cleared (replace-all with empty set; edge case in spec).
- Server rejects a missing/non-array `items` with `400 ProblemDetails` (FR-011).

### `ImportHangarResult` (Application) / `ImportHangarResultDto` (API response, 200)

| Field               | Type       | Notes                                                          |
|---------------------|------------|----------------------------------------------------------------|
| `totalRecords`      | int        | Number of records received in `items`.                         |
| `importedShips`     | int        | Distinct Active catalog ships matched & now in the hangar.     |
| `unmatchedRecords`  | int        | Records skipped (`unidentified` or no Active catalog match).   |
| `unmatchedShipNames`| string[]   | Distinct effective names that did not match (for display).     |

- Invariants: `importedShips ≥ 0`; `unmatchedRecords ≥ 0`; `unmatchedRecords` counts records
  (not distinct names); duplicates that collapse into an imported ship are reflected only in
  `importedShips` being ≤ count of matched records (research R4/R5).

## Processing rules (handler + repository)

1. **Filter** out records with a non-null `unidentified` → counted toward `unmatchedRecords`.
2. **Resolve effective name** for each remaining record; blank effective name → unmatched.
3. **Match** effective names to `sc.ships` (case-insensitive, `status = Active`) → ship IDs.
4. **De-duplicate** matched ship IDs (R4).
5. **Replace atomically** (R3): delete all `sc.hangar_entries` for the user, insert one entry per
   distinct matched ship (`added_at` = now), commit.
6. **Summarize** (R5): compute counts and the distinct unmatched effective names.

## State transition

```
Member hangar = { existing entries }
        │  import committed
        ▼
Member hangar = { one entry per distinct Active catalog ship whose name matched a non-unidentified record }
```

All-or-nothing: a failure at any step leaves the pre-import hangar intact (FR-012).
