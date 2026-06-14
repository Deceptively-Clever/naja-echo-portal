# Feature Specification: Item Data Import

**Feature Branch**: `009-item-data-import`

**Created**: 2026-06-13

**Status**: Draft

**Input**: Add item imports to the existing admin Data Import page, including a new Items tab with category management and item import by category or all categories.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Category Refresh (Priority: P1)

An admin opens the Data Import page and navigates to the Items tab. They see that no categories have been loaded yet, and item import actions are disabled with a message explaining that categories must be refreshed first. The admin triggers a category refresh, waits for it to complete, and sees a result summary showing how many categories were fetched, inserted, updated, unchanged, and failed. The last-refreshed timestamp is now displayed.

**Why this priority**: Without categories, no item imports are possible. Category refresh is the entry point for the entire Items workflow and must work before anything else can be tested.

**Independent Test**: Can be fully tested by navigating to the Items tab and clicking "Refresh Categories" — delivers the complete category management workflow independently of any item import.

**Acceptance Scenarios**:

1. **Given** an admin is on the Data Import page, **When** they open the Items tab, **Then** they see a "Refresh Categories" action and, if no categories are stored locally, item import actions are disabled with a clear explanation.
2. **Given** an admin triggers a category refresh, **When** the refresh is in progress, **Then** all import and refresh actions are disabled and a loading state is shown.
3. **Given** an admin triggers a category refresh, **When** the UEX categories endpoint responds successfully, **Then** categories are stored locally and a result summary is displayed showing categories fetched, inserted, updated, unchanged, and failed.
4. **Given** categories have been refreshed at least once, **When** an admin views the Items tab, **Then** the last-refreshed timestamp is displayed.
5. **Given** an admin triggers a category refresh, **When** the category refresh fails, **Then** an error is shown and no partial category data is stored.

---

### User Story 2 - Import Items for a Single Category (Priority: P2)

An admin has previously refreshed categories. They want to import items for a specific category. They use the category selector — which supports search, section filter, mining-related filter, and game-related filter — to find and select a category. The selector displays enough context per category (section, name, type, game-related flag, mining-related flag, source modified date, and local import state) to make an informed choice. The admin triggers the import, waits for it to complete, and sees a result summary with counts of items fetched, inserted, updated, skipped, soft-deleted, and failed.

**Why this priority**: The single-category import is the most targeted and repeatable admin action for maintaining item data accuracy.

**Independent Test**: Can be fully tested end-to-end by selecting one category and importing it, verifying the result summary and that imported items are available for normal use.

**Acceptance Scenarios**:

1. **Given** local item categories exist, **When** an admin views the category selector, **Then** they see category name, section, type, game-related flag, mining-related flag, source modified date, and local import state for each category.
2. **Given** an admin uses the search filter, **When** they type a search term, **Then** only categories whose names match the search term are shown.
3. **Given** an admin uses the section filter, **When** they select a section, **Then** only categories in that section are shown.
4. **Given** an admin uses the mining-related or game-related filters, **When** they activate those filters, **Then** only matching categories are shown.
5. **Given** an admin selects a category and triggers a single-category import, **When** the import completes successfully, **Then** a result summary is displayed with items fetched, inserted, updated, skipped (unchanged/no UUID), soft-deleted, and failed counts.
6. **Given** an item record with a UUID already stored locally, **When** a category import runs, **Then** the local record is updated with the latest source data.
7. **Given** an item record with a UUID not yet stored locally, **When** a category import runs, **Then** a new record is inserted.
8. **Given** an item record with `uuid = null`, **When** a category import runs, **Then** the record is skipped and counted in the UUID-null skipped total; the category import does not fail.
9. **Given** a previously imported item is absent from the current import for the same category, **When** the import completes, **Then** that item is soft-deleted and counted in the soft-deleted total.
10. **Given** a previously soft-deleted item appears again in a category import, **When** the import completes, **Then** the item is restored and updated with the latest source data.
11. **Given** non-soft-deleted items have been successfully imported, **When** normal application use occurs, **Then** those items are available; soft-deleted items are hidden.

---

### User Story 3 - Import Items for All Eligible Categories (Priority: P3)

