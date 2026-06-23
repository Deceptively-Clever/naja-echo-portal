# Feature Specification: Star Systems & Space Station Import

**Feature Branch**: `016-star-systems-station-import`

**Created**: 2026-06-20

**Status**: Draft

**Input**: Import star systems and space stations from the UEX Corp API into new catalog tables. The imported station list drives a structured Location combobox across all three warehouse features (items inventory, ship components, materials). Warehouse entities retain the existing free-text Location string column and gain a new nullable FK column pointing to the space stations catalog. A Transfer action on each warehouse row allows moving an item's location by selecting a station.

---

## Clarifications

### Session 2026-06-20

- Q: If the UEX source returns a valid but empty record set, should the import treat this as an error or a no-op? → A: Treat as an error — abort import, show error message, commit no changes.
- Q: Should location updates use a dedicated Transfer action or a unified Edit action? → A: Unified Edit modal covering location, quantity, and owner. The Edit modal pre-populates all fields with the row's current values.
- Q: When a member selects a station from the combobox, should the free-text Location field be auto-populated? → A: The station reference (new field) is the canonical field being populated. The old free-text Location field is deprecated and will be removed in a future update; it is retained in this version for backward compatibility only.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Trigger Star Systems & Space Station Import (Priority: P1)

An admin navigates to the data import admin page and triggers the star systems and space station import. The system fetches all star systems from the UEX catalog, upserts them into the local catalog, then fetches all space stations and upserts them into a separate catalog table. Previously imported records no longer present in the source are soft-deleted. An import summary is displayed on completion.

**Why this priority**: Populating the station catalog is the prerequisite for all downstream value — without it, no structured location selection is possible. This story delivers a standalone, verifiable catalog populated with authoritative game data.

**Independent Test**: Can be fully tested by triggering the import on the admin page and confirming star systems and space stations are populated with records matching the UEX source, with soft-deletes applied for any previously present records now absent from the source.

**Acceptance Scenarios**:

1. **Given** an admin is on the data import page, **When** they trigger the star systems import, **Then** the system fetches and upserts all star system records into the local catalog.
2. **Given** an admin triggers the space stations import, **When** the source returns records, **Then** all station records are upserted with correct references to their parent star systems.
3. **Given** a station existed in a previous import but is absent from the current source, **When** the import completes, **Then** that station is soft-deleted in the catalog.
4. **Given** the import completes, **When** the admin views the result, **Then** a summary shows fetched / inserted / updated / soft-deleted / skipped counts for each entity type (star systems and space stations separately).
5. **Given** the external catalog source is unreachable, **When** an admin triggers the import, **Then** the import fails with a clear error message and no changes are committed.

---

### User Story 2 — Select Station Location When Adding or Editing a Warehouse Entry (Priority: P2)

A member adds a new entry (or edits an existing one) in any of the three warehouse features — item inventory, ship components, or materials. The Location field presents a searchable dropdown of active, non-decommissioned space stations instead of a free-text input. The member selects a station and saves the entry.

**Why this priority**: This is the primary consumer of the imported station catalog. Once the catalog exists, members should be able to use it immediately when creating new warehouse entries. It eliminates location typos and duplicates for all new entries without affecting existing data.

**Independent Test**: Can be fully tested by adding a new warehouse entry in any of the three features, selecting a station from the Location dropdown, saving, and confirming the entry is stored with the canonical station reference.

**Acceptance Scenarios**:

1. **Given** a member is on the add/edit dialog for any warehouse feature, **When** they interact with the Location field, **Then** a searchable dropdown of active, non-decommissioned space stations is presented.
2. **Given** the station dropdown is open, **When** a member types partial text, **Then** the list filters to stations whose names contain the typed text.
3. **Given** a member selects a station and saves the entry, **When** the entry is persisted, **Then** the canonical station reference is stored as the entry's location.
4. **Given** no stations have been imported yet, **When** a member opens the Location combobox, **Then** the combobox is empty; the deprecated free-text field remains visible for backward compatibility.

---

### User Story 3 — Edit a Warehouse Entry's Location, Quantity, and Owner (Priority: P3)

A member selects an Edit action on an existing warehouse row (item, ship component, or material). A modal dialog opens with fields for location (station combobox), quantity, and owner. The member can update any combination of these fields. On confirmation, the entry is updated with the new values.

**Why this priority**: Enables members to update key fields of an existing entry without a separate workflow per field. Builds on the station catalog and dropdown work already done in P2, providing a unified edit experience for the fields most likely to change after initial entry.

**Independent Test**: Can be fully tested by triggering Edit on an existing warehouse row, changing the station, quantity, and/or owner, confirming, and verifying all changed fields are persisted correctly.

**Acceptance Scenarios**:

1. **Given** a member views a warehouse row, **When** they select the Edit action, **Then** a modal dialog opens pre-populated with the row's current location, quantity, and owner.
2. **Given** a member changes the station in the Edit modal and confirms, **When** the edit is saved, **Then** the warehouse row's station reference is updated to the chosen station.
3. **Given** a member changes the quantity in the Edit modal and confirms, **When** the edit is saved, **Then** the warehouse row's quantity reflects the new value.
4. **Given** a member changes the owner in the Edit modal and confirms, **When** the edit is saved, **Then** the warehouse row's owner reflects the new value.
5. **Given** a member opens the Edit dialog, **When** they cancel without saving, **Then** the warehouse row is unchanged.

