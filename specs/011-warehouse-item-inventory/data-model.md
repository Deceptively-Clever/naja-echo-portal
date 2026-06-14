# Phase 1 Data Model: Warehouse Item Inventory

## New entity

### WarehouseInventoryEntry  (`public.warehouse_inventory`)

One row = one quantity of one catalog item, owned by one portal user, at one location.

| Field         | Type              | Constraints / Notes                                                        |
|---------------|-------------------|---------------------------------------------------------------------------|
| `Id`          | `Guid`            | PK (surrogate). Used as the `{id}` route param for edit/remove.            |
| `ItemId`      | `Guid`            | FK → `sc.items.Id`. Required. `ON DELETE RESTRICT` (catalog is read-only). |
| `OwnerUserId` | `Guid`            | FK → Identity user (`AspNetUsers`). Required. Registered portal user.      |
| `Location`    | `string`          | Required. Trimmed before persist. Non-empty after trim. Free text.         |
| `Quantity`    | `int`             | Required. ≥ 1 (CHECK + app validation).                                    |
| `CreatedAt`   | `DateTimeOffset`  | Set on insert.                                                             |
| `UpdatedAt`   | `DateTimeOffset`  | Set on insert and on every quantity change.                               |

**Indexes / constraints**

- **Unique index** on (`ItemId`, `OwnerUserId`, `Location`) — enforces FR-019 (separate rows per
  owner/location) and is the race arbiter for FR-017 add-or-increment.
- Index on `ItemId` (FK lookups / catalog joins) and on `OwnerUserId` (Owner filter).
- CHECK `Quantity >= 1`.
- FK `ItemId` → `sc.items` with **restrict** delete; FK `OwnerUserId` → users.

**Configuration**: `WarehouseInventoryEntryConfiguration` maps `ToTable("warehouse_inventory")`
(public schema), the composite unique index, the supporting indexes, the check constraint, and
`Location` max length (e.g. 200). Registered in `AppDbContext.OnModelCreating`; `DbSet` added to
`AppDbContext`. Delivered by one migration `<timestamp>_AddWarehouseInventory`.

## Referenced existing entities (read-only from this feature)

### Item  (`sc.items`) — catalog source

Provides display + filter fields. Only `Status == Active` items are searchable in the add flow and
contribute Type/Subtype values.

| Field      | Used as            |
|------------|--------------------|
| `Id`       | join target (`ItemId`) |
| `Name`     | inventory **Name** column; catalog search; default sort key |
| `Section`  | inventory **Type** column + Type filter (exact) |
| `Category` | inventory **Subtype** column + Subtype filter (exact) |
| `Status`   | only `Active` items are addable / searchable |

### ItemCategory  (`sc.item_categories`) — filter option source (FR-007)

| Field     | Used as              |
|-----------|----------------------|
| `Section` | distinct **Type** filter options |
| `Name`    | distinct **Subtype** filter options |

### ApplicationUser (Identity) — owner source

| Field         | Used as                                  |
|---------------|------------------------------------------|
| `Id` (`Guid`) | `OwnerUserId`; Owner filter; add-form owner picker |
| display name  | **Owner** column label; owner picker label |

## Derived / projected DTOs (not persisted)

- **InventoryRow** (list item): `Id`, `ItemId`, `Name`, `Type` (Section), `Subtype` (Category),
  `Quantity`, `OwnerUserId`, `OwnerDisplayName`, `Location`.
- **InventoryFilters**: `Types: string[]`, `Subtypes: string[]`, `Owners: { userId, displayName }[]`.
- **CatalogItemResult** (add-flow search): `ItemId`, `Name`, `Type`, `Subtype`.

## Validation rules (Application layer — FluentValidation)

- **Add** (`AddInventoryItemCommand`): `ItemId` references an Active catalog Item (else 404);
  `OwnerUserId` references a registered user; `Location` non-empty after trim; `Quantity` ≥ 1.
- **Change quantity** (`ChangeInventoryQuantityCommand`): target row exists (else 404); `Quantity` ≥ 1.
- **Remove** (`RemoveInventoryItemCommand`): target row exists (idempotent-friendly: missing → 404).

## State & behaviour notes

- **Add-or-increment**: resolve by (ItemId, OwnerUserId, trimmed Location). Exists → `Quantity +=
  submitted`, bump `UpdatedAt`. Absent → insert. Unique-constraint violation on concurrent insert →
  caught, retried as increment (see research §3).
- **Change quantity**: replaces `Quantity` (not additive); bump `UpdatedAt`.
- **Remove**: deletes the `WarehouseInventoryEntry` row only; the `sc.items` row is never touched
  (FK restrict + no cascade), satisfying FR-025.