An admin wants to bulk-refresh item data across all locally stored item categories. They trigger an "Import All" action. The system processes each eligible category (those with `type = "item"`), recording failures per category without stopping the overall run. When complete, the admin sees a result summary aggregated across all categories, including which specific categories failed, so they can manually retry those later.

**Why this priority**: All-category import builds on single-category import and is a convenience feature for bulk refresh.

**Independent Test**: Can be fully tested with at least two local item categories by triggering "Import All" and verifying partial-failure handling and aggregated results.

**Acceptance Scenarios**:

1. **Given** local item categories with `type = "item"` exist, **When** an admin triggers "Import All," **Then** all eligible categories are processed sequentially.
2. **Given** one category fails during an all-category import, **When** the import continues, **Then** remaining categories are still processed and the failed category is recorded.
3. **Given** an all-category import completes with one or more failures, **When** the admin sees the result, **Then** the result summary lists which categories failed so the admin can retry them individually.
4. **Given** an all-category import completes, **When** the admin sees the result, **Then** the summary includes categories processed, categories succeeded, categories failed, and all item-level counts (fetched, inserted, updated, skipped, soft-deleted, failed, UUID-null skipped).

---

### User Story 4 - Access Control (Priority: P1)

The Items tab and all associated actions are admin-only, using the same authorization behavior as the existing ship import on the Data Import page.

**Why this priority**: Authorization is non-negotiable and should be verified before any other story is considered complete.

**Independent Test**: Can be tested by logging in as a non-admin and attempting to access the Items tab or invoke item import actions.

**Acceptance Scenarios**:

1. **Given** an authenticated admin, **When** they open the Data Import page, **Then** the Items tab is accessible.
2. **Given** a non-admin authenticated user, **When** they attempt to access item import actions, **Then** they are denied using the same authorization behavior as ship import.

---

### User Story 5 - Concurrency Guard (Priority: P2)

Only one import or category refresh can run at a time. This behavior matches the existing ship import.

**Why this priority**: Prevents data corruption from concurrent writes to the same category data.

**Independent Test**: Can be tested by triggering an import and immediately attempting a second action.

**Acceptance Scenarios**:

1. **Given** a category refresh or item import is already in progress, **When** an admin attempts to start another import or refresh, **Then** all such actions are disabled until the current operation completes.

---

### Edge Cases

- What happens when a category import returns an invalid or unexpected API response shape? The category is treated as failed; the error is recorded and the all-category import continues with remaining categories. A single-category import shows the failure directly.
- What happens when all items in a category have `uuid = null`? All items are skipped; the import still completes successfully with a skipped-UUID count equal to the total fetched.
- What happens when a category with no previous imports has items absent from the response? There is nothing to soft-delete; no soft-delete records are created.
- What happens when the category selector has no results matching the active filters? The selector shows an empty state indicating no categories match.
- What happens when the last-refreshed timestamp is not known (first session after categories were imported in an earlier version without tracking)? The UI omits the timestamp or shows it as unknown rather than showing an incorrect value.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Items tab MUST be accessible from the existing Data Import page to authorized admins only, using the same authorization behavior as ship import.
- **FR-002**: The Items tab MUST provide a "Refresh Categories" action that calls the UEX categories endpoint and stores the results locally.
- **FR-003**: Category refresh MUST be atomic with respect to failure: if the refresh fails, no partial category data may be stored.
- **FR-004**: The UI MUST display the date and time categories were last refreshed when that information is available.
- **FR-005**: When no local categories exist, item import actions MUST be disabled and the UI MUST display an explanation directing the admin to refresh categories first.
- **FR-006**: The Items tab MUST provide a category selector that displays, for each category: section, name, type, game-related flag, mining-related flag, source modified date, and local import state.
- **FR-007**: The category selector MUST support filtering by: text search (name), section, mining-related flag, and game-related flag.
- **FR-008**: Only categories where `type = "item"` are eligible for item import.
- **FR-009**: The Items tab MUST provide an action to import items for a single selected category.
- **FR-010**: The Items tab MUST provide an action to import items for all locally stored eligible item categories.
- **FR-011**: Item identity MUST be based on the `uuid` field. Items with `uuid = null` MUST be skipped and counted; they MUST NOT cause the category import to fail.
- **FR-012**: Existing items (matched by UUID) MUST be updated with the latest source data on import.
- **FR-013**: New items (UUID not yet stored locally) MUST be inserted on import.
- **FR-014**: Items that were previously imported for a category but are absent from a subsequent import of the same category MUST be soft-deleted. Soft-delete scope is limited to the category being imported.
- **FR-015**: A soft-deleted item that reappears in a subsequent category import MUST be restored and updated with the latest source data.
- **FR-016**: Soft-deleted items MUST be hidden from normal application use but preserved for existing references and troubleshooting.
- **FR-017**: When importing all categories, a failure in one category MUST NOT prevent processing of remaining categories. Each category failure MUST be individually recorded.
- **FR-018**: Only one import or category refresh action may run at a time; all other actions MUST be disabled while an operation is in progress.
- **FR-019**: After a category refresh completes, a result summary MUST be displayed including: categories fetched, inserted, updated, unchanged/skipped, failed, start time, end time, and duration.
- **FR-020**: After an item import completes (single or all-category), a result summary MUST be displayed including: categories processed, categories succeeded, categories failed, items fetched, inserted, updated, unchanged/skipped, skipped due to null UUID, soft-deleted, failed, start time, end time, duration, and error details by category when applicable.
- **FR-021**: The item `attributes` field MUST NOT be imported or stored (deprecated field).
- **FR-022**: The item `screenshot` field MUST NOT be imported or stored in v1.

