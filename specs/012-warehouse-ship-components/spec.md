# Feature Specification: Warehouse Ship Components Subpage

**Feature Branch**: `012-warehouse-ship-components`

**Created**: 2026-06-14

**Status**: Draft

## User Scenarios & Testing *(mandatory)*

### User Story 1 — View Ship Components Inventory (Priority: P1)

An authenticated member of the org wants to see all ship component inventory separate from general warehouse inventory, so they can quickly find components relevant to ship outfitting without scrolling through unrelated items.

**Why this priority**: Viewing is the foundational capability all users need; every other story builds on it. Delivering this alone gives org members immediate value.

**Independent Test**: Can be fully tested by navigating to the Ship Components page and confirming that only inventory records for items in the Systems section are displayed.

**Acceptance Scenarios**:

1. **Given** an authenticated user is on any page in the app, **When** they navigate to Warehouse → Ship Components, **Then** they land on the Ship Components page and see a table of inventory records scoped exclusively to Systems-section items.
2. **Given** the Ship Components page is loaded, **When** the table renders, **Then** the columns Name, Type, Class, Size, Grade, Quantity, Owner, and Location are displayed in that order, with no Section column.
3. **Given** the Ship Components page is loaded, **When** records are displayed, **Then** they are sorted by Name ascending, then Type ascending, then Size ascending, then Class ascending, then Grade ascending by default.
4. **Given** a ship component record has no Class, Size, or Grade value, **When** the row is displayed, **Then** the missing field shows "Unknown".
5. **Given** an unauthenticated visitor attempts to access the Ship Components page, **When** they navigate to the route, **Then** they are redirected to the login page using the same unauthorized behavior as the existing Inventory page.

---

### User Story 2 — Filter Ship Components (Priority: P2)

An authenticated user wants to narrow the Ship Components list by Name, Type, Class, Size, Grade, Owner, or Location so they can quickly find specific components in a large inventory.

**Why this priority**: Filtering is the primary discovery tool when inventory is large; without it the page is much less useful but the base view still delivers value.

**Independent Test**: Can be fully tested by applying each filter independently and in combination and verifying the visible rows match all active criteria.

**Acceptance Scenarios**:

1. **Given** the Ship Components page is loaded, **When** the user types text into the Name filter, **Then** only rows whose Name contains that text (case-insensitive, partial match) are displayed.
2. **Given** the Ship Components page is loaded, **When** the user selects one or more values from the Type filter, **Then** only rows whose Type matches any of the selected values are displayed.
3. **Given** the Ship Components page is loaded, **When** the user selects one or more values from the Class, Size, Grade, Owner, or Location filters, **Then** only rows matching any selected value within each filter are shown, and filters across different fields combine with AND logic.
4. **Given** a filter has no value selected, **When** the page renders or filters are applied, **Then** that filter is ignored and does not restrict results.
5. **Given** ship component records exist with no Class, Size, or Grade, **When** the user opens the corresponding filter dropdown, **Then** "Unknown" appears as a selectable option in that filter.
6. **Given** the user has one or more active filters, **When** they click the clear/reset filters action, **Then** all filters are reset to empty and the full unfiltered list is displayed.
7. **Given** active filters return no matching records, **When** the filtered result is empty, **Then** a "no results match the current filters" empty state is displayed.

---

### User Story 3 — Quartermaster Manages Ship Component Inventory (Priority: P3)

A Quartermaster (or Admin) needs to add new ship component inventory records, edit existing ones, and delete records from the Ship Components page, using the same workflow as the existing Inventory page.

**Why this priority**: Management actions are essential for Quartermasters but the page still delivers significant read-only value before this story is implemented.

**Independent Test**: Can be fully tested by creating, editing, and deleting a ship component inventory record from the Ship Components page and confirming the table reflects each change.

**Acceptance Scenarios**:

