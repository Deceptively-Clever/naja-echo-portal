# Feature Specification: Character Registration & RSI Verification

**Feature Branch**: `015-character-registration`

**Created**: 2026-06-20

**Status**: Draft

**Input**: User description: "A new feature for character registration. When initiating a character registration, the authenticated user is given a unique id to place on their character profile page on the website and supplies their in-game handle. The user then selects verify which will check https://robertsspaceindustries.com/en/citizens/{handle} for that unique id. If found the character is added to the characters table and linked to that authenticated user."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Start Character Registration and Receive Verification Token (Priority: P1)

An authenticated member visits their profile page and sees a "Register Character" section. They click to initiate registration. The system immediately generates a unique short-lived verification token and displays it prominently alongside clear instructions: copy this token and paste it into the bio field of their RSI citizen profile at robertsspaceindustries.com. An expiry countdown (30 minutes) is shown so the member knows how long the token remains valid.

**Why this priority**: Token generation and display is the entry point for the entire verification flow — nothing else is possible without it.

**Independent Test**: Can be fully tested by clicking "Register Character" on the profile page and confirming a unique token with an expiry indicator is displayed.

**Acceptance Scenarios**:

1. **Given** an authenticated member is on their profile page, **When** they click "Register Character", **Then** a unique token is generated and displayed with copy-to-clipboard functionality and a 30-minute expiry indicator.
2. **Given** a member already has a pending registration token that has not expired, **When** they initiate registration again, **Then** the same pending token is shown (not a new one) to prevent orphaned tokens.
3. **Given** a member's previous token has expired, **When** they initiate registration again, **Then** a fresh token is generated.

---

### User Story 2 — Verify RSI Handle Ownership (Priority: P1)

After placing the verification token in their RSI citizen profile bio, the member returns to their profile page, enters their in-game handle into the provided field, and clicks "Verify". The system fetches their public RSI citizen page and searches for the token in the page content. If the token is found and still valid, the character is saved and immediately appears in the member's character list on their profile page.

**Why this priority**: Handle verification is the core trust mechanism — the feature delivers no value without successful ownership confirmation.

**Independent Test**: Can be fully tested end-to-end by placing the token in an RSI bio, submitting a handle, and confirming the character appears in the member's character list.

**Acceptance Scenarios**:

1. **Given** a member has placed their token in their RSI bio and enters their handle, **When** they click "Verify" and the token is found on the RSI page, **Then** a character record is created linked to their account and a success confirmation is shown.
2. **Given** a member enters a handle but has not placed the token in their RSI bio, **When** they click "Verify", **Then** no character is created and a clear "Token not found on your RSI profile" message is displayed.
3. **Given** a member's token has expired before they click "Verify", **When** verification is attempted, **Then** the request is rejected with a "Token expired — please start a new registration" message.
4. **Given** a member enters a handle that is already registered by another user, **When** they click "Verify", **Then** registration is blocked and an "This handle is already claimed" message is shown, regardless of whether the token was found.
5. **Given** the RSI citizen page for the submitted handle does not exist, **When** verification runs, **Then** no character is created and a "RSI citizen profile not found for that handle" message is shown.

---

### User Story 3 — View Registered Characters (Priority: P2)

A member can see all of their registered and verified characters listed on their profile page, showing each character's name and handle. This allows them to confirm which alts are registered and track their linked characters.

**Why this priority**: Visibility into registered characters is necessary for members to manage their linked identities, but depends on Story 2 producing data to display.

**Independent Test**: Can be fully tested by registering at least one character and confirming it appears in the character list with the correct name and handle.

**Acceptance Scenarios**:

1. **Given** a member has one or more verified characters, **When** they view their profile page, **Then** each character is listed with its name and handle.
2. **Given** a member has no registered characters, **When** they view their profile page, **Then** the character section shows an empty state with a prompt to register their first character.

---

### Edge Cases

