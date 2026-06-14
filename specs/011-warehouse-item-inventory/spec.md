# Feature Specification: Warehouse Item Inventory

**Feature Branch**: `011-warehouse-item-inventory`

**Created**: 2026-06-14

**Status**: Draft

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Item Inventory (Priority: P1)

Any authenticated member of the portal can navigate to the Warehouse section and see the current item inventory. The inventory table displays each item's Name, Type, Subtype, Quantity, Owner, and Location, sorted by Name ascending. Members can apply one or more optional filters to narrow the list.

**Why this priority**: Read access is the most-used capability; it gates all other inventory workflows and delivers immediate value with no write permissions required.

**Independent Test**: Can be fully tested by signing in as a non-Quartermaster user, navigating to the Warehouse page, and confirming the inventory table is visible and filterable.

**Acceptance Scenarios**:

1. **Given** an authenticated user is on the Warehouse page, **When** the page loads, **Then** the inventory list is displayed with columns Name, Type, Subtype, Quantity, Owner, and Location, sorted by Name ascending.
2. **Given** an authenticated user, **When** they enter a partial name in the Name filter, **Then** only rows whose item name contains that text (case-insensitive) are shown.
3. **Given** an authenticated user, **When** they select a Type filter value, **Then** only rows matching that Type are shown.
4. **Given** an authenticated user, **When** they select a Subtype filter value, **Then** only rows matching that Subtype are shown.
5. **Given** an authenticated user, **When** they select an Owner filter value, **Then** only rows matching that registered portal user are shown.
6. **Given** an authenticated user, **When** they enter a partial location in the Location filter, **Then** only rows whose location contains that text (case-insensitive) are shown.
7. **Given** multiple filters are active, **When** the inventory renders, **Then** only rows satisfying all active filters (AND logic) are shown.
8. **Given** an anonymous (unauthenticated) visitor, **When** they navigate to the Warehouse page, **Then** they are redirected to the sign-in page and cannot view inventory.

---

### User Story 2 - Add Item to Inventory (Priority: P2)

A Quartermaster navigates to the Warehouse page, searches for an item from the existing item catalog, specifies the quantity, owner, and location, and submits the addition. If the same item already exists for the same owner and location, the quantity is incremented by the entered amount. If the owner or location differs, a new row is created.

**Why this priority**: Adding inventory is the primary write operation and is prerequisite to all inventory management workflows.

**Independent Test**: Can be fully tested by signing in as a Quartermaster, adding a new item to a new location, confirming the row appears, then adding the same item to the same location and confirming the quantity incremented.

**Acceptance Scenarios**:

1. **Given** a Quartermaster on the Warehouse page, **When** they initiate the add-item flow and search for an item by name, **Then** matching items from the catalog are displayed with Name, Type, and Subtype shown for each result.
2. **Given** a Quartermaster has selected an item, **When** the add form is shown, **Then** Owner defaults to the currently signed-in user and Quantity defaults to 1.
3. **Given** a Quartermaster submits a valid add (item, quantity ≥ 1, owner, non-empty location), **When** no matching row exists for that Item + Owner + Location, **Then** a new inventory row is created and appears in the inventory list.
4. **Given** a Quartermaster submits an add for an Item + Owner + Location that already exists, **When** the submission is processed, **Then** the existing row's quantity is incremented by the submitted quantity.
5. **Given** a Quartermaster submits an add for an item that exists under a different owner, **When** the submission is processed, **Then** a separate inventory row is created for the new owner.
6. **Given** a Quartermaster submits an add for an item that exists at a different location, **When** the submission is processed, **Then** a separate inventory row is created for the new location.
7. **Given** a Quartermaster has successfully added an item, **When** they add a second item, **Then** the Owner and Location fields are pre-populated with the values from the previous add (remembered for the page lifetime).
8. **Given** a non-Quartermaster authenticated user on the Warehouse page, **When** they attempt to access the add-item flow, **Then** the option is not available or access is denied.
9. **Given** a Quartermaster submits the add form with a quantity less than 1 or a non-whole number, **When** validation runs, **Then** an error is shown and the item is not added.
10. **Given** a Quartermaster submits the add form with an empty location, **When** validation runs, **Then** an error is shown and the item is not added.

---

### User Story 3 - Change Item Quantity (Priority: P3)

