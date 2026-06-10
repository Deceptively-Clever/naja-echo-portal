# Feature Specification: Discord Authentication

**Feature Branch**: `001-discord-auth`

**Created**: 2026-06-08

**Status**: Draft

**Input**: User description: "Build the initial authentication experience for a full-stack web application using Discord as the login provider."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - New User Signs In with Discord (Priority: P1)

A visitor arrives at the public landing page with no existing account. They click "Sign in with Discord,"
complete Discord's authorization flow, and are returned to the application as a signed-in user. The
application creates a local profile for them on first login and takes them to the dashboard.

**Why this priority**: This is the entry point to the entire application. Without it, no other
authenticated feature is reachable. Every other story depends on a working sign-in flow.

**Independent Test**: Navigate to the landing page as a visitor, complete the Discord authorization
flow, and verify arrival at the dashboard with the correct display name and avatar shown. A new
local user profile must exist after this action.

**Acceptance Scenarios**:

1. **Given** a visitor is on the public landing page and has no local account,
   **When** they click "Sign in with Discord" and complete Discord authorization,
   **Then** they are redirected to the dashboard, their display name and avatar are shown,
   and a local user profile has been created linked to their Discord account.

2. **Given** a visitor clicks "Sign in with Discord",
   **When** they are redirected to Discord's authorization page,
   **Then** the authorization request asks only for the minimum required Discord scopes
   and does not request Discord server membership, bot permissions, or unnecessary data.

3. **Given** Discord authorization is granted,
   **When** the application processes the callback,
   **Then** the user's Discord display name and avatar are stored in their local profile,
   and their email is stored only if the email scope was granted and the address is verified.

4. **Given** a user has just completed sign-in,
   **When** they view the dashboard,
   **Then** their display name and avatar match the current data from their Discord account.

---

### User Story 2 - Returning User Signs In Again (Priority: P2)

A visitor who has previously signed in returns to the application and signs in with the same Discord
account. No duplicate local profile is created. If their Discord display name or avatar has changed
since their last login, the local profile is updated to reflect the current data.

**Why this priority**: Without this, every return visit creates a duplicate account. This story
establishes the idempotent login contract that makes authentication trustworthy for returning users.

**Independent Test**: Sign in with a Discord account, sign out, then sign in again with the same
account. Verify only one local user profile exists and that the Last Login date has been updated.
Then simulate a Discord profile data change and verify the local profile reflects the new data.

**Acceptance Scenarios**:

1. **Given** a user has previously signed in and a local profile exists for their Discord account,
   **When** they sign in with the same Discord account,
   **Then** no new local profile is created, and the existing profile's Last Login date is updated.

2. **Given** a returning user whose Discord display name has changed since their last login,
   **When** they complete sign-in,
   **Then** the local profile's stored display name is updated to match the current Discord value.

3. **Given** a returning user whose Discord avatar has changed since their last login,
   **When** they complete sign-in,
   **Then** the local profile's stored avatar reference is updated to match the current Discord value.

4. **Given** a returning user signs in,
   **When** no Discord profile data has changed since last login,
   **Then** only the Last Login date is updated; no other profile fields are modified.

---

### User Story 3 - User Signs Out (Priority: P3)

An authenticated user on the dashboard chooses to sign out. Their session is cleared, they are
returned to the public landing page, and they can no longer access protected pages without
signing in again.

**Why this priority**: Sign-out closes the session lifecycle and is essential for shared-device
security and user control over their authentication state.

**Independent Test**: Sign in, navigate to the dashboard, click sign out, then attempt to navigate
directly to the dashboard URL. Verify the user is on the landing page and the dashboard is
inaccessible.

**Acceptance Scenarios**:

1. **Given** an authenticated user on the dashboard,
   **When** they click the sign-out control,
   **Then** their session is cleared and they are returned to the public landing page.

2. **Given** a user has signed out,
   **When** they attempt to navigate directly to a protected page,
   **Then** they are redirected to the public landing page and cannot view the protected content.

