# Phase 1 Data Model: Item Data Import

Two new tables in the `sc` schema, delivered by one EF Core migration
(`<timestamp>_AddItemCategoriesAndItems`). Both follow the `sc.ships` hybrid pattern: promoted
typed columns for identity/filtering/display + a `jsonb raw_data` column for source fidelity.
Naming uses the repo's snake_case convention (`UseSnakeCaseNamingConvention`), so most column names
are derived automatically; the configuration classes set table/schema, keys, indexes, conversions,
and the `jsonb` type explicitly (mirroring `ShipConfiguration`).

## Entity: ItemCategory → `sc.item_categories`

Local copy of a UEX category. Refreshed by `RefreshCategories`. Drives the category selector and
item-import eligibility.

| Domain property | Column | Type | Notes |
|-----------------|--------|------|-------|
| `Id` | `id` | `uuid` (PK) | App-generated stable PK. |
| `UexId` | `uex_id` | `int` | UEX `id` (source route id). **Unique index** — refresh upsert key. |
| `Type` | `type` | `text` | UEX `type`. Eligibility = `type == "item"`. Indexed. |
| `Section` | `section` | `text?` | UEX `section`. Selector filter. Indexed. |
| `Name` | `name` | `text` | UEX `name`. Selector search. |
| `IsGameRelated` | `is_game_related` | `bool` | UEX `is_game_related`. Selector filter. |
| `IsMining` | `is_mining` | `bool` | UEX `is_mining`. Selector filter. |
| `SourceDateAdded` | `source_date_added` | `timestamptz?` | From UEX `date_added`. |
| `SourceDateModified` | `source_date_modified` | `timestamptz?` | From UEX `date_modified`. Selector column. |
| `RawData` | `raw_data` | `jsonb` | Full source category record. |
| `ImportedAt` | `imported_at` | `timestamptz` | First refresh time. |
| `UpdatedAt` | `updated_at` | `timestamptz` | Last refresh that changed/touched the row. Page "last refreshed" = `MAX(updated_at)`. |

**Indexes**: unique `ix_item_categories_uex_id`; `ix_item_categories_type`; `ix_item_categories_section`.

**Notes**: Categories are never soft-deleted in v1 (the spec only soft-deletes *items*). Refresh
upserts by `uex_id`. UEX boolean-ish fields may arrive as `0/1`/strings — normalize at map time.

## Entity: Item → `sc.items`

Local copy of a UEX item, scoped to a category. Imported by `ImportItems`. Identity is `uuid`.

| Domain property | Column | Type | Notes |
|-----------------|--------|------|-------|
| `Id` | `id` | `uuid` (PK) | App-generated stable PK. |
| `Uuid` | `uuid` | `text` | **Stable identity. Unique index.** Upsert key. (Null-uuid records are skipped before insert, so this column is never null.) |
| `UexId` | `uex_id` | `int` | UEX `id` (source route id). Indexed, **not** unique. |
| `IdParent` | `id_parent` | `int?` | UEX `id_parent`. |
| `IdCategory` | `id_category` | `int` | UEX `id_category`. **Soft-delete scope key.** Indexed. |
| `IdCompany` | `id_company` | `int?` | UEX `id_company`. |
| `IdVehicle` | `id_vehicle` | `int?` | UEX `id_vehicle`. |
| `Name` | `name` | `text` | UEX `name`. |
| `Section` | `section` | `text?` | UEX `section`. |
| `Category` | `category` | `text?` | UEX `category` (denormalized name). |
| `CompanyName` | `company_name` | `text?` | UEX `company_name`. |
| `VehicleName` | `vehicle_name` | `text?` | UEX `vehicle_name`. |
| `Slug` | `slug` | `text?` | UEX `slug`. |
| `Size` | `size` | `text?` | UEX `size` (UEX may send number or string — store as text for fidelity). |
| `Color` | `color` | `text?` | UEX `color`. |
| `Color2` | `color2` | `text?` | UEX `color2`. |
| `UrlStore` | `url_store` | `text?` | UEX `url_store`. |
| `Wiki` | `wiki` | `text?` | UEX `wiki`. |
| `Quality` | `quality` | `text?` | UEX `quality`. |
| `IsExclusivePledge` | `is_exclusive_pledge` | `bool` | UEX `is_exclusive_pledge`. |
| `IsExclusiveSubscriber` | `is_exclusive_subscriber` | `bool` | UEX `is_exclusive_subscriber`. |
| `IsExclusiveConcierge` | `is_exclusive_concierge` | `bool` | UEX `is_exclusive_concierge`. |
| `IsCommodity` | `is_commodity` | `bool` | UEX `is_commodity`. |
| `IsHarvestable` | `is_harvestable` | `bool` | UEX `is_harvestable`. |
| `Notification` | `notification` | `text?` | UEX `notification`. |
| `GameVersion` | `game_version` | `text?` | UEX `game_version`. |
| `SourceDateAdded` | `source_date_added` | `timestamptz?` | From UEX `date_added`. |
| `SourceDateModified` | `source_date_modified` | `timestamptz?` | From UEX `date_modified`. |
| `Status` | `status` | `text` | `ItemStatus` enum stored as string (`Active` / `SoftDeleted`), via `HasConversion<string>()`. |
| `RawData` | `raw_data` | `jsonb` | Full source record **with `attributes` and `screenshot` removed** (FR-021, FR-022). |
| `ImportedAt` | `imported_at` | `timestamptz` | First import time. |
| `UpdatedAt` | `updated_at` | `timestamptz` | Last import that touched the row. |
| `SoftDeletedAt` | `soft_deleted_at` | `timestamptz?` | Set when soft-deleted, cleared on restore. |

