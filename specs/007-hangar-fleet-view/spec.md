# Feature Specification: Hangar

**Feature Branch**: `007-hangar-fleet-view`

**Created**: 2026-06-13

**Status**: Draft

**Input**: User description: "Hangar feature allowing members to view ships owned by themselves and by other members of the organization"

## Clarifications

### Session 2026-06-13

- Q: Can members remove ships from their hangar in this version? → A: Yes, members can remove ships from My Hangar.
- Q: Should the member filter in Org Hangar list all org members or only members who own at least one ship? → A: Only members who own at least one ship.
- Q: After successfully adding a ship in the Add Ship dialog, does the dialog stay open or close automatically? → A: Dialog stays open so the member can add more ships; the member closes it manually.
- Q: When a ship image URL exists but the image fails to load at runtime, how should the card behave? → A: Fall back to the default card background silently; no error indicator on the card.
- Q: How is the remove action surfaced on a My Hangar card? → A: Remove action is revealed on hover as an icon/button overlaid on the card.
- Clarification: Org Hangar is not a separate data store. It is a derived, aggregated view of all individual member hangar entries. There are no "org ships" tracked independently — every ship in Org Hangar originates from one or more members' personal hangars.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse My Hangar (Priority: P1)

A member navigates to Hangar from the main navigation and sees all the ships they personally own displayed in a visual card gallery. Each card shows the ship's name and, when available, the ship's image as the card background. The member can search their ships by name to quickly find a specific model. If their hangar is empty, they see a prompt encouraging them to add their first ship.

**Why this priority**: Seeing one's own hangar is the entry point for the entire feature and the most personally relevant view. Without this, nothing else in the feature makes sense.

**Independent Test**: Can be fully tested by navigating to Hangar, confirming My Hangar loads by default, verifying own ships appear as cards with correct names and backgrounds, and confirming the search bar filters results correctly.

**Acceptance Scenarios**:

1. **Given** I am authenticated, **When** I click Hangar in the main navigation, **Then** I am taken to My Hangar.
2. **Given** I am on My Hangar, **When** the page loads, **Then** I see only ships I own displayed as visual cards.
3. **Given** I am on My Hangar with ships, **When** each card renders, **Then** the ship name appears in the top-left corner.
4. **Given** a ship in my hangar has an image, **When** its card renders, **Then** the ship image is used as the card background.
5. **Given** a ship in my hangar has no image, **When** its card renders, **Then** the card uses the default card background.
6. **Given** I am on My Hangar, **When** I type in the search bar, **Then** only my ships whose names contain the search text (case-insensitive) are shown.
7. **Given** my search returns no results, **When** the gallery renders, **Then** I see a message explaining no ships match the search.
8. **Given** my hangar is empty, **When** the page loads, **Then** I see an empty state encouraging me to add my first ship.
9. **Given** I am on My Hangar, **When** I view the cards, **Then** no owner count or owner information is shown.

---

### User Story 2 - Add and Remove Ships in My Hangar (Priority: P2)

A member wants to register a ship they own in the game. From My Hangar, they click Add Ship, search the ship database for their ship model, and add it to their hangar. The system prevents adding the same ship twice and clearly marks already-owned ships in the dialog. After a successful add, the ship immediately appears in My Hangar. The member can also remove a ship they no longer own; after removal, it disappears from My Hangar and from Org Hangar if they were the sole owner.

**Why this priority**: Members need a way to populate and maintain their hangar. The Hangar feature delivers no value until ships can be added, and a member who sells a ship in-game must be able to correct their hangar record.

**Independent Test**: Can be fully tested by opening the Add Ship dialog, adding a ship, confirming it appears in My Hangar, then removing it and confirming it disappears. Duplicate prevention can be tested by attempting to add the same ship a second time.

**Acceptance Scenarios**:

1. **Given** I am on My Hangar, **When** the page loads, **Then** I see an Add Ship button.
2. **Given** I click Add Ship, **When** the dialog opens, **Then** I can search the ship database by name.
3. **Given** I search in the Add Ship dialog, **When** matching ships exist, **Then** matching ships are shown in the results.
4. **Given** a ship is not in my hangar, **When** I select it in the dialog and confirm, **Then** it is added to my hangar and appears in My Hangar.
5. **Given** a ship is already in my hangar, **When** it appears in the Add Ship dialog search results, **Then** it is marked as already owned and cannot be added again.
6. **Given** I successfully add a ship, **When** the operation completes, **Then** I receive a clear success message and the dialog remains open so I can add another ship.
7. **Given** adding a ship fails, **When** the operation completes, **Then** I receive a clear error message and the dialog remains open.
8. **Given** I am on My Hangar, **When** I choose to remove a ship, **Then** I am asked to confirm the removal before it is deleted.
9. **Given** I confirm removal of a ship, **When** the operation completes, **Then** the ship no longer appears in My Hangar.
10. **Given** I was the only member who owned a ship and I remove it, **When** the operation completes, **Then** that ship no longer appears in Org Hangar.
11. **Given** other members also own the same ship model and I remove mine, **When** the operation completes, **Then** Org Hangar continues to show that ship with the updated owner count.
12. **Given** removing a ship fails, **When** the operation completes, **Then** I receive a clear error message and the ship remains in my hangar.