---

### Edge Cases

- What happens when the external catalog source returns an empty list? The import treats this as an error — it aborts, displays an error message, and commits no changes. An empty result is considered a suspicious source condition, not a legitimate empty catalog state.
- What happens when a space station references a star system ID not yet present in the local catalog? The record should be skipped with an appropriate count in the "skipped" summary field.
- What happens when a member adds a warehouse entry without selecting a station? The free-text Location field remains available as a fallback; the station FK column is left null.
- What happens when the only available stations are all decommissioned or unavailable? The dropdown is empty; the member can still use the free-text Location field.
- What happens when a member opens the Edit dialog and cancels without saving? The warehouse row is unchanged — no partial updates are applied.
- What happens when an entry's previously referenced station is subsequently soft-deleted via import? The entry retains its stored reference (no cascade), and the station name continues to display using the last known name.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST fetch all star system records from the UEX external catalog on admin request and upsert them into the local star systems catalog.
- **FR-002**: The system MUST fetch all space station records from the UEX external catalog on admin request and upsert them into the local space stations catalog, preserving each station's reference to its parent star system.
- **FR-003**: The system MUST soft-delete any previously imported catalog record that is absent from the current source during an import run.
- **FR-004**: The import MUST be triggerable only by authenticated admins from the existing data import admin page.
- **FR-005**: The system MUST display an import summary after each run showing fetched, inserted, updated, soft-deleted, and skipped counts separately for star systems and space stations.
- **FR-006**: The system MUST expose a read-only endpoint listing space stations for use in frontend dropdowns, accessible to any authenticated member.
- **FR-007**: The space station list endpoint MUST return only stations where availability is active and decommissioned status is false.
- **FR-008**: All three warehouse features (item inventory, ship components, materials) MUST present a searchable station combobox in their add/edit dialogs for the Location field.
- **FR-009**: The station combobox MUST display the full station name (e.g., "ARC-L1 Wide Forest Station").
- **FR-010**: The new nullable station reference column is the canonical Location field for warehouse entries. The existing free-text Location string column is retained in this version for backward compatibility only and is considered deprecated; it will be removed in a future update.
- **FR-011**: Each warehouse row MUST expose an Edit action that opens a modal dialog with fields for location (station combobox), quantity, and owner, allowing a member to update any combination of these fields.
- **FR-014**: The Edit modal MUST pre-populate all fields with the row's current values when opened.
- **FR-012**: The import MUST fail with a clear error message and commit no changes if the external catalog source is unreachable or returns an empty record set.
- **FR-013**: Space station records referencing a star system ID not present in the local catalog MUST be skipped and counted in the "skipped" import summary.

### Key Entities

- **Star System**: A planetary system within Star Citizen. Identified by a unique external ID, a name, a short code, and availability flags. Parent of space stations.
- **Space Station**: A player-accessible station within a star system. Identified by a unique external ID, a reference to its parent star system, a full name, a nickname, availability status, decommissioned status, landable status, and capability flags (refinery, trading post, etc.).
- **Warehouse Entry** (Item Inventory / Ship Component / Material): An existing catalog entity representing an org-owned item stored at a location. Gains a new nullable station reference as the canonical location field. The existing free-text Location string is deprecated (retained this version for backward compatibility, removed in a future update).

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An admin can trigger the full star systems + space stations import and see a populated catalog within a reasonable completion time for the expected data volume from the UEX source.
- **SC-002**: Following a successful import, all three warehouse add/edit dialogs present a non-empty, searchable station list without additional configuration.
- **SC-003**: Members can type a partial station name in the Location combobox and see a filtered list of matching stations within a single interaction.
- **SC-004**: A warehouse entry saved with a selected station accurately retains the canonical station reference when retrieved in a subsequent session.
- **SC-005**: The Edit action allows a member to update a warehouse entry's location, quantity, and/or owner in a single modal interaction (open edit, change fields, confirm).
- **SC-006**: An import run with an unreachable source leaves the existing catalog intact — zero rows inserted, updated, or deleted.
- **SC-007**: Existing warehouse entries with only a deprecated free-text Location value continue to display and function correctly after the database schema change; no existing data is lost or corrupted.

---

## Assumptions

- The UEX Corp API (`/2.0/star_systems` and `/2.0/space_stations`) is publicly accessible without authentication from the server side; no API key management is required.
- The volume of star systems and space stations from the UEX source is small enough (tens to low hundreds) to be fetched and upserted in a single synchronous request from the admin page within a normal HTTP timeout.
- Existing warehouse entries are not migrated to the new station reference column; the deprecated free-text Location column remains populated as-is for pre-existing rows. Migration of existing rows is out of scope for this version.
- The station combobox reuses the existing combobox component pattern already used in the warehouse features (e.g., material/item selection), not a bespoke control.
- Capability-based filtering of the station dropdown (e.g., "only show stations with a refinery") is out of scope for this version.
- Scheduled or automatic refresh of the station catalog is out of scope; import is admin-triggered only.
- Importing planets, moons, cities, orbits, or other location types beyond star systems and space stations is out of scope for this version.
- The same import pipeline pattern used for commodity, item, and ship data imports applies here without new abstractions.
- The Transfer action updates only the new station reference column; it does not alter the deprecated free-text Location string.
