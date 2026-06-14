# Feature Specification: Commodity Data Import

**Feature Branch**: `010-commodity-data-import`

**Created**: 2026-06-14

**Status**: Draft

**Input**: User description: "Add admin-only commodity data import feature from UEX API"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Trigger Commodity Import (Priority: P1)

An admin user navigates to the data import admin page and triggers a commodity catalog import. The system fetches all commodity records from the UEX source, upserts them locally, soft-deletes any previously imported commodities no longer present in the source, and reports a completion summary.

**Why this priority**: This is the core action the feature exists to enable — without it, no commodity data is available for downstream use.

**Independent Test**: Can be tested end-to-end by triggering the import and verifying that the local commodities table is populated with records that match the source data, including boolean flag normalization and parsed location IDs.

**Acceptance Scenarios**:

1. **Given** the admin is authenticated and on the data import page, **When** they trigger the commodity import, **Then** the system fetches the commodity catalog from the UEX source and upserts all valid records into the local commodities table.
2. **Given** the import is triggered, **When** the source returns records with integer flag fields (e.g., `is_available = 1`), **Then** those flags are stored as boolean values in the local table.
3. **Given** the import is triggered, **When** the source returns records with comma-separated location ID fields (e.g., `ids_star_systems = "1,4,7"`), **Then** both the raw string and the parsed integer array are stored locally.
4. **Given** the import is triggered, **When** the source returns records with Unix timestamp fields (`date_added`, `date_modified`), **Then** both the raw integer value and the converted UTC datetime are stored locally.
5. **Given** the import is triggered, **When** the source returns a commodity with a `null` UUID, **Then** the record is still imported (UUID is not required).
6. **Given** the import completes, **When** the admin views the result, **Then** a summary is displayed showing counts of records fetched, inserted, updated, restored, soft-deleted, and skipped.

---

### User Story 2 - Skip Invalid Records Without Halting (Priority: P2)

During an import, individual source records that are missing a required `id` or `name` field are skipped silently, while the remainder of the import continues.

**Why this priority**: Source data quality cannot be guaranteed; the import must be resilient to individual bad records without discarding an otherwise valid dataset.

**Independent Test**: Can be tested by simulating a source payload containing one record missing `name` and verifying that record is excluded while all other records are imported.

**Acceptance Scenarios**:

1. **Given** a source record is missing the `id` field, **When** the import processes that record, **Then** the record is skipped and the import continues with the next record.
2. **Given** a source record is missing the `name` field, **When** the import processes that record, **Then** the record is skipped and the import continues with the next record.
3. **Given** some records are skipped, **When** the import completes, **Then** the count of skipped records is reflected in the completion summary.

---

### User Story 3 - Soft Delete Removed Commodities (Priority: P2)

When a commodity exists in the local table but is absent from a subsequent full source import, it is soft-deleted rather than permanently removed.

**Why this priority**: Soft deletion preserves referential integrity and historical data while accurately reflecting the current source catalog.

**Independent Test**: Can be tested by first running a full import, then running a second import from a source payload that omits a previously imported commodity, and verifying the omitted commodity is marked as deleted rather than removed.

**Acceptance Scenarios**:

1. **Given** commodity A was imported in a previous run, **When** a new full import runs and commodity A is absent from the source, **Then** commodity A is soft-deleted in the local table.
2. **Given** commodity A is soft-deleted, **When** a subsequent import includes commodity A again, **Then** commodity A is restored (undeleted) and updated with current source data.

---

### User Story 4 - Fail Import on Source Unavailability or Invalid Shape (Priority: P1)

If the UEX source endpoint cannot be reached, or the response does not match the expected data shape, the entire import fails with a clear error message. No partial data is committed.

**Why this priority**: Importing from a broken or unexpected source could corrupt the commodity catalog; failing fast is safer than partial imports.

**Independent Test**: Can be tested by simulating an unreachable endpoint or a malformed response and verifying the import is aborted with an error and the local table is unchanged.

**Acceptance Scenarios**:

1. **Given** the UEX endpoint is unreachable, **When** the admin triggers an import, **Then** the import fails with an error message and no changes are made to the local commodities table.
2. **Given** the UEX endpoint returns a response with an unexpected shape (e.g., missing the top-level data array), **When** the import processes the response, **Then** the import fails with an error message and no changes are made to the local commodities table.

---

### User Story 5 - Prevent Concurrent Imports (Priority: P2)

Only one commodity import may run at a time. If an admin triggers an import while another is already in progress, the second request is rejected.

**Why this priority**: Concurrent imports against the same table would produce undefined upsert and soft-delete behavior.

**Independent Test**: Can be tested by triggering two imports in rapid succession and verifying that the second receives a rejection response while the first completes normally.

**Acceptance Scenarios**:

1. **Given** a commodity import is already in progress, **When** an admin triggers another import, **Then** the second request is rejected with a message indicating an import is already running.

---

### Edge Cases