---

### User Story 3 - Browse Org Hangar (Priority: P3)

A member switches to Org Hangar to see what ships are collectively owned across the organization. Ships are grouped by unique model — if three members own the same ship, it appears once with an owner count. Each card shows how many members own that ship, and hovering over the count reveals the list of owners. The member can search by ship name and filter to see only their own ships or ships owned by a specific member.

**Why this priority**: Org Hangar builds on My Hangar data and provides collective fleet visibility, but is not required for the core personal hangar experience.

**Independent Test**: Can be fully tested by switching to Org Hangar, confirming org ships appear grouped by model with owner counts, hovering to see owner lists, and exercising the search, My Ships filter, and member filter.

**Acceptance Scenarios**:

1. **Given** I am in Hangar, **When** I click Org Hangar in the sub-navigation, **Then** I see ships owned by organization members.
2. **Given** multiple members own the same ship model, **When** Org Hangar renders, **Then** that ship appears once with an owner count in the bottom-right corner of the card.
3. **Given** I hover over the owner count on a card, **When** the tooltip appears, **Then** I see the names of all members who own that ship.
4. **Given** I am on Org Hangar, **When** I type in the search bar, **Then** only ship cards whose names match the search (case-insensitive, partial) are shown.
5. **Given** I enable the My Ships filter, **When** the gallery updates, **Then** only ships I own are shown.
6. **Given** I select a specific member in the member filter, **When** the gallery updates, **Then** only ships owned by that member are shown.
7. **Given** I select All Members in the member filter, **When** the gallery updates, **Then** the member-specific filter is cleared and all org ships are shown.
8. **Given** I enable My Ships and then select a different member, **When** the gallery updates, **Then** My Ships is turned off and only the selected member's ships are shown.
9. **Given** a member filter returns no results, **When** the gallery renders, **Then** I see a message explaining no ships were found for that member.
10. **Given** no members have added any ships to their hangars, **When** I load Org Hangar, **Then** I see an empty state explaining that no members have added ships yet.
11. **Given** I am on Org Hangar, **When** I view the page, **Then** there is no Add Ship button.

---

### User Story 4 - Infinite Scroll Through Ship Gallery (Priority: P4)

A member with a large hangar or viewing a large org fleet scrolls through the card gallery without encountering pagination controls. As they scroll toward the bottom, more ship cards load automatically and appear in place.

**Why this priority**: Scrolling behavior is a quality-of-life concern. The gallery functions without it; pagination could substitute in a degraded experience.

**Independent Test**: Can be fully tested by scrolling through a gallery with more ships than the initial page size and confirming more cards appear without clicking pagination controls.

**Acceptance Scenarios**:

1. **Given** I am on My Hangar or Org Hangar, **When** I scroll to the bottom of the visible cards, **Then** more matching ship cards load and appear without any pagination controls visible.
2. **Given** I change the search text or active filters, **When** the gallery resets, **Then** results start from the beginning of the filtered set.

---

### Edge Cases

