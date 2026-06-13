# Feature Specification: Ship Data Import

**Feature Branch**: `006-ship-data-import`

**Created**: 2026-06-13

**Status**: Draft

**Input**: User description: "A data import page that allows an admin to trigger data imports from various sources for different types of data for things like ships, in game items, prices, etc. This initial pass will be for ships only. The admin should be able to trigger an import, view the data and modify the data if needed. The ship data will come from https://api.uexcorp.uk/2.0/vehicles and include all the information availalable in that dataset. The data rows will contain name, company_name, and a button to view details which will allow the admin to view the rest of the data for that ship record."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Trigger Ship Data Import (Priority: P1)

An admin navigates to the Data Import page and triggers a ship data import from the UEX Corp vehicle feed. The system fetches all available ship records and stores them locally. The admin sees a success confirmation with the count of records imported.

**Why this priority**: The import is the foundational capability; nothing else in this feature works until data has been imported at least once.

**Independent Test**: Can be fully tested by triggering an import and confirming that ship records appear in the data table, delivering a populated data set for downstream viewing.

**Acceptance Scenarios**:

1. **Given** no ship data has been imported, **When** the admin clicks "Import Ships", **Then** the system fetches all ship records from the UEX Corp vehicle feed, stores them, and displays a success message with the total number of records imported.
2. **Given** ship data was previously imported, **When** the admin triggers a new import, **Then** the system re-fetches all records and updates the stored data, refreshing the table view upon completion.
3. **Given** the admin triggers an import, **When** the external data source is unavailable, **Then** the system displays a clear error message explaining the failure, and the existing stored data remains unchanged.
4. **Given** the admin triggers an import, **When** the import is in progress, **Then** the import button is disabled and a progress indicator is shown so the admin cannot trigger a duplicate import.

---

### User Story 2 - View Imported Ship Records (Priority: P2)

After an import, the admin can view the list of imported ship records in a paginated table. Each row displays the ship name and manufacturer company name. The admin can browse all records efficiently.

**Why this priority**: Viewing the data is the primary way admins verify a successful import and locate records for review.

**Independent Test**: Can be fully tested by importing data and browsing the resulting table — delivers immediate value by confirming what was imported.

**Acceptance Scenarios**:

1. **Given** ship data has been imported, **When** the admin views the Data Import page, **Then** a table displays all imported ship records with at minimum the ship name and manufacturer company name visible per row.
2. **Given** more than one page of records exists, **When** the admin navigates through pages, **Then** the correct subset of records is displayed per page with appropriate pagination controls.
3. **Given** no data has been imported yet, **When** the admin views the page, **Then** an empty state message is shown with a prompt to trigger the first import.

---

### User Story 3 - View Full Ship Record Details (Priority: P3)

The admin can click a "View Details" button on any ship row to see the complete set of data fields available for that ship from the UEX Corp feed, presented in a readable format.

**Why this priority**: The table shows a summary; the full detail view allows admins to audit the complete record.

**Independent Test**: Can be fully tested by clicking "View Details" on any row and confirming all available data fields are displayed for that ship.

**Acceptance Scenarios**:

1. **Given** the ship list is displayed, **When** the admin clicks "View Details" on a row, **Then** a detail view (panel or page) opens showing all available data fields for that ship record.
2. **Given** the detail view is open, **When** the admin closes it, **Then** they are returned to the ship list with their place in the list preserved (page and scroll position unchanged).
3. **Given** the detail view is open, **When** the record contains optional fields with no data, **Then** those fields are displayed as clearly empty rather than hidden, so the admin can see the full data shape.
4. **Given** a record has been soft-deleted (removed from the source feed), **When** the admin views the detail, **Then** a visible indicator shows the record is no longer present in the source feed.

---

### Edge Cases

