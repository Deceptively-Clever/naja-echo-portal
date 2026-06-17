# Feature Specification: Warehouse Materials Subpage

**Feature Branch**: `014-warehouse-materials`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "Create a feature specification for Warehouse - Materials. This feature adds a Materials page under the existing Warehouse area. The page tracks available crafting material inventory sourced from the existing sc.commodities table, following the same behavior and UX patterns as the existing Warehouse inventory and ship component flows."

## Clarifications

### Session 2026-06-15

- Q: Is "adjust quantity" an absolute set-to-value operation or a relative increment/decrement (+/-) operation? → A: Absolute set — the Quartermaster enters the new total quantity directly.
- Q: Do the Owner and Location filters support selecting multiple values at once (OR within the field), or only a single value at a time? → A: Single-select — only one Owner and one Location value may be chosen at a time per filter.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — View Materials Inventory (Priority: P1)

An authenticated member of the org wants to see all crafting material inventory on a dedicated Materials page so they can quickly find what materials are stocked, how much exists, who owns it, where it is stored, and at what quality.

**Why this priority**: Viewing is the foundational capability every authenticated user needs; all other stories build on it. Delivering this alone gives org members immediate value.

**Independent Test**: Can be fully tested by navigating to Warehouse → Materials and confirming the material inventory list renders with the expected columns and default sort order.

**Acceptance Scenarios**:

1. **Given** an authenticated user is on any page in the app, **When** they navigate to Warehouse → Materials, **Then** they land on the Materials page and see a list of material inventory rows.
2. **Given** the Materials page is loaded, **When** the list renders, **Then** each row displays Material, Owner, Location, Quantity, and Quality.
3. **Given** material inventory exists, **When** the list renders, **Then** Quantity is displayed as a decimal value with exactly 2 decimal places.
4. **Given** material inventory exists, **When** the list renders, **Then** rows are sorted by Material name ascending, then Quality descending, then Owner name ascending, then Location ascending.
5. **Given** an unauthenticated visitor attempts to access the Materials page, **When** they navigate to the route, **Then** they are denied access using the same unauthorized behavior as the existing Inventory page.
6. **Given** no material inventory exists, **When** the page renders, **Then** an appropriate "no material inventory" empty state is displayed.

---

### User Story 2 — Add Material (Priority: P2)

A Quartermaster (or Admin) needs to add crafting material to inventory by searching and selecting a commodity, assigning an owner, location, quantity, and quality, using the same interaction pattern as adding items or ship components.

**Why this priority**: Without the ability to add material, the inventory cannot grow; this is the primary management capability and the core of the feature's value. It depends on the view (P1) existing first.

