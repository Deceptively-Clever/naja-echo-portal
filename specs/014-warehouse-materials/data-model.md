# Phase 1 Data Model: Warehouse Materials Subpage

## New entity — `WarehouseMaterialEntry`

Domain entity in `NajaEcho.Domain/Warehouse/WarehouseMaterialEntry.cs`, persisted to
`public.warehouse_material_inventory`. Mirrors `WarehouseInventoryEntry` but is commodity-sourced,
uses a decimal quantity, and includes Quality in its uniqueness key.

| Field | .NET type | Column | Rules |
|-------|-----------|--------|-------|
| `Id` | `Guid` | `id` (PK) | Server-generated. |
| `CommodityId` | `Guid` | `commodity_id` | Required. FK → `sc.commodities(id)`, `ON DELETE RESTRICT`. The selected material (FR-007, FR-008). |
| `OwnerUserId` | `Guid` | `owner_user_id` | Required. FK → `AspNetUsers(id)`. Defaults to caller on add; a Quartermaster may pick another user (FR-012..FR-014). |
| `Location` | `string` | `location` | Required, free text, `HasMaxLength(200)`, trimmed (FR-015, FR-016). |
| `Quantity` | `decimal` | `quantity` `decimal(18,2)` | Required, `> 0.00`. Rounded half-up to 2 places before validation/storage (FR-010, FR-017, FR-018). |
| `Quality` | `int` | `quality` | Required, `1..1000`, default `500`. Set only at creation; immutable thereafter (FR-020..FR-023). Part of the unique key. |
| `CreatedAt` | `DateTimeOffset` | `created_at` | Set on insert. |
| `UpdatedAt` | `DateTimeOffset` | `updated_at` | Set on insert and on every increment/adjust. |

### Constraints & indexes (EF `WarehouseMaterialEntryConfiguration`)

- **Primary key**: `id`.
- **Unique index** `ux_warehouse_material_inventory_commodity_owner_location_quality` on
  `(commodity_id, owner_user_id, location, quality)` — enforces FR-024 and is the upsert conflict
  target (FR-025, FR-026).
- **Check** `ck_warehouse_material_inventory_quantity`: `quantity > 0` (FR-017, FR-018, SC-005).
- **Check** `ck_warehouse_material_inventory_quality`: `quality >= 1 AND quality <= 1000`
  (FR-020, SC-006).
- **Index** `ix_warehouse_material_inventory_commodity_id` on `commodity_id`.
- **Index** `ix_warehouse_material_inventory_owner_user_id` on `owner_user_id`.
- **FK** `fk_warehouse_material_inventory_commodity_id` → `sc.commodities(id)`, `Restrict`.
- (Owner FK follows the existing project convention for `AspNetUsers` references.)

### State transitions

```
(none) --add (new key)----------------> Row{ quantity = q (>0), quality = Q }
Row    --add (matching key)-----------> Row{ quantity += q }            # quality unchanged
Row    --adjust (PUT, new total t>0)--> Row{ quantity = t }            # quality/material/owner/location unchanged
Row    --delete (DELETE)--------------> (removed)
```

There is no zero/negative quantity state — it is rejected on both add and adjust; the only exit is
delete (FR-019, FR-031, FR-033).

## Referenced existing entities (read-only here)

### `Commodity` (`sc.commodities`)

Source of selectable materials (FR-007). Relevant fields: `Id` (FK target), `Name` and `Code`
(searched and displayed — FR-009, FR-035), plus `Status` / `SoftDeletedAt` (catalog search excludes
soft-deleted commodities). Never created or modified by this feature.

### Owner — `AspNetUsers`

A registered user who owns a row. `Id` is the FK; `DisplayName` is projected into list/filter
responses (FR-009, FR-012). Selecting an owner does not change that user's roles (Assumptions).

### Warehouse Location

Not a table — a free-text value on each row. The distinct set of `location` values currently present
in material inventory is surfaced as add-form suggestions and as the Location filter's options
(FR-016, FR-037, Assumptions).

## DTOs (Application layer)

- **`MaterialRowDto`** (`GetMaterials`): `Id`, `CommodityId`, `MaterialName`, `MaterialCode`,
  `Quantity` (decimal), `Quality`, `OwnerUserId`, `OwnerDisplayName`, `Location`.
- **`MaterialFiltersDto`** (`GetMaterialFilters`): `Owners` (list of `OwnerOption{ UserId,
  DisplayName }`), `Locations` (list of string).
- **`CommodityResultDto`** (`SearchCommodities`): `CommodityId`, `Name`, `Code`.

API request/response records (`MaterialDtos.cs`) map 1:1 to the OpenAPI schemas in
`contracts/openapi.yaml`.