A Quartermaster edits the quantity on an existing inventory row. The new value replaces the old value. The quantity must be a whole number of at least 1.

**Why this priority**: Quantity correction is a common maintenance action but is secondary to initially adding inventory.

**Independent Test**: Can be fully tested by signing in as a Quartermaster, editing the quantity of an existing row, confirming the new value is saved, and confirming that setting quantity to 0 or a non-integer is rejected.

**Acceptance Scenarios**:

1. **Given** a Quartermaster views an inventory row, **When** they edit the quantity and submit a whole number ≥ 1, **Then** the row's quantity is updated to the new value.
2. **Given** a Quartermaster attempts to set quantity to 0, **When** they submit, **Then** an error is shown and the quantity is not changed.
3. **Given** a Quartermaster attempts to set a non-whole-number quantity, **When** they submit, **Then** an error is shown and the quantity is not changed.
4. **Given** a non-Quartermaster authenticated user, **When** they view the inventory, **Then** no quantity-edit control is available to them.

---

### User Story 4 - Remove Inventory Row (Priority: P3)

A Quartermaster removes an inventory row. The specific Item + Owner + Location entry is deleted from the inventory. The underlying item in the item catalog is unaffected.

**Why this priority**: Row removal is a less frequent action; adding and viewing inventory are more critical to deliver first.

**Independent Test**: Can be fully tested by signing in as a Quartermaster, removing an inventory row, confirming it no longer appears in the list, and confirming the item still exists in the item catalog.

**Acceptance Scenarios**:

1. **Given** a Quartermaster views an inventory row, **When** they initiate and confirm the remove action, **Then** the inventory row is deleted and no longer appears in the inventory list.
2. **Given** an inventory row is removed, **When** the item catalog is checked, **Then** the item record still exists.
3. **Given** a non-Quartermaster authenticated user, **When** they view the inventory, **Then** no remove control is available to them.

---

### Edge Cases

- What happens when the item catalog is empty? The item search in the add flow returns no results and shows a helpful message.
- What happens when the inventory list is empty? The inventory table shows an empty state message rather than a blank table.
- What happens when filters return no results? The inventory table shows a no-results message while filters remain active.
- What happens when a Quartermaster searches for an item by a name that matches many results? The search returns a manageable paginated or truncated list (assumed: top results up to a reasonable limit).
- What happens when location input contains leading or trailing whitespace? The system trims the whitespace before saving, so "Bay 3 " and "Bay 3" are treated as the same location.
- What happens when Owner and Location remembered values are no longer valid after a page reload? They are cleared on reload — remembered values are session-only.
- What happens when the add form is submitted concurrently for the same Item + Owner + Location? The system ensures exactly one increment, preventing duplicate rows.

## Requirements *(mandatory)*

### Functional Requirements

**Viewing Inventory**

- **FR-001**: The system MUST restrict warehouse inventory access to authenticated users only; unauthenticated visitors MUST be redirected to the sign-in page.
- **FR-002**: The inventory list MUST display the following columns for each entry: Name, Type, Subtype, Quantity, Owner, and Location.
- **FR-003**: The inventory list MUST be sorted by item Name ascending by default.
- **FR-004**: Users MUST be able to filter the inventory by Name (partial text, case-insensitive), Type (exact match), Subtype (exact match), Owner (registered portal user), and Location (partial text, case-insensitive).
- **FR-005**: All filters MUST be optional; an inventory with no active filters MUST show all entries.
- **FR-006**: When multiple filters are active, the system MUST return only rows that satisfy ALL active filter conditions (AND logic).
- **FR-007**: Type and Subtype filter values MUST be derived from the existing item category data (Type = category section, Subtype = category name).

**Adding Inventory**

- **FR-008**: Only users holding the Quartermaster role or the Admin role MUST be permitted to add, edit, or remove inventory entries.
- **FR-009**: The add-item flow MUST provide an item search that searches the existing item catalog by item name.
- **FR-010**: Item search results MUST display Name, Type, and Subtype for each result to distinguish similar items.
- **FR-011**: The add form MUST default Owner to the currently signed-in user.
- **FR-012**: The add form MUST default Quantity to 1.
- **FR-013**: Location MUST be required; the system MUST reject submissions with an empty or whitespace-only location.
- **FR-014**: The system MUST trim leading and trailing whitespace from location values before saving.
- **FR-015**: After a successful add, the Owner and Location values MUST be retained in the form for the current page lifetime to allow rapid entry of multiple items.
- **FR-016**: Retained Owner and Location values MUST be cleared when the page is reloaded.
- **FR-017**: When the submitted Item + Owner + Location combination already exists in inventory, the system MUST increment the existing row's quantity by the submitted quantity rather than creating a duplicate row.
- **FR-018**: When the submitted Item + Owner + Location combination does not exist, the system MUST create a new inventory row.
- **FR-019**: Inventory entries with the same item but a different owner or a different location MUST be stored as separate rows.