- What happens when the source returns an empty commodity list? The import completes with no changes applied and returns a warning. No upserts are performed and no existing commodities are soft-deleted. FR-006 applies only when a non-empty source import is received; an empty feed is treated as a likely transient API error, not an authoritative signal to remove all commodities.
- What happens when a commodity has `id_parent` or `id_item` that does not reference any local record? The relationship identifiers are stored as-is; no integrity check or error is raised.
- What happens when `ids_star_systems` or similar fields contain whitespace around commas (e.g., `"1, 4, 7"`)? Whitespace is trimmed when parsing to produce clean integer arrays.
- What happens when a timestamp field (`date_added`, `date_modified`) contains an invalid or zero value? The raw value is stored; UTC conversion is attempted and stored as null if the value is not a valid Unix timestamp.
- What happens when the same commodity import is triggered by a non-admin user? The request is rejected; the endpoint is inaccessible to non-admin roles.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide an admin-only endpoint to trigger a full commodity catalog import.
- **FR-002**: The system MUST fetch commodity data from the UEX commodity catalog source during an import.
- **FR-003**: The system MUST fail the entire import if the source endpoint is unreachable or the response does not conform to the expected structure.
- **FR-004**: The system MUST upsert each valid source record into the local commodities store, keyed on the UEX commodity `id` field.
- **FR-005**: The system MUST skip any individual source record that is missing a required `id` or `name` field and continue processing remaining records. A record where the `id` field is absent, null, or zero is treated as missing and is skipped.
- **FR-006**: The system MUST soft-delete any locally stored commodity that is absent from the current full source import.
- **FR-007**: The system MUST restore (un-delete) a soft-deleted commodity if it reappears in a subsequent source import.
- **FR-008**: The system MUST store the complete raw source object for each commodity record alongside the normalized fields.
- **FR-009**: The system MUST convert all UEX integer flag fields into boolean values when storing commodity records locally.
- **FR-010**: The system MUST store the commodity UUID when present; records with a null UUID MUST be imported without error.
- **FR-011**: The system MUST store `id_parent` and `id_item` relationship identifiers as provided, without enforcing that the referenced records exist.
- **FR-012**: The system MUST store both the raw comma-separated string and the parsed integer array for each location identifier field (`ids_star_systems`, `ids_planets`, `ids_moons`, `ids_poi`, `ids_orbits`).
- **FR-013**: The system MUST store both the raw Unix integer value and the converted UTC datetime for `date_added` and `date_modified`.
- **FR-014**: The system MUST NOT import pricing fields (`price_buy`, `price_sell`) even if they appear in the source payload.
- **FR-015**: The system MUST import all records regardless of their flag values (visibility, availability, temporary, buggy, illegal, etc.).
- **FR-016**: The system MUST reject concurrent import requests, allowing only one commodity import to run at a time.
- **FR-017**: The system MUST return a summary upon import completion including counts of records fetched, inserted, updated, restored, soft-deleted, and skipped.
- **FR-018**: Only users with the admin role MUST be permitted to trigger or view the commodity import endpoint.

### Key Entities

- **Commodity**: A single tradeable or harvestable commodity from the Star Citizen universe. Identified durably by its UEX source `id`. Carries catalog attributes (name, code, slug, kind, weight), boolean classification flags, location availability identifiers, relationship references (parent commodity, associated item), and timestamps. Stores the full raw source payload for audit and forward-compatibility.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A full commodity catalog import (expected hundreds of records) completes without error in under 60 seconds under normal network conditions.
- **SC-002**: All valid records from the source are present in the local commodities store after a successful import, with no valid record omitted.
- **SC-003**: All integer flag fields from the source are stored as boolean values; no raw integer flag value is exposed in the local commodity data.
- **SC-004**: Records with null UUIDs are present in the local store after import; zero valid records are rejected solely because UUID is absent.
- **SC-005**: After a second import that omits previously seen commodities, those commodities are marked as soft-deleted and are not lost from the local store.
- **SC-006**: Triggering a second concurrent import while one is in progress results in a rejection response 100% of the time.
- **SC-007**: An import triggered when the source is unreachable leaves the local commodities table entirely unchanged.

## Assumptions

- The UEX commodity source endpoint returns a JSON array of commodity objects under a standard response envelope consistent with the existing UEX API patterns already used in this project (ships, items).
- The commodity `id` field is a stable, durable identifier that does not change between source refreshes.
- The admin import UI follows the same interaction pattern as the existing ship and item import controls on the data import admin page; no new UI patterns are required.
- No import history, progress bar, or real-time status streaming is required for v1; a synchronous response with a completion summary is sufficient.
- The parsed integer arrays for location identifier fields contain only valid integers; malformed tokens (non-numeric) are discarded silently during parsing.
- UTC datetime conversion uses Unix epoch (seconds since 1970-01-01T00:00:00Z) as the interpretation of the source integer timestamp fields.
- Pricing fields (`price_buy`, `price_sell`) may or may not be present in the source payload; they are excluded from storage in either case.
- Relationship integrity for `id_parent` and `id_item` is deferred to a future feature; no foreign key constraints are applied for v1.
- Location join tables are deferred to a future feature; only raw strings and parsed arrays are stored for v1.
