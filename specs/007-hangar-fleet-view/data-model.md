# Phase 1 Data Model: Hangar

## Entities

### HangarEntry (NEW — the only data this feature persists)

The single source of ship ownership. A row means "this member owns this catalog ship."

| Property   | Type             | Column         | Notes                                                        |
|------------|------------------|----------------|--------------------------------------------------------------|
| `Id`       | `Guid`           | `id` (PK)      | Surrogate key.                                               |
| `UserId`   | `Guid`           | `user_id` (FK) | → `AspNetUsers.Id` (`ApplicationUser`). Owning member.       |
| `ShipId`   | `Guid`           | `ship_id` (FK) | → **`sc.ships.id`** (catalog PK — verified in research R1).  |
| `AddedAt`  | `DateTimeOffset` | `added_at`     | When the member added the ship (display order fallback).     |

**Constraints / indexes**
- **Unique** `(user_id, ship_id)` — enforces "a member owns a ship model at most once"
  (FR-018; research R6). Database name e.g. `ux_hangar_entries_user_ship`.
- Index on `ship_id` — supports Org Hangar grouping.
- Index on `user_id` — supports My Hangar lookup and "My Ships" filter.
- FK `ship_id` → `sc.ships(id)` (restrict/no cascade; catalog rows are soft-deleted, not hard-deleted).
- FK `user_id` → `AspNetUsers(id)` (cascade on user delete).
- **Schema**: table lives alongside the catalog (suggest `sc.hangar_entries`) — confirm during
  migration; the FK target `sc.ships(id)` is fixed regardless.

**EF mapping**: `HangarEntryConfiguration : IEntityTypeConfiguration<HangarEntry>`, registered in
`AppDbContext.OnModelCreating`; `DbSet<HangarEntry> HangarEntries`. Additive migration
`AddHangarEntries` (non-destructive — no approval gate needed).

### Ship (EXISTING — `sc.ships`, READ-ONLY here)

Not modified. Fields consumed by Hangar:

| Source                         | Used as                | Where it comes from                  |
|--------------------------------|------------------------|--------------------------------------|
| `name` (column)                | card display name + search | promoted column                  |
| `company_name` (column)        | manufacturer / metadata    | promoted column                  |
| `raw_data ->> 'url_photo'`     | card background image      | jsonb key (nullable)             |
| `raw_data ->> 'scu'`           | cargo capacity metadata    | jsonb key, cast numeric (nullable) |
| `raw_data ->> 'crew'`          | crew size metadata         | jsonb key, kept as string (nullable) |
| `id` (PK)                      | ownership FK / card key     | promoted column                  |
| `status`                       | filter Add Ship to `Active` | promoted column                 |

### Organization Member (EXISTING — `ApplicationUser`)

`Id` (`Guid`), `DisplayName`. Used for owner lists/counts and the member filter. "Organization" =
all authenticated members in this single-org deployment (research R5).

## Read models (Application layer — not persisted)

### ShipCard (My Hangar card row)
```
{ shipId: Guid, name: string, companyName: string?, urlPhoto: string?, scu: number?, crew: string? }
```

### OrgShipCard (Org Hangar grouped card)
```
{ shipId, name, companyName, urlPhoto, scu, crew,
  ownerCount: int,                       // COUNT(DISTINCT user_id)
  owners: [ { userId: Guid, displayName: string } ] }   // for hover list
```

### CatalogSearchRow (Add Ship result)
```
{ shipId, name, companyName, urlPhoto, scu, crew, alreadyOwned: bool }   // alreadyOwned for current user
```

### OwningMember (member filter option)
```
{ userId: Guid, displayName: string }   // only members owning >= 1 ship
```

## Derivations & rules

- **My Hangar** = `HangarEntry` where `user_id = current` ⨝ `sc.ships` → `ShipCard[]`, paged,
  name-filtered (case-insensitive, partial). No owner data (FR-011).
- **Org Hangar** = all `HangarEntry` ⨝ `sc.ships`, grouped by `ship_id` → `OrgShipCard[]`, paged,
  name-filtered; optional `mine`/`memberId` filter. `ownerCount` = distinct owners (FR-012,
  SC-004). Adding/removing entries changes the grouping immediately (no stored aggregate;
  research R4).
- **Add** = insert `HangarEntry(current, shipId)`; **409** if `(user_id, ship_id)` exists (R6).
- **Remove** = delete `HangarEntry(current, shipId)`; if it was the last owner the ship simply
  drops out of the Org Hangar query result (FR-036); otherwise the count decreases (FR-037).
- **Add Ship search** queries `sc.ships.name` over `Active` ships only (research R10), annotating
  `alreadyOwned` for the current member (FR-019).

## Transfer shapes (API DTOs)

Mirror `contracts/openapi.yaml`; frontend Zod view-model schemas live in
`features/hangar/schemas/`. See the contract for `HangarShipCard`, `OrgHangarShipCard`,
`HangarOwner`, `CatalogSearchItem`, `OwningMember`, `AddShipRequest`, and the paged envelopes.