**Indexes**: unique `ix_items_uuid`; `ix_items_uex_id`; `ix_items_id_category`;
`ix_items_status`. (`(id_category, status)` composite is acceptable since the soft-delete sweep
filters on both — optional optimization.)

**Excluded by design**: `attributes` (deprecated, FR-021) and `screenshot` (v1 out-of-scope,
FR-022) are stripped from `raw_data` and never promoted to columns.

## Enum: ItemStatus

`Active` | `SoftDeleted`. Mirrors `Domain/Ships/ShipStatus`. Stored as string.

- **Active**: visible to normal application use.
- **SoftDeleted**: hidden from normal use, preserved for references/troubleshooting (FR-016).
- Transitions: insert → Active; absent from a category import → SoftDeleted; reappears → Active
  (restore, FR-015).

## State transitions (Item, per category import)

```
incoming record, uuid == null         → skipped (counted ItemsSkippedNoUuid), no row
incoming uuid not in table            → INSERT Active                     (inserted++)
incoming uuid present, row Active      → UPDATE                           (updated++ or unchanged++)
incoming uuid present, row SoftDeleted → UPDATE + Active, clear SoftDeletedAt (restored++ → reported under updated/restored)
stored Active row in this category,
  uuid absent from incoming           → SoftDeleted, set SoftDeletedAt    (softDeleted++)
stored row in OTHER category          → untouched (scope guarantee, FR-014)
```

> "Unchanged" vs "updated": the spec accepts counting unchanged rows either separately or within
> updated (spec Assumptions). v1 may classify a matched row whose promoted columns + raw_data are
> byte-identical as `unchanged`; otherwise `updated`. Restored rows count toward the soft-delete
> reversal and are reported so the admin sees they came back.

## Result DTOs (transient — not persisted)

### RefreshCategoriesResult
`CategoriesFetched`, `CategoriesInserted`, `CategoriesUpdated`, `CategoriesUnchanged`,
`CategoriesFailed`, `StartedAt`, `CompletedAt`, `Duration`.

### ImportItemsResult
`CategoriesProcessed`, `CategoriesSucceeded`, `CategoriesFailed`, `ItemsFetched`, `ItemsInserted`,
`ItemsUpdated`, `ItemsUnchanged`, `ItemsSkippedNoUuid`, `ItemsSoftDeleted`, `ItemsFailed`,
`StartedAt`, `CompletedAt`, `Duration`, `Errors` (list of `CategoryImportError { CategoryUexId,
CategoryName, Message }`), and a derived `Status` (`success` | `completedWithErrors` | `failed`).

### CategoryListItem (selector context — GetCategories)
`UexId`, `Name`, `Section`, `Type`, `IsGameRelated`, `IsMining`, `SourceDateModified`, and local
import state (`LocalItemCount`, `LastImportedAt` nullable). Plus the page-level
`CategoriesLastRefreshedAt` (nullable aggregate) returned alongside the list.