1. **Given** a Quartermaster is on the Ship Components page, **When** they initiate adding a new inventory record, **Then** the item search/selection flow shows only items from the Systems section.
2. **Given** a Quartermaster is adding a new inventory record, **When** they select an item, **Then** Name, Type, Class, Size, and Grade are populated automatically from the item data and are not editable; only Quantity, Owner, and Location are editable.
3. **Given** a Quartermaster submits a new inventory record, **When** the record is saved, **Then** it appears in the Ship Components table following the default sort order.
4. **Given** a Quartermaster edits an existing inventory record, **When** they save changes, **Then** only Quantity, Owner, and Location can be modified; derived fields remain unchanged.
5. **Given** a Quartermaster deletes an inventory record, **When** the deletion is confirmed, **Then** the record is removed from the table.
6. **Given** a non-Quartermaster authenticated user is on the Ship Components page, **When** the page renders, **Then** no add, edit, or delete controls are visible or accessible.
7. **Given** an Admin is on the Ship Components page, **When** the page renders, **Then** Quartermaster-level add, edit, and delete controls are available, identical to a Quartermaster's experience.

---

### Edge Cases

- What happens when Ship Component inventory is empty (no records exist at all)? → A "no ship component inventory" empty state is displayed.
- What happens when filters are applied and no Systems-section inventory records match? → The "no results match the current filters" empty state is displayed.
- What happens when Class, Size, or Grade is missing and the user filters by "Unknown"? → Only records with the corresponding missing value are shown.
- What happens when multiple owners or locations exist for the same item? → Each unique owner/location combination appears as a separate row, matching existing Inventory page uniqueness behavior.
- What happens when an Admin navigates to the Ship Components page? → The Admin sees the same Quartermaster controls without needing a separate role assignment.

## Requirements *(mandatory)*

### Functional Requirements

**Navigation**

- **FR-001**: The Warehouse navigation area MUST be updated to include sub-navigation items for Inventory, Ship Components, and Materials (Materials links to a placeholder or future page; its content is out of scope).
- **FR-002**: Users MUST be able to navigate to the Ship Components page via the Warehouse → Ship Components navigation path.

**Access Control**

- **FR-003**: The Ship Components page MUST be accessible only to authenticated users; unauthenticated visitors MUST be redirected using the same unauthorized behavior as the existing Inventory page.
- **FR-004**: Authenticated users without the Quartermaster role MUST be able to view Ship Component inventory but MUST NOT see add, edit, or delete controls.
- **FR-005**: Users with the Quartermaster role MUST be able to create, update, and delete Ship Component inventory records from this page.
- **FR-006**: Admins MUST automatically have Quartermaster-level access to the Ship Components page without requiring a separate role assignment.

**Data Scope**

- **FR-007**: The Ship Components page MUST display only inventory records for items whose category section is Systems.
- **FR-008**: The Section column MUST NOT be displayed on the Ship Components page.

**Displayed Columns**

- **FR-009**: The Ship Components table MUST display the following columns in order: Name, Type, Class, Size, Grade, Quantity, Owner, Location.
- **FR-010**: Name MUST display the item/component name.
- **FR-011**: Type MUST display the item category name.
- **FR-012**: Class, Size, and Grade MUST be derived from component attributes and displayed as read-only values.
- **FR-013**: Quantity, Owner, and Location MUST follow the same display behavior as the existing Inventory page.
- **FR-014**: If Class, Size, or Grade is absent for a record, the cell MUST display "Unknown".

**Default Sorting**

- **FR-015**: Ship Component records MUST be sorted by: Name ascending → Type ascending → Size ascending → Class ascending → Grade ascending as the default order.

**Filters**