### Key Entities

- **Category**: A game data classification from UEX. Attributes: `id` (source), `type`, `section`, `name`, `is_game_related`, `is_mining`, `date_added`, `date_modified`, plus a locally tracked import timestamp. Categories with `type = "item"` are eligible for item import.
- **Item**: A Star Citizen game item from UEX. Attributes: `id` (source route ID), `id_parent`, `id_category`, `id_company`, `id_vehicle`, `name`, `section`, `category`, `company_name`, `vehicle_name`, `slug`, `size`, `uuid` (stable identity), `color`, `color2`, `url_store`, `wiki`, `quality`, `is_exclusive_pledge`, `is_exclusive_subscriber`, `is_exclusive_concierge`, `is_commodity`, `is_harvestable`, `notification`, `game_version`, `date_added`, `date_modified`, plus a local soft-delete flag. Items belong to the `sc` schema.
- **Import Result**: A transient result record describing the outcome of a single import or category refresh operation, including counts and timing. Not persisted in v1.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An admin can complete a full category refresh and see a result summary within a single page interaction with no page reload.
- **SC-002**: An admin can select a category and import its items, receiving a complete result summary, within a single page interaction.
- **SC-003**: An admin can trigger an all-category import and receive a complete result summary with per-category failure details if any failures occurred.
- **SC-004**: When a category import is re-run, items absent from the response are soft-deleted; items that reappear after soft-delete are restored — all verified by the result counts.
- **SC-005**: Item records with `uuid = null` never cause an import to fail; they appear in the result as skipped-UUID counts.
- **SC-006**: Non-admin users cannot invoke any item import or category refresh action; the denial matches the behavior already in place for ship import.
- **SC-007**: Only one import or category refresh operation can be active at any moment; the UI prevents a second concurrent operation.

## Assumptions

- The existing Data Import page already enforces admin-only access via a pattern that the Items tab will reuse; the specific authorization mechanism is not reimplemented, only extended.
- "Local categories" refers to categories previously fetched from UEX and stored in the application's own data store in the `sc` schema.
- The UEX `id` on items is stored for source-side routing purposes but is not the stable application identity; `uuid` is the stable identity.
- "Unchanged" items (those imported where no fields changed) may be counted as part of the "updated" count or as a separate "unchanged/skipped" count depending on implementation convenience; the spec treats both as acceptable provided the summary is accurate.
- The last-refreshed timestamp for categories is tracked at the application level; if that tracking did not exist before this feature, it will be introduced as part of this feature.
- Import result summaries are displayed in the UI for the current session only; they are not persisted between sessions (import history is out of scope for v1).
- No item categories other than those with `type = "item"` will be processed by the item import actions; service, contract, and other category types are out of scope for v1.
- The category selector is expected to handle a moderately large list (potentially hundreds of categories) with filtering; pagination is not required for v1 if client-side filtering is sufficient.