- What happens when the external data source returns zero records? The admin sees a warning that the feed returned no data, and the existing stored data is preserved unchanged.
- What happens if a network failure or feed error occurs mid-import? The import is rolled back entirely — any partially fetched data is discarded and the existing stored data remains unchanged. The admin sees an error message indicating the import failed.
- What happens when a re-import adds new ship records not present in the previous import? New records are added to the stored set; existing records are updated with the latest data from the feed.
- What happens when a record is deleted from the external feed but exists locally? The record is soft-deleted — flagged as "no longer in source feed" — and remains visible in the list with a clear indicator, but is not removed from the local data set.
- What happens when a previously soft-deleted record reappears in the feed? The record is automatically re-activated — the soft-delete flag is cleared and the record resumes active status with updated data from the feed.
- What happens when two admins trigger an import simultaneously? Only one import runs at a time; a concurrent attempt receives a message that an import is already in progress.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a dedicated Data Import administration page accessible only to admin users.
- **FR-002**: The system MUST allow an admin to trigger a ship data import from the UEX Corp vehicle feed on demand via a single action.
- **FR-003**: The system MUST prevent duplicate concurrent imports — if an import is already in progress, additional import requests MUST be rejected with a clear message.
- **FR-004**: The system MUST display the number of records imported (added or updated) after each successful import operation.
- **FR-005**: The system MUST display an error message when an import fails and MUST roll back any partial changes — existing stored data MUST remain entirely unchanged after a failed import.
- **FR-006**: The system MUST display imported ship records in a paginated table with at minimum ship name and manufacturer company name visible per row.
- **FR-007**: The system MUST show an appropriate empty state when no data has been imported yet.
- **FR-008**: Each ship record row MUST include a "View Details" action that opens the full set of data fields for that record.
- **FR-009**: Ship records removed from the UEX Corp feed on a subsequent import MUST be soft-deleted — flagged as no longer present in the source — rather than permanently removed from local storage.
- **FR-010**: Soft-deleted records MUST remain visible in the ship list with a clear visual indicator distinguishing them from active records.
- **FR-011**: If a previously soft-deleted ship record reappears in the UEX Corp feed on a subsequent import, the system MUST automatically re-activate it — clearing the soft-delete flag and updating its data — without requiring admin intervention.
- **FR-012**: The Data Import page MUST be extensible to support additional data types (in-game items, prices, etc.) in future iterations without redesigning the core layout.

### Key Entities

- **Ship Record**: Represents a single ship/vehicle from the UEX Corp feed. Contains all fields provided by the feed including name, manufacturer company name, and all other available attributes. Carries a status indicating whether it is active (present in the latest import) or soft-deleted (no longer returned by the feed).
- **Import Job**: Represents a single execution of the import process. Used internally to coordinate import state (pending, in progress, completed, failed) and enforce concurrency rules. Not exposed as a visible entity in the UI; outcomes are surfaced via inline success/error messages only.
- **Data Import Source**: A configured external data provider (initially UEX Corp vehicle feed). Defines what data type it provides (ships) and how to retrieve it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An admin can trigger a full ship data import and see the results reflected in the table in under 30 seconds for a typical feed size.
- **SC-002**: An admin can navigate to any page of imported ship records using the pagination controls; the table displays 25 records per page with clear prev/next (or numbered-page) navigation so any record is reachable through sequential browsing.
- **SC-003**: An admin can open the full detail view for a ship record, review all fields, and return to the list in under 60 seconds.
- **SC-004**: 100% of ship records from the UEX Corp feed are imported with no data loss for any field provided by the feed.
- **SC-005**: Records removed from the source feed are correctly identified and soft-deleted on the very next import run, with no manual intervention required.

## Clarifications

### Session 2026-06-13

- Q: Should the page display a history of past import runs (timestamps, status, record counts)? → A: No import history shown; only the current data table is displayed. Import Job entity is used internally for processing only and is not exposed in the UI.
- Q: When a previously soft-deleted ship record reappears in the feed on a subsequent import, should it be re-activated automatically or require admin action? → A: Automatically re-activate — the soft-delete flag is cleared and the record is treated as active again.
- Q: If a network failure or feed error occurs mid-import after some records have been fetched, what happens to the partial data? → A: Roll back entirely — discard all partial results and leave existing stored data unchanged.
- Q: Should a cooldown period be enforced between successive import runs? → A: No cooldown — admins may re-trigger an import immediately after the previous one completes; the concurrency lock is the only guard.

## Assumptions

- The Data Import page is restricted to admin roles only; no non-admin user should access or trigger imports.
- The UEX Corp vehicle feed is publicly accessible during import; no authentication credentials are required to call it (if credentials become needed, that is out of scope for this feature and will be handled separately).
- The initial implementation covers ships only; the page design anticipates future data types but does not implement them in this feature.
- Ship records are read-only after import; admins can view all data but cannot modify any field.
- Pagination default is 25 records per page; this is a standard web default and is not specified by the user.
- Basic text search/filtering of the ship list is out of scope for this initial pass; browsing by page is sufficient.
- Ship record identity across imports is determined by a stable identifier provided by the UEX Corp feed (e.g., a vehicle ID or unique code); if no stable identifier exists, this must be resolved during planning.
- No cooldown is enforced between successive imports; admins may re-trigger immediately after completion. The only concurrency constraint is that a second import cannot start while one is already in progress.
- Mobile responsiveness for this admin page is desirable but not a hard requirement for the initial pass.
