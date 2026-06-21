# Feature Specification: Admin Users Page

**Feature Branch**: `017-admin-users-page`

**Created**: 2026-06-21

**Status**: Draft

**Input**: User description: "A new admin section 'Users'. The user page shows all authenticated users, any characters they have registered to them and any roles they have. It can be filtered by auth name, character name, or role. This page is only visible to admins. It should also have the option to add a registered character to an authenticated user just by specifying the RSI handle. When adding, it will fetch the RSI page same as registered to verify that the handle exists and pull the character's name from the page."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — View All Users with Characters and Roles (Priority: P1)

An admin navigates to the Users section of the admin panel and sees a table listing every
authenticated member. Each row shows the member's Discord auth name, their assigned roles
displayed with friendly names, and the names and handles of any characters registered to them.
The admin can type in a filter box to narrow the list by auth name, character name, or role
in real time without a page reload.

**Why this priority**: The user list is the entry point for all admin user-management tasks on
this page. Without it, the character-add action has no context, and there is no way to audit
membership, roles, or character registrations without direct database access.

**Independent Test**: Navigate to `/dashboard/admin/users` as an admin and confirm all registered
members appear with correct character and role data. Confirm the page is inaccessible (redirected
or denied) for non-admin accounts. (The conceptual section is `/admin/users`; the concrete path
under the existing admin nesting is `/dashboard/admin/users` — research Decision 7.)

**Acceptance Scenarios**:

1. **Given** an admin is authenticated, **When** they navigate to `/admin/users`, **Then** a
   table is displayed with one row per authenticated user, each showing the member's auth name,
   assigned roles (friendly names), and registered characters (name and handle).
2. **Given** a member has two registered characters, **When** the admin views the users table,
   **Then** both characters (name and handle) appear in that member's row.
3. **Given** a member has no registered characters, **When** the admin views the users table,
   **Then** that member's row shows an empty character list (not an error or blank row).
4. **Given** the admin types a character name in the filter input, **When** the filter is applied,
   **Then** only rows containing a character matching that name remain visible; all others are
   hidden.
5. **Given** the admin types a role name in the filter input, **When** the filter is applied,
   **Then** only rows where the member holds a matching role remain visible.
6. **Given** the admin types an auth name in the filter input, **When** the filter is applied,
   **Then** only rows whose auth name matches remain visible.
7. **Given** a non-admin authenticated user navigates to `/admin/users`, **When** the page
   loads, **Then** they are denied access (redirected away from the admin section or shown a
   403 response).
8. **Given** an unauthenticated user navigates to `/admin/users`, **When** the page loads,
   **Then** they are redirected to the sign-in page.

---

### User Story 2 — Add a Character to a User (Priority: P2)

An admin selects a member from the users table and uses the "Add Character" action to attach
a character to that member by entering an RSI handle. The system verifies the handle exists on
the RSI website, retrieves the character's name, and links the character to the member — skipping
the token-verification flow required for self-registration. If the handle is already registered
to any member (including the target), the add is blocked and the admin sees a clear error.

**Why this priority**: This is the primary write action on the page and directly unblocks members
who cannot complete self-registration. It depends on the user list (P1) being rendered, so it is
naturally P2.

**Independent Test**: From the users table, invoke "Add Character" for a specific member, enter a
valid RSI handle not yet registered, and confirm the character appears in that member's row
immediately. Then attempt to add the same handle again and confirm it is blocked with an error.

**Acceptance Scenarios**:

1. **Given** an admin has selected a member and entered a valid, unregistered RSI handle, **When**
   they submit the Add Character form, **Then** the system fetches the RSI citizen page, confirms
   it exists, retrieves the character's name, creates the character record linked to that member,
   and the character appears in the member's row in the users table.
2. **Given** an admin enters an RSI handle that does not exist on the RSI website, **When** they
   submit the Add Character form, **Then** the system returns an error message stating the handle
   was not found and no character record is created.
3. **Given** an admin enters an RSI handle already registered to any member (including the target
   member), **When** they submit the Add Character form, **Then** the system blocks the action and
   displays an error indicating the handle is already claimed.
4. **Given** the RSI website is unreachable during the add attempt, **When** the admin submits
   the form, **Then** the system displays an error indicating the handle could not be verified
   and no character record is created.
5. **Given** the Add Character form is submitted with an empty or whitespace-only handle, **When**
   the form is validated, **Then** an inline error is displayed before any network request is made.

---

### Edge Cases

- If the RSI citizen page returns HTTP 200 but no recognizable character name can be extracted
  (malformed structure, name field absent), the add is blocked with a distinct error: "Character
  name could not be retrieved — the handle may be valid but the RSI page returned no name."
  This is separate from "handle not found" (FR-007) and "service unavailable" (FR-008).
