# Feature Specification: Add Cities Import & Rename Stations to Locations

**Feature Branch**: `018-add-cities-locations`

**Created**: 2026-06-22

**Status**: Draft

**Input**: Review feature 16. In addition to space stations, we're going to add an import for cities (https://api.uexcorp.uk/2.0/cities). On all warehouse pages we're going to rename "Stations" to "Locations" which will consist of both stations and cities.

---

## Clarifications

### Session 2026-06-22

- Q: How should a warehouse entry reference a location that may be either a station or a city (and potentially other types in future)? → A: Polymorphic reference — one nullable location FK plus a `location_type` discriminator column on the warehouse entry, so future location types require no new columns.
- Q: What availability/status flags does the UEX cities API provide, and which should be used to filter the active locations list? → A: Cities expose three flags — `is_available`, `is_available_live`, and `is_visible`. Apply only these fields as they actually exist; cities have no decommission flag. The locations endpoint filters cities where `is_available = 1` and `is_visible = 1`; `is_available_live` is stored but not used as a filter in this version.
- Q: If a city record has a null or missing star system reference, should it be skipped or imported as a parentless record? → A: Skip and count in "skipped" — consistent with the station import rule. A city without a parent star system reference is treated as incomplete data.
- Q: How should existing warehouse entries that already have a station FK (from feature 16) be handled when the schema migrates to the polymorphic model? → A: Migrate existing station FK values into the new polymorphic columns (location FK + `Station` discriminator) in the same migration, then drop the old station FK column. No data is lost and the application reads from one place only.
- Q: When displaying a location in a warehouse row, should the origin type (station vs. city) be shown to help members distinguish the two? → A: Assumed label only (no type tag) — members see the location name. Amend if a type indicator is wanted.
- Q: For the Edit modal introduced in feature 16, should cities appear in the same location combobox as stations (combined list), or as a separate picker? → A: Assumed combined "Locations" combobox sorted alphabetically by name.
- Q: Should the combined Location combobox group stations and cities by type, or present them as one flat alphabetical list? → A: Fully interleaved alphabetical list — no type grouping, no type badge on individual rows.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Trigger City Import (Priority: P1)

An admin navigates to the data import admin page and triggers the city import. The system fetches all cities from the UEX catalog and upserts them into a local cities catalog table. Previously imported cities no longer present in the source are soft-deleted. An import summary is displayed on completion.

**Why this priority**: Populating the city catalog is the prerequisite for cities to appear in any Location picker. Without this import the downstream stories cannot be tested. It is also a standalone, independently verifiable piece of work.

**Independent Test**: Can be fully tested by triggering the city import on the admin page and confirming cities are populated with records matching the UEX source, with soft-deletes applied for any previously present cities now absent from the source.

**Acceptance Scenarios**:

1. **Given** an admin is on the data import page, **When** they trigger the city import, **Then** the system fetches and upserts all city records into the local cities catalog.
2. **Given** the city import completes, **When** the admin views the result, **Then** a summary shows fetched, inserted, updated, soft-deleted, and skipped counts for cities.
3. **Given** a city existed in a previous import but is absent from the current source, **When** the import completes, **Then** that city is soft-deleted in the catalog.
4. **Given** the external city source is unreachable or returns an empty record set, **When** an admin triggers the import, **Then** the import fails with a clear error message and no changes are committed.

---

### User Story 2 — Select a Location (Station or City) When Adding or Editing a Warehouse Entry (Priority: P2)

A member adds a new entry (or edits an existing one) in any of the three warehouse features — item inventory, ship components, or materials. The Location combobox now presents both active space stations and active cities in a single, searchable list. The member selects a location and saves the entry.

**Why this priority**: This is the primary consumer-facing change. Once both catalogs are populated, the combined Location picker becomes the canonical way to record where an asset lives. Renaming the UI label from "Station" to "Location" is part of this story.

**Independent Test**: Can be fully tested by adding a new warehouse entry in any of the three features, selecting either a station or a city from the unified Location combobox, saving, and confirming the entry stores the chosen location reference correctly.

**Acceptance Scenarios**:

1. **Given** a member is on the add/edit dialog for any warehouse feature, **When** they interact with the Location field, **Then** a searchable combobox labelled "Location" (not "Station") is presented containing both active stations and active cities.
2. **Given** the Location combobox is open, **When** a member types partial text, **Then** the list filters to entries (stations and cities) whose names contain the typed text.
3. **Given** a member selects a city and saves the entry, **When** the entry is persisted, **Then** the canonical location reference is stored as that city.
4. **Given** a member selects a station and saves the entry, **When** the entry is persisted, **Then** the canonical location reference is stored as that station.
5. **Given** no stations or cities have been imported yet, **When** a member opens the Location combobox, **Then** the combobox is empty with an appropriate empty-state message.