**Quantity Rules**

- **FR-020**: Quantity MUST be required.
- **FR-021**: Quantity MUST be a whole number (integer).
- **FR-022**: Quantity MUST be at least 1; a value of 0 or negative MUST be rejected.
- **FR-023**: When a Quartermaster edits quantity on an existing row, the new value MUST replace the existing value (not be added to it).

**Removing Inventory**

- **FR-024**: A Quartermaster MUST be able to remove an inventory row, which deletes the specific Item + Owner + Location entry.
- **FR-025**: Removing an inventory row MUST NOT delete or affect the corresponding item in the item catalog.

**Role**

- **FR-026**: The system MUST recognize a Quartermaster role; users assigned this role gain inventory write permissions.
- **FR-027**: Admins MUST automatically have all Quartermaster permissions without requiring explicit Quartermaster role assignment.
- **FR-028**: Role assignment is performed directly in the data store; no role-management UI is provided in this version.

### Key Entities

- **WarehouseInventory**: Represents a single inventory entry. Unique by Item + Owner + Location. Attributes: item reference, owner (portal user), location (free text), quantity (positive integer).
- **Item**: An item from the existing item catalog. Provides Name, Type (category section), and Subtype (category name). Read-only from the warehouse's perspective.
- **Owner**: A registered portal user. Referenced by inventory entries to indicate who owns the stock.
- **Quartermaster Role**: A portal role granting write access to warehouse inventory. Assigned manually in this version.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Any authenticated user can navigate to the Warehouse page and see the full inventory list within 2 seconds under normal load.
- **SC-002**: Applying one or more filters produces a visibly updated inventory list within 1 second.
- **SC-003**: A Quartermaster can complete the add-item flow (search, select, fill fields, submit) in under 60 seconds for a known item.
- **SC-004**: Repeated adds of known items to a remembered owner and location can be completed in under 15 seconds per item after the first add (thanks to remembered fields).
- **SC-005**: 100% of inventory write actions (add, quantity change, remove) are inaccessible to non-Quartermaster, non-Admin authenticated users — no write operation succeeds without the appropriate role.
- **SC-006**: Unauthenticated access to the Warehouse page results in a redirect to sign-in 100% of the time — no inventory data is exposed to anonymous users.

## Assumptions

- The Quartermaster role is a new portal role. For this version, it is assigned directly in the data store by an administrator; no in-app role-management UI is provided.
- Admins inherit Quartermaster permissions automatically through the existing admin role hierarchy — no additional configuration is needed per admin.
- The existing item catalog (Items and their category data) is the sole source of truth for which items can be added to inventory; Quartermasters cannot create new items through the warehouse interface.
- Owner refers exclusively to registered portal users; inventory owned by the organisation as a whole (rather than an individual) is out of scope for this version.
- Location is free-form text in this version; there is no structured location registry, hierarchy, or validation beyond non-empty and trimming.
- Type and Subtype filter options are populated from existing item category data and do not require a separate data entry workflow.
- Item search results are limited to a practical number of results to keep the selection UI usable; exact limit to be determined during planning.
- The remembered Owner and Location values live only in the browser's page state; they do not persist across reloads, tabs, or sessions.
- Concurrent writes for the same Item + Owner + Location are handled safely by the system without producing duplicate rows or incorrect quantities.
- Mineral inventory, inventory audit/history, inventory transaction ledgers, structured location management, and org-owned inventory are explicitly out of scope for this version.

## Out of Scope

- Mineral inventory
- Role assignment UI (Quartermaster role assigned directly in the data store)
- Inventory audit trail or history
- Inventory transaction ledger
- Organisation-owned inventory (inventory must have an individual owner)
- Structured location management (locations are free text only)
- Anonymous or public warehouse access