- How does the table render when a member has a very large number of characters (layout overflow)?
- What if filtering produces zero results — is there a clear empty-state message?
- What if the RSI handle contains special characters that are invalid per RSI naming rules?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST restrict access to the `/admin/users` page and all related data
  endpoints to users holding the Admin role; any other authenticated or unauthenticated user
  MUST be denied access.
- **FR-002**: The system MUST display a table of all authenticated users, with each row showing
  the member's auth name (Discord display name), their assigned roles using friendly display names,
  and all characters registered to that member (character name and RSI handle).
- **FR-003**: The system MUST allow real-time filtering of the users table by auth name, character
  name, and role without a page reload; the filter MUST match any of the three fields so a single
  input narrows results across all three simultaneously.
- **FR-004**: The system MUST provide an "Add Character" action per user that allows an admin to
  enter an RSI handle and attach a new character record to the target user.
- **FR-005**: Before creating a character record, the system MUST fetch the RSI citizen page for
  the provided handle, confirm the page exists and is valid, and extract the character's display
  name from that page.
- **FR-006**: The system MUST block the Add Character action if the provided RSI handle is already
  registered to any user (including the target user), and MUST display a clear error message
  explaining the handle is already claimed.
- **FR-007**: The system MUST block the Add Character action if the RSI citizen page for the
  handle cannot be found (non-existent handle), and MUST display a clear error message.
- **FR-008**: The system MUST block the Add Character action if the RSI citizen page cannot be
  reached or returns an unusable response, and MUST display a clear error message distinguishing
  a "handle not found" error from a "verification service unavailable" error.
- **FR-009**: The system MUST block the Add Character action if the RSI citizen page returns a
  successful response but no recognizable character name can be extracted, and MUST display a
  distinct error: "Character name could not be retrieved — the handle may be valid but the RSI
  page returned no name." This is a third failure mode, separate from FR-007 and FR-008.
- **FR-010**: The users table MUST display all members regardless of whether they have any
  registered characters or assigned roles (empty states are valid rows).
- **FR-011**: Role names displayed in the table MUST use friendly display names rather than raw
  internal role identifiers.

### Key Entities *(include if feature involves data)*

- **User (authenticated member)**: Represents a member who has signed in via Discord OAuth.
  Has an auth name (Discord username), zero or more assigned roles, and zero or more registered
  characters.
- **Role**: A permission designation assigned to a user. Displayed with a friendly name (e.g.,
  "Administrator" instead of "Admin").
- **Character**: A Star Citizen character linked to a user. Has a display name (scraped from RSI)
  and an RSI handle (the unique citizen identifier). A character belongs to exactly one user.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An admin can locate any specific member in the users list within 10 seconds of
  navigating to the page, using the filter input.
- **SC-002**: The users list loads and is readable within 3 seconds for the full member roster.
- **SC-003**: An admin can complete the Add Character workflow (enter handle, submit, see result)
  in under 60 seconds for a valid, unregistered handle.
- **SC-004**: 100% of Add Character attempts with a duplicate handle are blocked before a
  character record is created.
- **SC-005**: 100% of attempts to access the admin users page by non-admin accounts result in
  denial of access; no user or character data is exposed.
- **SC-006**: Admins require zero direct database interventions to attach a character to a member
  who cannot self-register (compared to the current baseline of 100% requiring database access).

## Assumptions

- The member roster is small enough (current org size) that loading all users in a single request
  and filtering client-side is acceptable without pagination.
- The RSI handle verification and name-scraping logic from the character self-registration feature
  (015) is reused without duplication; no new external integration is introduced.
- Friendly role display names are defined by a static map within the application; no dynamic
  role-name configuration is required in v1.
- Removing or unlinking a character from a user is out of scope for v1.
- Editing a user's roles from this page is out of scope for v1.
- Bulk character import is out of scope for v1.
- Viewing user activity or login history is out of scope for v1.
- The Add Character action skips the token-generation and ownership-verification steps used in
  self-registration; admin authority is the only authorization required.
- Audit logging of admin character-add actions is out of scope; there is only one admin and no
  audit trail is required.

## Clarifications

### Session 2026-06-21

- Q: When the RSI citizen page returns HTTP 200 but no character name can be extracted, should this be a distinct third error mode or map to an existing one? → A: Distinct third error mode — block the add with "Character name could not be retrieved — the handle may be valid but the RSI page returned no name." (Added FR-009; edge case resolved.)
- Q: Should admin character-add actions emit structured audit log entries as a formal requirement? → A: No — this is a single-admin side project, audit logging is out of scope. (Removed from Assumptions; no FR added.)