---

### User Story 3 — Edit a Warehouse Entry's Location to a Station or City (Priority: P3)

A member selects the Edit action on an existing warehouse row. The Edit modal's location combobox is relabelled "Location" and now lists both active stations and active cities. The member selects any location (station or city), optionally updates quantity and/or owner, and confirms; the entry is updated.

**Why this priority**: Builds directly on the unified Edit modal from feature 16. Extending the location combobox to include cities is a small, targeted change that makes the Edit workflow consistent with the updated Location concept.

**Independent Test**: Can be fully tested by triggering Edit on an existing warehouse row, selecting a city as the location, confirming, and verifying the entry's stored location reference reflects the chosen city.

**Acceptance Scenarios**:

1. **Given** a member opens the Edit modal for a warehouse row, **When** the modal opens, **Then** the location combobox is labelled "Location" and contains both active stations and active cities.
2. **Given** a member selects a city and confirms the edit, **When** the edit is saved, **Then** the warehouse row's location reference is updated to that city.
3. **Given** a member cancels the Edit dialog, **When** the modal is dismissed, **Then** the warehouse row is unchanged.

---

### User Story 4 — Rename "Station" Labels to "Location" Across All Warehouse Pages (Priority: P4)

Every visible reference to "Station" in the warehouse features (column headers, form labels, filter labels, empty-state messages, button tooltips) is renamed to "Location". The underlying behavior is unchanged; only the displayed text updates.

**Why this priority**: A pure label change with no data-model impact. It can be done before or after the import and combobox changes, but must be complete before any warehouse page ships with the new feature so users see a consistent "Location" vocabulary throughout.

**Independent Test**: Can be fully tested by visiting each of the three warehouse pages (item inventory, ship components, materials) and confirming no visible text reads "Station" in the context of location selection or display.

**Acceptance Scenarios**:

1. **Given** a member views any warehouse list page, **When** they read column headers and labels, **Then** no column, label, or tooltip uses the word "Station" in the context of location — all read "Location".
2. **Given** a member opens the add/edit dialog for any warehouse feature, **When** they view the form, **Then** the location input label reads "Location" (not "Station").
3. **Given** a member views an existing warehouse row whose location is a station, **When** they read the row, **Then** the station name is displayed in the "Location" column without a "Station" type indicator.

---

### Edge Cases

- What happens when the city source returns an empty record set? The import treats this as an error — aborts, displays an error message, and commits no changes. Consistent with the behavior defined in feature 16 for space stations.
- What happens when a city record has a null or missing star system reference, or references a star system ID not present in the local catalog? The record is skipped and counted in the "skipped" import summary. Both conditions are treated identically.
- What happens when a member opens the Location combobox and both catalogs are empty? The combobox is empty; the deprecated free-text Location field (retained from feature 16) remains available as a fallback.
- What happens if a warehouse entry's previously referenced station or city is subsequently soft-deleted via import? The entry retains its stored reference, and the location name continues to display using the last known name.
- What happens when a member searches the Location combobox and the query matches both a station name and a city name? Both appear in the combined filtered list.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST expose a city import action on the admin data import page, triggerable only by authenticated admins.
- **FR-002**: The city import MUST fetch all city records from the UEX external catalog and upsert them into the local cities catalog table.
- **FR-003**: The city import MUST soft-delete any previously imported city record absent from the current source during an import run.
- **FR-004**: The city import MUST display an import summary after each run showing fetched, inserted, updated, soft-deleted, and skipped counts for cities.
- **FR-005**: The city import MUST fail with a clear error message and commit no changes if the external source is unreachable or returns an empty record set.
- **FR-006**: City records with a null or missing star system reference, or referencing a star system ID not present in the local catalog, MUST be skipped and counted in the "skipped" summary.
- **FR-007**: The system MUST expose a read-only endpoint listing all active locations (stations and cities combined) for use in frontend comboboxes, accessible to any authenticated member.
- **FR-008**: The locations endpoint MUST return only active records. For stations, the existing filter applies (availability active, not decommissioned). For cities, the filter is `is_available = 1` and `is_visible = 1`; cities have no decommission flag. The `is_available_live` flag is stored on the city record but is not used as a filter in this version.
- **FR-009**: All three warehouse features (item inventory, ship components, materials) MUST present a unified, searchable Location combobox that includes both active stations and active cities, sorted as a single flat alphabetical list with no type grouping or type badges.
- **FR-010**: The Location combobox MUST be labelled "Location" everywhere it appears — in add/edit dialogs, Transfer modals, column headers, filter labels, and any other visible context.
- **FR-011**: Every visible reference to "Station" in the warehouse features (column headers, form labels, empty-state messages, button tooltips) MUST be renamed to "Location".
- **FR-012**: The Edit modal from feature 16 MUST be updated so its location combobox includes both stations and cities under the "Location" label.
- **FR-013**: The Edit modal MUST continue to pre-populate the location field with the row's current location, which may now be a station or a city.
- **FR-015**: The schema migration MUST copy all existing station FK values from warehouse entries into the new polymorphic location columns (with `location_type = Station`) and then drop the old station FK column. No existing station location data may be lost.
- **FR-014**: A warehouse entry's canonical location reference MUST be stored as a polymorphic reference — a single nullable location FK combined with a `location_type` discriminator — so that future location types can be added without schema column proliferation. Valid discriminator values in this version are `Station` and `City`.

