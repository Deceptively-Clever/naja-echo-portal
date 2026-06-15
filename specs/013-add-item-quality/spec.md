# Spec: Add item quality

Short name: add-item-quality

Summary

As a quartermaster, when adding items to the warehouse, the user must be able to specify an item quality as an integer between 1 and 1000. If not provided, the system defaults the quality to 500.

Background and Motivation

Quartermasters need a way to record the relative quality or condition of items on intake to support inventory decisions, prioritization for distribution, and reporting. A numeric quality field (1-1000) provides a fine-grained, sortable metric that supports downstream workflows.

Actors

- Quartermaster (primary): adds and manages warehouse items
- Warehouse system: stores item records

User Scenarios

1) Create item with explicit quality
   - Given a quartermaster adding a new item
   - When they enter a quality value of 750
   - Then the created item record stores quality = 750

2) Create item without quality
   - Given a quartermaster adding a new item and leaving quality unspecified
   - When the item is saved
   - Then the created item record stores quality = 500 (default)

3) Validation on out-of-range input
   - Given a quartermaster enters quality = 0 or 1001
   - When they attempt to save
   - Then the system rejects the input and shows a validation error explaining allowed range 1–1000

Functional Requirements (testable)

FR-1: Quality field presence
- The item creation form accepts an integer field named "quality".
- If omitted, default value 500 is stored.

FR-2: Value range and validation
- The quality value must be an integer between 1 and 1000 inclusive.
- Non-integer or out-of-range values are rejected with a clear validation message.

FR-3: Persistence
- The quality value persists on the item record and is returned in item read/list responses.

FR-4: Backwards compatibility
- Existing items without a quality set should be treated as having quality 500 when read by the system (for reporting and sorting), unless and until updated.

FR-5: Sorting/Filtering (informational)
- Item lists should be able to sort or filter by quality. (This is a dependent capability; verify storage and API surfaces allow this.)

Acceptance Criteria / Success Criteria

- SC-1: 100% of new items created without an explicit quality store quality = 500.
- SC-2: 100% of attempts to save quality <1 or >1000 are rejected and return a user-visible validation error.
- SC-3: At least one create and one read operation confirm the quality value persists and is retrievable.
- SC-4: Documentation for the item creation flow references the quality field and default value.

Key Entities

- Item
  - quality: integer (1..1000), default 500
  - existing item attributes remain unchanged

Assumptions

- Quality is an integer; fractional quality values are not supported.
- Default of 500 is acceptable to business stakeholders as the midpoint value.
- No additional business logic (e.g., quality tiers mapping to statuses) is required as part of this change.

Dependencies

- UI/UX changes to item creation/edit screens to expose the quality field.
- API/data model changes to store and return the quality field.

Testing / Acceptance Scenarios (executable)

- Test 1: Create item with quality = 1, expect stored value 1.
- Test 2: Create item with quality = 1000, expect stored value 1000.
- Test 3: Create item without quality, expect quality = 500.
- Test 4: Attempt to create item with quality = 0, expect validation error and no record created.
- Test 5: Read an existing item that lacks quality; system treats it as quality = 500 for reporting.

Notes

- No security or privacy impacts identified for adding a numeric quality field.
- If stakeholders later require named tiers (e.g., 'Poor', 'Fair', 'Good'), map tiers to numeric ranges in a follow-up feature.
