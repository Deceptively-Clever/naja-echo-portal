# Phase 1 Data Model: Warehouse Ship Components

Two new `sc`-schema tables are introduced via **one** EF Core migration
(`AddShipComponentAttributes`). No changes to `public.warehouse_inventory` or `sc.items`. Reads join
existing tables to the new projection.

## Entity: ItemAttribute (`sc.item_attributes`)

Raw cache of every attribute UEX returns from `items_attributes?id_item={uexItemId}`, one row per
attribute per item. All attributes are stored, including those not displayed (e.g. Volume, Mass).

| Field                      | Type (C#)          | Column (snake_case)          | Notes |
|----------------------------|--------------------|------------------------------|-------|
| Id                         | `Guid`             | `id` (PK)                    | App-generated |
| ItemId                     | `Guid`             | `item_id`                    | FK → `sc.items.id`, required, `OnDelete(Cascade)` |
| UexAttributeId             | `int?`             | `uex_attribute_id`           | UEX per-row `id` (traceability; nullable) |
| UexItemId                  | `int`              | `uex_item_id`                | UEX `id_item` used for the fetch |
| UexCategoryId              | `int?`             | `uex_category_id`            | UEX `id_category` |
| UexCategoryAttributeId     | `int`              | `uex_category_attribute_id`  | UEX `id_category_attribute` (uniqueness key) |
| AttributeName              | `string`           | `attribute_name`             | required, `HasMaxLength(256)` |
| Value                      | `string?`          | `value`                      | raw value as text (Size stored as text here), `HasMaxLength(1024)` |
| Unit                       | `string?`          | `unit`                       | `HasMaxLength(64)` |
| SourceDateAdded            | `DateTimeOffset?`  | `source_date_added`          | from UEX `date_added` |
| SourceDateModified         | `DateTimeOffset?`  | `source_date_modified`       | from UEX `date_modified` |
| FetchedAt                  | `DateTimeOffset`   | `fetched_at`                 | required; when this cache row was written |

**Indexes / constraints**

- Unique: (`item_id`, `uex_category_attribute_id`) → `ux_item_attributes_item_category_attr`
  (prevents duplicating the same attribute for an item; see research R3).
- Index: `item_id` → `ix_item_attributes_item_id`.
- FK `item_id` → `sc.items.id`, `OnDelete(Cascade)` (cache is meaningless without its item).

## Read model: ShipComponentAttributes (`sc.ship_component_attributes` — **view**)

A **read-only SQL view** (not a table), one row per item that has any cached attribute. It derives
its columns live from `sc.item_attributes` by pivoting on `attribute_name`, so it can never drift
from the raw cache and needs no upsert/refresh step. Mapped via `ToView(...)`.

| Field                | Type (C#)          | Column (snake_case)     | Derivation |
|----------------------|--------------------|-------------------------|------------|
| ItemId               | `Guid`             | `item_id` (key)         | `item_attributes.item_id` (GROUP BY) |
| Class                | `string?`          | `class`                 | `max(value) FILTER (attribute_name = 'Class')`; null = Unknown |
| Size                 | `int?`             | `size`                  | "Size" value cast to int when it matches `^[+-]?\d+$` (≤ 9 digits); else null (mirrors `int.TryParse`) |
| Grade                | `string?`          | `grade`                 | `max(value) FILTER (attribute_name = 'Grade')`; null = Unknown |
| AttributesFetchedAt  | `DateTimeOffset`   | `attributes_fetched_at` | `max(item_attributes.fetched_at)` |

**Notes**

- No PK/FK/indexes (plain view). `attribute_name` matching is case-insensitive and trimmed.
- Reads (`GET /api/warehouse/ship-components` + filters) LEFT JOIN this view unchanged; items with no
  cached attributes simply yield null Class/Size/Grade ("Unknown").
- DDL lives in migration `AddShipComponentAttributes` (`CREATE VIEW` / `DROP VIEW`).

## Read model: ShipComponentRow (DTO, not a table)

Returned by `GET /api/warehouse/ship-components`. Produced by a SQL projection joining
`warehouse_inventory` → `sc.items` (Systems only) → `sc.ship_component_attributes` (left join) →
`AspNetUsers`.

| Field            | Source |
|------------------|--------|
| Id               | `warehouse_inventory.id` (inventory row id; used for edit/delete) |
| ItemId           | `warehouse_inventory.item_id` |
| Name             | `sc.items.name` |
| Type             | `sc.items.category` (item category name) |
| Class            | `ship_component_attributes.class` (nullable → "Unknown" in UI) |
| Size             | `ship_component_attributes.size` (nullable → "Unknown" in UI) |
| Grade            | `ship_component_attributes.grade` (nullable → "Unknown" in UI) |
| Quantity         | `warehouse_inventory.quantity` |
| OwnerUserId      | `warehouse_inventory.owner_user_id` |
| OwnerDisplayName | `AspNetUsers.display_name` |
| Location         | `warehouse_inventory.location` |

**Scope filter**: `sc.items.section = 'Systems'` (case-insensitive).

**Default sort**: `name ASC`, `category (Type) ASC`, `size ASC NULLS LAST`, `class ASC NULLS LAST`,
`grade ASC NULLS LAST`.

## Filter option model: ShipComponentFilters (DTO)

Returned by `GET /api/warehouse/ship-components/filters`, derived from current Systems inventory only.

| Field         | Source |
|---------------|--------|
| Types         | distinct `sc.items.category` over Systems inventory |
| Classes       | distinct non-null `ship_component_attributes.class` over Systems inventory |
| Sizes         | distinct non-null `ship_component_attributes.size` over Systems inventory |
| Grades        | distinct non-null `ship_component_attributes.grade` over Systems inventory |
| Owners        | distinct (`owner_user_id`, `display_name`) over Systems inventory |
| Locations     | distinct `location` over Systems inventory |
| UnknownClass  | bool — true if any Systems inventory row has null class |
| UnknownSize   | bool — true if any Systems inventory row has null size |
| UnknownGrade  | bool — true if any Systems inventory row has null grade |

## Relationships

```text
sc.items (1) ──< sc.item_attributes              (cascade delete)
sc.item_attributes ──> sc.ship_component_attributes  (VIEW; derived projection, 1 row per item with attributes)
sc.items (1) ──< public.warehouse_inventory      (restrict delete; from feature 011, unchanged)
AspNetUsers (1) ──< public.warehouse_inventory   (owner)
```

## Validation & derivation rules

- **Add scope (FR-023)**: an item is acceptable for Ship Component add only if `Item.Status = Active`
  **and** `Item.Section = 'Systems'`; otherwise reject (400/404 as appropriate).
- **Lazy fetch (research R5)**: fetch attributes from UEX only when no `sc.item_attributes` rows
  exist for the item and `Item.UexId > 0`. Never re-fetch when present.
- **Size parse**: the view casts the "Size" value to int only when it matches `^[+-]?\d+$` (≤ 9
  digits); failure or absence → `null` (mirrors `int.TryParse`).
- **Projection**: derived live by the `sc.ship_component_attributes` view from the latest raw rows;
  no upsert/refresh step — caching raw `sc.item_attributes` is sufficient.
- **Non-blocking failure**: any UEX/fetch/parse error is logged and swallowed; the inventory row is
  still created with attributes left Unknown.
- **Inventory identity / quantity / location**: unchanged from 011 (`item_id` + `owner_user_id` +
  trimmed `location` unique; quantity ≥ 1; add increments, edit replaces).