### Key Entities

- **City**: A player-accessible city on a planet or moon within a star system. Identified by a unique external ID, a reference to its parent star system, a full name, and three status flags: `is_available`, `is_available_live`, and `is_visible`. Has no decommission flag. Treated as a peer of Space Station for location-selection purposes.
- **Location** (unified concept): A combined set of Space Stations and Cities presented to members as a single searchable list wherever location selection occurs in the warehouse features.
- **Warehouse Entry** (Item Inventory / Ship Component / Material): Gains a polymorphic location reference — a single nullable FK plus a `location_type` discriminator (`Station` or `City`) — replacing the station-only FK introduced in feature 16. The deprecated free-text Location string is retained for backward compatibility. The polymorphic design accommodates additional location types in future without adding new FK columns.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An admin can trigger the city import and see a populated cities catalog within a reasonable completion time for the expected data volume from the UEX source.
- **SC-002**: Following successful station and city imports, all three warehouse add/edit dialogs present a non-empty, combined Location list without additional configuration.
- **SC-003**: Members can type a partial name in the Location combobox and see a filtered list of matching stations and cities within a single interaction.
- **SC-004**: A warehouse entry saved with a selected city accurately retains the canonical city reference when retrieved in a subsequent session.
- **SC-005**: A city import run with an unreachable source leaves the existing catalog intact — zero rows inserted, updated, or deleted.
- **SC-006**: No visible use of the word "Station" remains in the warehouse features after the label rename — confirmed by reviewing all three warehouse pages.
- **SC-007**: The Edit action allows a member to change a warehouse entry's location to a city (or from a city to a station) in a single modal interaction (open edit, select location, confirm).
- **SC-008**: After the schema migration, all warehouse entries that previously held a station reference continue to display the correct station name in the Location column — zero entries lose their location data.

---

## Assumptions

- The UEX Corp API (`/2.0/cities`) is publicly accessible without authentication from the server side, consistent with the star systems and space stations endpoints used in feature 16.
- City records from the UEX source include a parent star system reference, allowing the same skipping logic used for space stations to apply.
- The volume of cities from the UEX source is small enough (tens to low hundreds) to be fetched and upserted in a single synchronous request within a normal HTTP timeout.
- The warehouse entry's canonical location reference is modeled as a single polymorphic reference: one nullable location FK plus a `location_type` discriminator column (e.g., `Station`, `City`). This approach was chosen to avoid accumulating nullable FK columns as future location types are added.
- Existing warehouse entries that already reference a space station (from feature 16) will have those station references migrated to the new polymorphic columns in the same schema migration that introduces the polymorphic model. The old station FK column is dropped after migration. No station location data is lost.
- The city import follows the same pipeline pattern established for star systems and space stations in feature 16 — no new abstractions are introduced.
- Cities in the UEX API carry `is_available`, `is_available_live`, and `is_visible` flags but no decommission flag. The active-filter for cities uses `is_available` and `is_visible` only; `is_available_live` is persisted for future use.
- Filtering cities by capability (e.g., "only cities with a trading post") is out of scope for this version.
- Scheduled or automatic refresh of the city catalog is out of scope; import is admin-triggered only.
- The combined Location combobox presents stations and cities as a single flat alphabetical list with no type indicator, grouping, or badges. Members are expected to recognize location names without a type label.