- **FR-016**: The page MUST provide a Name filter that supports case-insensitive partial text matching.
- **FR-017**: The page MUST provide Type, Class, Size, Grade, Owner, and Location filters that present selectable values drawn from existing Ship Component inventory.
- **FR-018**: Filters across different fields MUST combine using AND logic.
- **FR-019**: Multiple selected values within a single filter MUST combine using OR logic.
- **FR-020**: Empty (unselected) filters MUST be ignored and MUST NOT restrict results.
- **FR-021**: "Unknown" MUST appear as a selectable filter option for Class, Size, and Grade only when records with a missing value for that attribute exist in the inventory.
- **FR-022**: The page MUST provide a clear/reset filters action that resets all filters to empty simultaneously.

**Add/Edit Behavior**

- **FR-023**: When a Quartermaster adds a new inventory record, the item selection flow MUST restrict available items to those in the Systems section only.
- **FR-024**: Name, Type, Class, Size, and Grade MUST be derived from item data and MUST NOT be manually editable on this page.
- **FR-025**: Quantity, Owner, and Location MUST be editable during add and edit operations, following the same rules as the existing Inventory page.
- **FR-026**: Row uniqueness behavior MUST remain unchanged: records with different owners, locations, or distinct properties remain separate rows.

**Empty States**

- **FR-027**: When no Ship Component inventory records exist, the page MUST display an appropriate empty state message.
- **FR-028**: When records exist but no results match active filters, the page MUST display a distinct "no results" empty state message.

### Key Entities

- **Ship Component Inventory Record**: An inventory entry for an item in the Systems section. Carries Quantity, Owner, and Location (editable), plus Name, Type, Class, Size, and Grade (derived from item/component data, read-only). Multiple records for the same item may exist when Owner or Location differs.
- **Item (Systems section)**: A warehouse item whose category section is Systems. Provides the derived attributes Name, Type, Class, Size, and Grade consumed by Ship Component inventory.
- **Quartermaster**: A user role granting create, update, and delete access to inventory records. Admins inherit this access automatically.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Authenticated users can navigate to the Ship Components page from any page in the application in two clicks or fewer.
- **SC-002**: The Ship Components page displays only Systems-section inventory records; no non-Systems items appear regardless of the size of total warehouse inventory.
- **SC-003**: All eight required columns (Name, Type, Class, Size, Grade, Quantity, Owner, Location) are present; Section is absent.
- **SC-004**: Records appear sorted by the five-level default sort (Name, Type, Size, Class, Grade ascending) on every page load without user action.
- **SC-005**: Any single filter or combination of filters returns correct results; no records outside the active filter criteria appear.
- **SC-006**: Applying and clearing filters completes within a time indistinguishable from normal page interaction (no perceptible delay beyond standard load times for the application).
- **SC-007**: A Quartermaster can complete an add, edit, or delete operation on a Ship Component inventory record without leaving the Ship Components page.
- **SC-008**: 100% of add-item flows initiated from the Ship Components page restrict item selection to Systems-section items only; no non-Systems item can be saved.
- **SC-009**: Unauthenticated users cannot reach the Ship Components page; 100% of such attempts result in redirect to the login flow.
- **SC-010**: Non-Quartermaster authenticated users see no add, edit, or delete controls on the Ship Components page.

## Assumptions

- The existing Inventory page's add, edit, and delete dialogs/flows can be reused or adapted for Ship Components with a Systems-section scope filter applied to item selection; no new modal or form pattern is required.
- "Admin automatically has Quartermaster-level access" is enforced at the same layer and by the same mechanism as on the existing Inventory page; no new role infrastructure is needed.
- Class, Size, and Grade are attributes already associated with items in the Systems section in the existing data model; this feature reads those attributes but does not define or import them.
- The Materials navigation item will be included in the navigation update but will link to a placeholder, stub route, or disabled state until its requirements are defined.
- Filter option lists for Type, Class, Size, Grade, Owner, and Location are derived at runtime from Ship Component inventory currently in the system, not from a static reference list.
- The "same unauthorized behavior as the existing Inventory page" means redirect to the application login page with the current URL preserved as a return destination.
- Row uniqueness rules from the existing Inventory page apply without change: the combination of item + owner + location determines uniqueness.
