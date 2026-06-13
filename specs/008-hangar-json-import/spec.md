# Feature Specification: Hangar JSON Import

**Feature Branch**: `008-hangar-json-import`

**Created**: 2026-06-13

**Status**: Draft

**Input**: User description: "Add an import button to the 'My Hangar' page allowing the user to upload a JSON file with a list of owned ships exported from the HangarXPLOR plugin. Ships are matched by name to the internal ship catalogue. Importing replaces all existing hangar entries for the user. A warning is displayed before import proceeds."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Import Ship List from HangarXPLOR Export (Priority: P1)

An authenticated user on the "My Hangar" page wants to bulk-populate their hangar by uploading a JSON file exported from the HangarXPLOR browser plugin. They click an "Import" button, are warned that the import will replace all existing ships in their hangar, confirm the action, select their JSON file, and see their hangar updated with the imported ships.

**Why this priority**: This is the core feature. Without it, nothing else in this feature delivers value.

**Independent Test**: Can be fully tested by uploading a valid HangarXPLOR JSON export and verifying the hangar is replaced with the imported ships.

**Acceptance Scenarios**:

1. **Given** a user is on the My Hangar page with existing ships, **When** they click "Import", **Then** a confirmation dialog appears warning them that all existing hangar ships will be replaced.
2. **Given** the warning dialog is shown, **When** the user confirms, **Then** they are prompted to select a JSON file.
3. **Given** a valid HangarXPLOR JSON file is selected, **When** the file is submitted, **Then** all existing hangar entries for that user are removed and replaced with matched ships from the file.
4. **Given** the import completes successfully, **When** the user views My Hangar, **Then** the hangar displays only the ships from the imported file that were matched to the internal ship catalogue.
5. **Given** the warning dialog is shown, **When** the user cancels, **Then** no changes are made and the hangar remains as-is.

---

### User Story 2 - Partial Match with Unrecognized Ships (Priority: P2)

A user imports a JSON file that contains some ships not recognized in the internal catalogue (these have an `unidentified` field in the export). The import proceeds for all recognized ships, and the user is informed of how many ships were unrecognized and skipped.

**Why this priority**: Real-world exports often include ships not yet in the catalogue. Users need to know when ships were skipped so they can manually add them if needed.

**Independent Test**: Upload a JSON file containing at least one entry with the `unidentified` field and verify the recognized ships are imported while a skip count is reported.

**Acceptance Scenarios**:

1. **Given** a JSON file containing a mix of recognized and unrecognized ships, **When** the user imports it, **Then** only recognized ships (those matchable to the catalogue) are added to the hangar.
2. **Given** an import with unrecognized ships, **When** the import completes, **Then** the user sees a summary indicating how many ships were imported and how many were skipped because they could not be matched.
3. **Given** a JSON file where ALL ships are unrecognized, **When** the import completes, **Then** the user's hangar is cleared (per the replace-all behavior) and a message informs them that no ships could be matched.

---

### User Story 3 - Invalid File Handling (Priority: P3)

A user accidentally selects a file that is not valid JSON or does not match the expected format. The system rejects the file with a clear error message without modifying the hangar.

**Why this priority**: Error handling prevents data corruption and maintains user confidence.

**Independent Test**: Upload a non-JSON file or a JSON file with invalid structure and verify the hangar is unchanged and an error message is displayed.

**Acceptance Scenarios**:

1. **Given** a user selects a file that is not valid JSON, **When** the file is submitted, **Then** the hangar is unchanged and a clear error message is displayed indicating the file is invalid.
2. **Given** a user selects a JSON file that does not match the expected array format, **When** the file is submitted, **Then** the hangar is unchanged and an error message is displayed.

---

### Edge Cases