- What happens when a ship is added and then immediately appears in Org Hangar (including the current user as an owner)?
- What happens when the last member who owned a ship removes it — does Org Hangar update immediately?
- What if a removal request fails mid-flight due to a network error — is the member's view consistent with the server state?
- How does the Add Ship dialog behave when the ship database returns no results for a search term?
- How does the member filter list populate if some members have no ships?
- How does Org Hangar respond when a search combined with an active member filter returns no results?
- What happens when search text changes while infinite scroll is mid-load?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The main navigation MUST include a "Hangar" item accessible to all authenticated members.
- **FR-002**: Hangar MUST have two sub-navigation views: My Hangar and Org Hangar.
- **FR-003**: Navigating to Hangar without a specific sub-view selected MUST default to My Hangar.
- **FR-004**: My Hangar MUST display only the ships owned by the currently authenticated member.
- **FR-005**: Org Hangar MUST display a derived, aggregated view of all member hangar entries, grouped by unique ship model. No separate org-level ship tracking exists; every entry originates from an individual member's hangar.
- **FR-006**: Both views MUST display ships as a visual card gallery (not a table or list).
- **FR-007**: Each ship card MUST display the ship name in the top-left corner.
- **FR-008**: Each ship card MUST use the ship's image as the card background when an image is available.
- **FR-009**: Each ship card MUST use the default card background when no ship image is stored or when a stored image URL fails to load at runtime; no error indicator is shown on the card in either case.
- **FR-010**: Ship cards MUST remain readable regardless of whether a background image is present.
- **FR-011**: My Hangar cards MUST NOT display owner counts or owner hover information.
- **FR-012**: Org Hangar cards MUST display the number of unique members who own that ship in the bottom-right corner, accompanied by a person icon.
- **FR-013**: Hovering over the owner count on an Org Hangar card MUST display a list of the members who own that ship.
- **FR-014**: Both views MUST include a search bar that filters ships by name (case-insensitive, partial match).
- **FR-015**: When a search returns no results, both views MUST display an appropriate empty state message.
- **FR-016**: My Hangar MUST include an Add Ship button.
- **FR-017**: The Add Ship button MUST open a dialog allowing the member to search the ship database by name and add a ship to their hangar.
- **FR-018**: A member MUST NOT be able to add the same ship more than once.
- **FR-019**: Ships already in the member's hangar MUST be visually marked in the Add Ship dialog and cannot be selected for addition.
- **FR-020**: After successfully adding a ship, it MUST immediately appear in My Hangar and in Org Hangar with the current member listed as an owner.
- **FR-021**: The system MUST provide clear success feedback when a ship is added successfully; the Add Ship dialog MUST remain open so the member can add additional ships.
- **FR-022**: The system MUST provide clear error feedback when adding a ship fails; the Add Ship dialog MUST remain open.
- **FR-033**: My Hangar MUST allow a member to remove a ship from their hangar. The remove action MUST be revealed on hover as an icon or button overlaid on the ship card; it MUST NOT be persistently visible.
- **FR-034**: Before removing a ship, the system MUST ask the member to confirm the removal.
- **FR-035**: After successfully removing a ship, it MUST no longer appear in My Hangar.
- **FR-036**: After removing a ship, if the member was the only owner, the ship MUST no longer appear in Org Hangar.
- **FR-037**: After removing a ship, if other members still own that ship model, Org Hangar MUST continue to show it with an updated owner count that excludes the removing member.
- **FR-038**: The system MUST provide clear error feedback when removing a ship fails, and the ship MUST remain in the member's hangar.
- **FR-023**: Org Hangar MUST include a My Ships filter that limits the displayed ships to those owned by the current member.
- **FR-024**: Org Hangar MUST include a member filter listing only organization members who own at least one ship, allowing the current member to filter the gallery to ships owned by a selected member.
- **FR-025**: The member filter MUST include an "All Members" option that clears the member-specific filter.
- **FR-026**: Selecting a specific member in the member filter MUST turn off the My Ships filter.
- **FR-027**: Org Hangar MUST NOT include an Add Ship button.
- **FR-028**: My Hangar MUST NOT include a My Ships filter or a member filter.
- **FR-029**: Both views MUST support infinite scrolling; no pagination controls MUST be visible.
- **FR-030**: When search or filter state changes, the displayed results MUST reset to reflect the new criteria.
- **FR-031**: My Hangar MUST show an empty state encouraging the member to add their first ship when no ships are owned.
- **FR-032**: Org Hangar MUST show an empty state explaining that no members have added any ships to their hangars yet, when there are no hangar entries across the organization.

### Key Entities

- **Ship Model**: A unique ship design in the game database. Has a name and optionally an image. Serves as the canonical identity for grouping in Org Hangar.
- **Hangar Entry**: A record linking a member to a ship model they own. A member may have at most one hangar entry per ship model in this version. This is the only data structure that tracks ship ownership — there is no separate org-level ship record.
- **Organization Member**: An authenticated user who belongs to the organization. Has a display name used in owner lists and filters.

> **Note**: Org Hangar is a derived view, not a separate data store. It aggregates all Hangar Entries across all organization members, grouped by Ship Model. No "org ship" entity exists independently of member Hangar Entries.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Members can navigate to Hangar and see their ships within 2 seconds of the page loading under normal network conditions.
- **SC-002**: Members can add a ship to their hangar in under 30 seconds from clicking Add Ship to seeing the ship in My Hangar.
- **SC-003**: Search results update within 500 milliseconds of the member stopping typing, with no full-page reload required.
- **SC-004**: Org Hangar correctly groups multiple owners of the same ship model so each model appears exactly once regardless of how many members own it.
- **SC-005**: 100% of ships with available images display the image as their card background; 100% of ships without images fall back to the default background.
- **SC-006**: The infinite scroll experience loads additional ships without visible pagination controls at any viewport size.
- **SC-007**: Already-owned ships are clearly distinguished in the Add Ship dialog, preventing accidental duplicate additions.

## Assumptions

- The ship database referenced in the Add Ship dialog is the same data imported via the ship data import feature (spec 006). The database is already populated with ship records, including image URLs where available.
- Organization membership is established through the existing identity and authentication system. Org Hangar is a derived view that aggregates all hangar entries belonging to members of the same organization as the current user. There is no separate data store for org-level ships.
- The member filter in Org Hangar lists only organization members who own at least one ship. Members with no ships do not appear in the filter.
- Ship images are stored as URLs and served from an external source (e.g., a CDN). The application does not manage image uploads.
- No role-based access restrictions apply to viewing Hangar in this version — all authenticated organization members can see both My Hangar and Org Hangar.
- No sorting controls are provided in this version; display order is determined by the system (insertion order or alphabetical is acceptable).
- No pagination is surfaced to the user; infinite scroll is the sole mechanism for loading additional results.
- The Add Ship dialog is the only entry point for adding ships in this version; bulk import and CSV upload are out of scope.
- Hangar does not support ship quantities — a member either owns a ship model or they do not.
