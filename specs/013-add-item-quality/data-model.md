# Phase 1 Data Model: Add item quality

## Entity update: WarehouseInventoryEntry (`public.warehouse_inventory`)

Existing entity gains one required field:

| Field | Type (C#) | Column | Rules |
|-------|-----------|--------|-------|
| Quality | `int` | `quality` | Required, integer, `1 <= quality <= 1000`, default `500` |

### Table constraints

- Add check constraint: `ck_warehouse_inventory_quality` with expression
  `quality >= 1 AND quality <= 1000`.
- Column is `NOT NULL` with default value `500`.

### Migration behavior

1. Add `quality` column with server default `500`.
2. Backfill existing rows (if needed) to `500`.
3. Add quality range check constraint.

## DTO and contract model updates

### Warehouse item inventory row (API/application DTO)

Add `quality` to list/read row shapes:

- `InventoryRowDto` (application)
- `InventoryRowResponse` (API contract record)
- frontend inventory row schema and generated types

### Ship components row (API/application DTO)

Add `quality` to ship-component row shapes:

- `ShipComponentRowDto` (application)
- `ShipComponentRowResponse` (API contract record)
- frontend ship-component row schema and generated types

### Add request models

Add optional request field:

| Request | Field | Type | Defaulting rule |
|---------|-------|------|-----------------|
| `AddInventoryItemRequest` | `quality` | integer (optional) | Missing/`null` => `500` |
| `AddShipComponentRequest` | `quality` | integer (optional) | Missing/`null` => `500` |

Validation for both add paths:
- must be integer
- must be in `1..1000`

## Relationships

No new entities or relationships are introduced. `quality` is scalar state on the existing
`warehouse_inventory` row and travels through existing read/write workflows.

## State/transition notes

- **Create row**: quality set from provided value or default `500`.
- **Increment existing row on add**: quantity increments and quality is set to the submitted value
  (`quality` in request, defaulting to `500` when omitted) so quartermasters can correct/refresh
  condition as they add stock.
- **Edit quantity**: quality unaffected.
- **Delete row**: quality removed with row.