- What happens when the JSON file is empty (empty array `[]`)? The hangar should be cleared with a confirmation that 0 ships were imported.
- What happens when the same ship appears multiple times in the import file? Multiple import records resolving to the same catalog ship collapse into a single hangar entry (the `ux_hangar_entries_user_ship` unique constraint enforces this).
- What happens when `ship_name` is absent but `name` is present in an import record? The `name` field is used as the fallback for catalogue matching.
- What happens if the file is too large? The system rejects files exceeding a reasonable size limit with a clear error.
- What happens if the user's session expires mid-import? The import fails gracefully and the user is prompted to log in again; no partial state is committed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The My Hangar page MUST display an "Import" button accessible to authenticated users.
- **FR-002**: Clicking "Import" MUST present the user with a warning that all existing hangar ships will be permanently replaced.
- **FR-003**: The user MUST be able to cancel the import from the warning screen without any changes being made to their hangar.
- **FR-004**: After confirming the warning, the system MUST allow the user to select a JSON file from their device.
- **FR-005**: The system MUST accept JSON files in the HangarXPLOR array format, where each element may contain fields including `name`, `ship_name`, `ship_code`, `manufacturer_name`, `manufacturer_code`, `lti`, `warbond`, `entity_type`, `pledge_id`, `pledge_name`, `pledge_date`, `pledge_cost`, and an optional `unidentified` field.
- **FR-006**: The system MUST match each imported ship to the internal ship catalogue using the `ship_name` field; when `ship_name` is absent, the `name` field MUST be used as the fallback.
- **FR-007**: Ships that include the `unidentified` field OR cannot be matched to any entry in the internal ship catalogue MUST be skipped.
- **FR-008**: On a successful import, all existing hangar entries for the authenticated user MUST be removed before inserting the matched ships.
- **FR-009**: Each matched ship from the import results in one hangar entry. Multiple import records resolving to the same catalog ship collapse into a single hangar entry (de-duplicated by ship ID before insert, enforced by the `ux_hangar_entries_user_ship` unique constraint).
- **FR-010**: After import completes, the system MUST display a summary to the user indicating how many ships were imported and how many were skipped (unmatched).
- **FR-011**: If the selected file is not valid JSON or does not conform to the expected array format, the system MUST reject the file, display a clear error message, and leave the hangar unchanged.
- **FR-012**: The import operation MUST be atomic: either all removals and all insertions succeed together, or none are committed.

### Key Entities

- **Hangar Entry**: Represents a single ship owned by a user in their hangar. On import, all existing entries for the user are replaced.
- **Ship**: An entry in the internal ship catalogue, identified by its `name`. Used as the matching target for imported ship records.
- **Import Record**: A single object from the uploaded JSON array. Contains at minimum a `name` field and optionally a `ship_name` field used for catalogue matching.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete a full hangar import (select file, confirm warning, upload) in under 60 seconds.
- **SC-002**: 100% of ships that exist in the internal catalogue and appear in the import file by name are correctly imported, with no omissions or duplications beyond what the file specifies.
- **SC-003**: Users see a clear import summary (count of imported and skipped ships) immediately after every import attempt, whether successful or partially successful.
- **SC-004**: Invalid or malformed files are rejected without modifying the hangar, 100% of the time.
- **SC-005**: The import warning is seen by 100% of users before any destructive action is taken — there is no way to trigger the file selection without first acknowledging the warning.

## Assumptions

- All users of this feature are authenticated. Unauthenticated users cannot access My Hangar or the import function.
- The internal ship catalogue (`ships` table) is already populated with ship names that are compatible with HangarXPLOR export names. Name matching is case-insensitive.
- The `entity_type` field in the import file is not used for filtering — all records are treated as ships regardless of this field value. Records with `entity_type` other than `"ship"` are still matched against the catalogue by name.
- File size is limited to a reasonable maximum (assumed 5 MB) to prevent abuse; this limit may be adjusted during implementation.
- The import replaces the entire hangar for the authenticated user, not a subset. There is no merge/append mode in this feature.
- The HangarXPLOR export format may include records without a `ship_name` field (notably ships flagged as `unidentified`). These are matched using `name` or skipped if neither field produces a catalogue match.
- This feature does not require changes to the ship catalogue itself — it is read-only during import.