- What happens when the RSI website is temporarily unavailable during verification? → The system returns a "Could not reach RSI — please try again shortly" error; the token remains valid so the member can retry.
- What happens if a member submits a handle with inconsistent casing (e.g., "StarFighter" vs "starfighter")? → Handle matching is case-insensitive for duplicate checks; the handle is stored exactly as provided by the member.
- What happens if the token appears in the RSI page content unintentionally (e.g., in an ad or injected content)? → Token values are sufficiently unique (high-entropy) that false positive matches are negligible; this is an accepted risk of the basic scrape approach.
- What happens if the member registers a second character using the same handle they already own? → Registration is blocked with a "You have already registered this handle" message (treated as a duplicate regardless of who owns it).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST generate a unique, high-entropy verification token when a member initiates character registration.
- **FR-002**: System MUST display the verification token to the member with a copy-to-clipboard mechanism and a visible expiry indicator showing the remaining validity window (30 minutes).
- **FR-003**: System MUST accept a member-provided RSI handle and fetch the corresponding public RSI citizen page to search for the verification token.
- **FR-004**: System MUST create a character record — storing the character name and handle name — linked to the authenticated member's account when the token is found on the RSI page and has not expired.
- **FR-005**: System MUST reject duplicate handle registration: a handle already linked to any account (including the requesting member's own account) cannot be registered again.
- **FR-006**: System MUST reject verification attempts where the token has exceeded its 30-minute validity window and instruct the member to start a new registration.
- **FR-007**: System MUST allow a member to register more than one character; each additional character requires its own independent verification flow.
- **FR-008**: System MUST display all of a member's verified characters on their profile page with each character's name and handle.
- **FR-009**: System MUST present a clear, human-readable error message for each failure case: token not found on RSI page, token expired, handle already claimed, RSI page not found, RSI temporarily unreachable.
- **FR-010**: System MUST reuse an existing pending token if the member initiates registration again before their current token expires, rather than generating a new one.

### Key Entities

- **Character**: Represents a verified Star Citizen in-game identity belonging to a member. Attributes: character name, RSI handle name, owning member, registration date. A member may own many characters; each RSI handle is unique across all members.
- **Pending Registration**: A short-lived record representing an in-progress verification attempt. Attributes: owning member, verification token, expiry timestamp. Exists only for the duration of the verification window; discarded on success or expiry.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Members can complete a successful character registration — from initiating registration to seeing the character appear on their profile — in under 5 minutes (excluding time spent editing their RSI profile bio).
- **SC-002**: 100% of verified characters are uniquely owned — no two member accounts share the same RSI handle.
- **SC-003**: Verification token lookups correctly detect the token presence in the RSI citizen page with zero false positives across all tested RSI profile layouts.
- **SC-004**: All failure cases (token not found, token expired, handle duplicate, RSI unreachable) present a clear error message that allows the member to understand what went wrong and what to do next without external assistance.
- **SC-005**: Members with multiple characters can see all of their registered handles listed on their profile page within one page load after registration completes.

## Assumptions

- The RSI citizen profile page at `https://robertsspaceindustries.com/en/citizens/{handle}` is publicly accessible without authentication and returns the bio content in its HTML.
- The **character name** is the RSI Community Moniker (display name) scraped from the citizen page during verification — not a label entered by the member. The **RSI handle** is the RSI-assigned username used to locate the public profile page. If the moniker cannot be parsed, the handle is stored as the character name. *(Product-owner direction — see plan research R1/R4; overrides the original member-chosen-name assumption.)*
- Token validity window of 30 minutes is sufficient for a member to switch to their browser, navigate to their RSI profile, paste the token into their bio, and return to the portal.
- Structured HTML parsing (via AngleSharp) of the RSI citizen page is used to extract the Community Moniker display name and confirm token presence; no RSI API key or authenticated access is required.
- The existing user profile page already exists as a feature in the portal; this feature adds a new "Characters" section to it.
- Mobile support follows whatever the existing profile page supports; no special mobile-specific character registration flow is required.
- Character deletion or deregistration is out of scope for v1.
- Re-verification (re-proving ownership of an already-registered handle) is out of scope for v1.
- Syncing RSI profile data beyond the Community Moniker display name (e.g., avatar, org affiliation, account age) is out of scope for v1. The Community Moniker is synced as a narrow exception per product-owner direction (research R1).