3. **Given** a user has signed out,
   **When** they view the landing page,
   **Then** the "Sign in with Discord" option is available and no authenticated user state is shown.

---

### User Story 4 - Unauthenticated Access to Protected Pages Is Prevented (Priority: P4)

A visitor who is not signed in attempts to access a URL for a protected page directly (e.g., by
typing it, bookmarking it, or following a link). The application prevents access and sends them
to the public landing page.

**Why this priority**: This is the core security boundary. Without it, protected content is public.

**Independent Test**: Without signing in, navigate directly to the dashboard URL. Verify that the
application redirects to the landing page and does not display any protected content.

**Acceptance Scenarios**:

1. **Given** a visitor with no active session,
   **When** they navigate directly to a protected page URL,
   **Then** they are redirected to the public landing page without seeing any protected content.

2. **Given** a visitor is redirected to the landing page due to missing authentication,
   **When** they view the landing page,
   **Then** no error message is shown — the landing page is presented normally.

---

### User Story 5 - Discord Authorization Fails, Is Canceled, or Is Denied (Priority: P5)

A visitor begins the sign-in flow but Discord authorization does not succeed — either because the
visitor explicitly denied access, canceled the flow, or an error occurred on Discord's side. The
application returns the visitor to a friendly error state with an option to try again.

**Why this priority**: Failure handling completes the auth flow. Without it, users who encounter any
problem are stranded with no recovery path.

**Independent Test**: Initiate sign-in, then deny authorization on Discord's consent screen.
Verify the application shows a user-friendly message and a "Try again" option. Verify no partial
user profile was created.

**Acceptance Scenarios**:

1. **Given** a visitor clicks "Sign in with Discord" and is redirected to Discord,
   **When** they deny authorization on Discord's consent screen,
   **Then** they are returned to a friendly error state that explains sign-in did not complete
   and offers a "Try again" option.

2. **Given** a visitor is mid-way through Discord authorization,
   **When** they cancel or close the browser tab and return,
   **Then** the application does not leave a partial or broken session state.

3. **Given** Discord returns an error response to the application's callback,
   **When** the application processes the failed callback,
   **Then** the visitor sees a user-friendly message with no internal error details exposed
   and is offered a "Try again" option.

4. **Given** any authorization failure or cancellation scenario,
   **When** the error state is shown,
   **Then** no local user profile has been created or partially written for that attempt.

---

### Edge Cases

- What happens when the same Discord account is used on two different browser tabs simultaneously?
  The application must not create duplicate local profiles; the idempotent login rule applies.
- What happens if Discord returns an avatar identifier that the application cannot resolve at
  display time? The application should show a fallback avatar rather than a broken image.
- What happens if the application's callback URL is called with a forged or replayed state
  parameter? The application must reject the callback and treat it as a failed authorization.
- What happens if Discord's service is unavailable during the callback? The user must see a
  friendly error with a retry option; no partial profile is created.
- What happens if a user's Discord email is removed or unverified after a prior login stored it?
  The application must not fail on subsequent logins; email becomes absent in the stored profile.

## Requirements *(mandatory)*

### Functional Requirements

**Sign-In Flow**

- **FR-001**: The application MUST provide a public landing page accessible to unauthenticated visitors.
- **FR-002**: The landing page MUST offer a "Sign in with Discord" action.
- **FR-003**: Clicking "Sign in with Discord" MUST initiate the Discord authorization flow.
- **FR-004**: The application MUST request only the minimum Discord scopes necessary for login
  (identity and, optionally, email if email collection is desired).
- **FR-005**: After successful Discord authorization, the application MUST redirect the user to
  the authenticated dashboard.
- **FR-006**: On first login, the application MUST create a local user profile linked to the
  Discord account.
- **FR-007**: On subsequent logins, the application MUST NOT create a new local profile;
  the existing profile MUST be located by the Discord user identifier.
- **FR-008**: On every successful login, the application MUST update the local profile's Last Login date.
- **FR-009**: On every successful login, the application MUST update the local profile's display name
  and avatar reference if they differ from the values received from Discord.