**Independent Test**: Can be fully tested by opening the add material dialog, selecting a commodity, completing the fields, saving, and confirming a row appears (or an existing matching row's quantity increases).

**Acceptance Scenarios**:

1. **Given** a Quartermaster is on the Materials page, **When** they open the add material dialog, **Then** they can search and select a material from `sc.commodities` and cannot create a custom material.
2. **Given** the add material dialog opens, **When** it renders, **Then** Owner defaults to the currently authenticated user.
3. **Given** the add material dialog opens, **When** it renders, **Then** Quality defaults to `500`.
4. **Given** a Quartermaster is adding material, **When** they enter the Location, **Then** the UI suggests existing Warehouse locations already used elsewhere in inventory.
5. **Given** a Quartermaster adds material with the same Material, Owner, Location, and Quality as an existing row, **When** they save, **Then** the existing row's quantity is increased by the entered amount and no duplicate row is created.
6. **Given** a Quartermaster adds material that differs from every existing row in Material, Owner, Location, or Quality, **When** they save, **Then** a new inventory row is created.
7. **Given** a Quartermaster enters a Quantity of `0.00` or less (including negative), **When** they attempt to save, **Then** the system prevents the material from being added and surfaces a validation message.
8. **Given** a Quartermaster enters a Quality below `1` or above `1000`, **When** they attempt to save, **Then** the system prevents the material from being added and surfaces a validation message.
9. **Given** a Quartermaster is adding material, **When** they select an Owner, **Then** they may choose any registered user, defaulting to themselves.
10. **Given** a non-Quartermaster authenticated user is on the Materials page, **When** the page renders, **Then** no add control is visible or accessible.

---

### User Story 3 — Adjust Material Quantity (Priority: P3)

A Quartermaster (or Admin) needs to adjust the quantity of an existing material row to keep inventory accurate, without being able to drive the quantity to zero or below.

**Why this priority**: Quantity adjustment keeps inventory accurate over time, but the page still delivers value with view and add before this is implemented.

**Independent Test**: Can be fully tested by adjusting an existing row's quantity to a valid positive value (success) and to `0.00` or less (blocked), confirming the row reflects only valid changes.

**Acceptance Scenarios**:

1. **Given** a Quartermaster is on the Materials page, **When** they adjust a material row's quantity to a value greater than `0.00`, **Then** the row's quantity is updated.
2. **Given** a Quartermaster adjusts a material row's quantity to `0.00` or below (including negative), **When** they attempt to save, **Then** the system prevents the update and the row retains its prior quantity.
3. **Given** a material row exists, **When** a Quartermaster adjusts quantity, **Then** Quality cannot be changed as part of the adjustment.
4. **Given** a non-Quartermaster authenticated user views a material row, **When** the page renders, **Then** no quantity-adjustment control is visible or accessible.

---

### User Story 4 — Remove Material (Priority: P3)

A Quartermaster (or Admin) needs to delete a material row to remove it from active inventory, since quantity cannot be reduced to zero.

**Why this priority**: Removal is the only supported way to take material out of inventory, complementing quantity adjustment, but read and add capabilities deliver value first.

**Independent Test**: Can be fully tested by deleting a material row and confirming it no longer appears in the list.

**Acceptance Scenarios**:

1. **Given** a Quartermaster is on the Materials page, **When** they delete a material inventory row, **Then** the row is removed from active inventory and no longer appears in the list.
2. **Given** a non-Quartermaster authenticated user views material inventory, **When** the page renders, **Then** no remove/delete control is visible or accessible.

---

### User Story 5 — Filter and View Quality (Priority: P3)

An authenticated user wants to narrow the Materials list by material search, owner, location, and quality range so they can quickly find the right material in a large inventory, and confirm a row's quality is shown but not editable after creation.

**Why this priority**: Filtering is the primary discovery tool for large inventories, but the base list (P1) still delivers value without it.

**Independent Test**: Can be fully tested by applying each filter independently and in combination and verifying the visible rows match all active criteria, and by confirming Quality is read-only after a row is created.

**Acceptance Scenarios**:

1. **Given** the Materials page is loaded, **When** the user enters text in the Material search filter, **Then** only rows whose material name or commodity code matches the text are displayed.
2. **Given** the Materials page is loaded, **When** the user searches for and selects a single Owner, **Then** only rows for that owner are shown; selecting a different owner replaces the prior selection.
3. **Given** the Materials page is loaded, **When** the user searches for and selects a single Location, **Then** only rows at that location are shown; selecting a different location replaces the prior selection.
4. **Given** the Materials page is loaded, **When** the user sets a Quality minimum and maximum, **Then** only rows with Quality inside that inclusive range are shown.
5. **Given** the Quality range filter, **When** the page renders, **Then** the range defaults to `1–1000` and is adjustable via a dual-ended min/max slider, optionally accompanied by numeric min/max inputs.
6. **Given** multiple filters are applied, **When** the list updates, **Then** only rows matching all selected filters are shown (filters combine with AND logic).
7. **Given** all filters are empty, **When** the list renders, **Then** all material inventory is shown.
8. **Given** a material row already exists, **When** a user views or edits the row, **Then** Quality is visible but cannot be changed.

---

### Edge Cases

- What happens when material inventory is entirely empty (no rows exist)? → A "no material inventory" empty state is displayed.
- What happens when active filters match no rows? → A distinct "no results match the current filters" empty state is displayed.
- What happens when a Quartermaster adds material matching an existing row, pushing the combined quantity higher? → The existing row's quantity increases; no duplicate row is created.
- What happens when a Quartermaster tries to "zero out" a row via quantity adjustment to remove it? → The update is blocked; the Quartermaster must delete the row instead. Quantity is never used as a soft-delete mechanism.
- What happens when quality was entered incorrectly? → Quality cannot be edited after creation; the user must delete the row and re-add the material with the correct quality.
- What happens when an Admin navigates to the Materials page? → The Admin sees the same Quartermaster controls (add, adjust quantity, delete) without a separate role assignment.
- What happens when the same commodity exists at multiple owners, locations, or qualities? → Each distinct combination of Material, Owner, Location, and Quality appears as its own row.
- What happens when a quantity is entered with more than 2 decimal places? → The input is rounded to 2 decimal places (half-up) before validation and storage; a rounded value that becomes `0.00` is rejected by the > 0.00 rule.

## Requirements *(mandatory)*

### Functional Requirements

**Navigation**

- **FR-001**: The Warehouse navigation area MUST include sub-navigation items for Inventory, Ship Components, and Materials.
- **FR-002**: Users MUST be able to navigate to the Materials page via the Warehouse → Materials navigation path.

**Access Control**

- **FR-003**: The Materials page MUST be accessible only to authenticated users; unauthenticated visitors MUST be denied access using the same unauthorized behavior as the existing Inventory page.
- **FR-004**: Authenticated users without the Quartermaster role MUST be able to view material inventory but MUST NOT see add, quantity-adjust, or delete controls.
- **FR-005**: Users with the Quartermaster role MUST be able to add materials, adjust material quantities, and delete material rows from this page.
- **FR-006**: Admins MUST automatically have Quartermaster-level access to the Materials page without requiring a separate role assignment.

**Data Source & Material Field**

- **FR-007**: Materials MUST be sourced exclusively from the existing `sc.commodities` table; users MUST NOT be able to create custom materials from this page.
- **FR-008**: The Material field MUST be required and selected from `sc.commodities`.

**Displayed Fields**

- **FR-009**: Each material inventory row MUST display Material, Owner, Location, Quantity, and Quality.
- **FR-010**: Quantity MUST be displayed as a decimal value with exactly 2 decimal places.
- **FR-011**: Quality MUST be displayed as a read-only value for rows that already exist.

**Field Rules — Owner**

- **FR-012**: Owner MUST be required and MUST be a registered user.
- **FR-013**: When adding material, Owner MUST default to the currently authenticated user.
- **FR-014**: A Quartermaster MUST be able to select another registered user as the Owner when
  adding material, chosen from users who already own at least one Materials inventory row
  (consistent with the existing Items/Ship Components Owner-selection pattern). Assigning
  ownership to a brand-new user who owns no rows is out of scope for v1.

**Field Rules — Location**

- **FR-015**: Location MUST be required and entered as free text.
- **FR-016**: The Location input MUST suggest existing locations already used elsewhere in Materials inventory.

**Field Rules — Quantity**

- **FR-017**: Quantity MUST be required and MUST be greater than `0.00`.
- **FR-018**: The system MUST reject any quantity equal to `0.00`, less than `0.00`, or negative on both add and adjust.
- **FR-019**: Quantity MUST NOT be used as a soft-delete mechanism; removal MUST be performed via row deletion only.

**Field Rules — Quality**

- **FR-020**: Quality MUST be required and MUST be an integer from `1` to `1000` inclusive.
- **FR-021**: Quality MUST default to `500` when adding material.
- **FR-022**: Quality MUST be set only when adding material and MUST NOT be editable after the row is created.
- **FR-023**: To correct an incorrect Quality, the user MUST delete the row and add the material again; the system MUST NOT provide an in-place quality edit.

**Row Uniqueness**

- **FR-024**: A material inventory row MUST be unique by the combination of Material, Owner, Location, and Quality.
- **FR-025**: When a Quartermaster adds material matching an existing row's Material, Owner, Location, and Quality, the system MUST increase that row's quantity instead of creating a duplicate row.
- **FR-026**: When any of Material, Owner, Location, or Quality differs from every existing row, the system MUST create a separate row.

**Add Material Flow**

- **FR-027**: The add material flow MUST follow the same interaction pattern as adding items or ship components: open a dialog, search `sc.commodities`, select a material, select or accept the default Owner, enter or select a Location, enter Quantity, enter or accept the default Quality, and save.
- **FR-028**: On save, the system MUST either create a new row or increment an existing matching row according to the row uniqueness rule.
- **FR-029**: The system MUST block saving when Quantity or Quality fail their validation rules and MUST surface a validation message.

**Adjust Quantity**

- **FR-030**: A Quartermaster MUST be able to adjust the quantity of an existing material row by entering the new total quantity directly (absolute set, not a relative increment/decrement).
- **FR-031**: The system MUST prevent any quantity adjustment that would set the row quantity to `0.00` or less; the row MUST retain its prior quantity when an invalid adjustment is rejected.
- **FR-032**: A quantity adjustment MUST NOT change the row's Quality, Material, Owner, or Location.

**Remove Material**

- **FR-033**: A Quartermaster MUST be able to delete a material inventory row, which removes it from active inventory.
- **FR-034**: No audit history is required for material additions, adjustments, or deletions in v1.

**Filters**

- **FR-035**: The page MUST provide a Material search filter that matches material name and commodity code.
- **FR-036**: The page MUST provide an Owner filter that allows searching and selecting a single registered user at a time.
- **FR-037**: The page MUST provide a Location filter that allows searching and selecting a single existing known Warehouse location at a time.
- **FR-038**: The page MUST provide a Quality range filter implemented as a dual-ended min/max range slider, optionally accompanied by numeric min/max inputs, defaulting to the range `1–1000`.
- **FR-039**: The Quality range filter MUST include rows whose Quality falls within the selected inclusive `[min, max]` range.
- **FR-040**: Filters across different fields MUST combine using AND logic.
- **FR-041**: Empty filters MUST be ignored and MUST show all material inventory.

**Sorting**

- **FR-042**: Material rows MUST be sorted by Material name ascending, then Quality descending, then Owner name ascending, then Location ascending as the default order.

**Empty States**

- **FR-043**: When no material inventory rows exist, the page MUST display an appropriate empty state message.
- **FR-044**: When rows exist but no results match active filters, the page MUST display a distinct "no results" empty state message.

### Key Entities

- **Material Inventory Row**: An inventory entry for a crafting material. Carries Material (commodity reference), Owner (registered user), Location (free text), Quantity (decimal > 0.00, 2 places), and Quality (integer 1–1000, set at creation, immutable thereafter). Unique by the combination of Material + Owner + Location + Quality.
- **Commodity (`sc.commodities`)**: The existing source of selectable materials. Provides material name and commodity code used for selection and search. Read-only with respect to this feature; commodities are neither created nor imported here.
- **Owner (Registered User)**: A registered application user who owns a material inventory row. Defaults to the authenticated user on add; a Quartermaster may assign another registered user.
- **Warehouse Location**: A free-text storage location. Existing locations used elsewhere in inventory are surfaced as suggestions and as filter options.
- **Quartermaster**: A role granting add, quantity-adjust, and delete access to material inventory. Admins inherit this access automatically.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Authenticated users can navigate to the Materials page from any page in the application in two clicks or fewer.
- **SC-002**: Every material row displays all five fields — Material, Owner, Location, Quantity (2 decimal places), and Quality — on every page load.
- **SC-003**: Material rows appear in the five-key default sort order (Material name asc, Quality desc, Owner name asc, Location asc) on every page load without user action.
- **SC-004**: 100% of add-material flows restrict selection to commodities from `sc.commodities`; no custom or non-commodity material can be saved.
- **SC-005**: 100% of attempts to save a quantity of `0.00` or less (on add or adjust) are blocked, and no row is ever persisted with a non-positive quantity.
- **SC-006**: 100% of attempts to save a Quality outside `1–1000` are blocked.
- **SC-007**: Adding material matching an existing row's Material, Owner, Location, and Quality increases that row's quantity and creates zero duplicate rows.
- **SC-008**: After creation, Quality cannot be changed through any control on the page; the only path to a different quality is delete-and-re-add.
- **SC-009**: Any single filter or combination of filters returns correct results; no rows outside the active filter criteria appear, and an empty filter set shows all rows.
- **SC-010**: Unauthenticated users cannot reach the Materials page; 100% of such attempts result in the same denial/redirect behavior as the existing Inventory page.
- **SC-011**: Non-Quartermaster authenticated users see no add, quantity-adjust, or delete controls on the Materials page.
- **SC-012**: A Quartermaster can complete an add, quantity-adjust, or delete operation without leaving the Materials page.

## Assumptions

- The existing Warehouse Inventory and Ship Components add/manage flows (dialog, commodity/item search, owner selection, location suggestion) can be reused or adapted for Materials; no new modal or form pattern is required.
- "Admin automatically has Quartermaster-level access" is enforced at the same layer and by the same mechanism as on the existing Inventory and Ship Components pages; no new role infrastructure is needed.
- "Same unauthorized behavior as the existing Inventory page" means the same denial/redirect-to-login flow with the current URL preserved as a return destination.
- Warehouse location suggestions and the Location filter draw from locations currently used
  within Materials inventory at runtime (not from Items or Ship Components inventory, and not
  from a static reference list) — consistent with how Ship Components (012) scopes its own
  Location filter to its own inventory rather than cross-referencing Items.
- `sc.commodities` already exists, is populated, and exposes a material name and a commodity code suitable for search and display; this feature reads it but neither imports nor modifies it.
- "Registered user" means any authenticated application user account; selecting an owner does not change that user's roles or permissions. Per FR-014, the Owner picker on add is populated from users who already own at least one Materials inventory row, not a search across all registered accounts.
- Quality is stored as an integer and Quantity as a decimal; display formatting (2 decimal places for Quantity) does not change stored precision.
- Excess Quantity precision is rounded to 2 decimal places before validation and storage.
- Material reservation, allocation, crafting consumption, transaction history, commodity import, custom material creation, Quartermaster role assignment, and zero-quantity rows are explicitly out of scope for v1.