- **FR-010**: The application MUST store the user's email address if and only if the email scope
  was granted and Discord provides a verified email address.

**Session and Access Control**

- **FR-011**: A successful login MUST establish an authenticated session for the user.
- **FR-012**: Discord access tokens and refresh tokens MUST NOT be exposed to the frontend.
- **FR-013**: Authenticated sessions MUST be cleared when the user signs out.
- **FR-014**: Protected pages MUST be inaccessible to visitors without an active authenticated session.
- **FR-015**: Unauthenticated requests to protected pages MUST redirect to the public landing page.

**Dashboard**

- **FR-016**: The authenticated dashboard MUST display the signed-in user's display name.
- **FR-017**: The authenticated dashboard MUST display the signed-in user's avatar.
- **FR-018**: The authenticated dashboard MUST provide a sign-out action.

**Error Handling**

- **FR-019**: If Discord authorization is denied, canceled, or fails, the application MUST return
  the visitor to an error state with a user-friendly message and a "Try again" action.
- **FR-020**: Error messages shown to the user MUST NOT expose internal error details, stack traces,
  or system implementation information.
- **FR-021**: Sensitive authentication data (tokens, secrets, identifiers used internally) MUST NOT
  appear in application logs.
- **FR-022**: A failed or canceled authorization attempt MUST NOT result in a created or partially
  written local user profile.

**Security**

- **FR-023**: The application MUST validate that the OAuth state parameter returned in the Discord
  callback matches the one issued at the start of the authorization flow, rejecting mismatches.
- **FR-024**: The application MUST NOT require Discord server membership or bot permissions.

### Key Entities

- **UserProfile**: Represents a local application user. Attributes: internal user ID (unique,
  system-generated), Discord user ID (unique, from Discord), display name (from Discord),
  avatar reference (URL or identifier, from Discord), email address (optional, only if granted),
  created date, last login date, last updated date.

- **AuthSession**: Represents an active authenticated session. Attributes: session identifier,
  associated internal user ID, session creation time, expiry. Exists only on the server side;
  MUST NOT contain raw Discord tokens visible to the frontend.

- **AuthorizationAttempt**: A transient record of an in-progress Discord authorization flow.
  Attributes: state parameter (for CSRF validation), creation time. Discarded after the flow
  completes or fails.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new visitor can complete the full sign-in flow — landing page to dashboard — in
  under 60 seconds under normal conditions.
- **SC-002**: A returning visitor signing in with the same Discord account never results in more
  than one local user profile for that Discord account, across any number of repeated sign-ins.
- **SC-003**: A signed-out visitor attempting to access a protected page is redirected to the
  landing page 100% of the time, with no protected content visible.
- **SC-004**: Discord authorization failures, cancellations, and denials are handled gracefully
  in all cases — the visitor always reaches a friendly error state with a retry option,
  never a blank page or raw error.
- **SC-005**: All acceptance scenarios defined in User Stories 1–5 are covered by testable
  acceptance criteria and verifiable without knowledge of the implementation.

## Assumptions

- Users have access to a Discord account and can authorize via Discord's web interface.
- The application is a web application accessible from a standard desktop or mobile browser.
- Session duration follows standard web-application defaults (session expires on browser close
  or after a reasonable inactivity period); explicit "remember me" persistence is out of scope.
- Email collection is optional at the scope level — if the email scope is not requested or not
  granted, the application functions normally without it.
- The Discord avatar field may be an opaque identifier rather than a full URL; the application
  stores whatever Discord provides and constructs display URLs as needed without storing secrets.
- The "Try again" option on the error state returns the visitor to the beginning of the
  sign-in flow (i.e., back to the landing page or directly re-initiates authorization).
- The application is a single-tenant deployment (no multi-tenant or white-labeling concerns).
- No rate limiting or brute-force protection is required for this initial version beyond what
  Discord's own authorization flow provides.
- The application is not required to support deep-link preservation — unauthenticated visitors
  redirected from a protected page land on the landing page, not back on their original URL.
