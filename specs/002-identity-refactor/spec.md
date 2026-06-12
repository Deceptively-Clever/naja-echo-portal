# Feature Specification: Identity-Backed Authentication Refactor

**Feature Branch**: `002-identity-refactor`

**Created**: 2026-06-12

**Status**: Draft

**Input**: Refactor the authentication system to use a managed application identity system while
continuing to support Discord OAuth login as the external sign-in provider.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - New User Signs In With Discord (Priority: P1)

A user who has never used NajaEchoPortal before clicks "Sign in with Discord". After authorising
on Discord's consent screen, the application automatically creates a linked application account
for them and redirects them to the authenticated dashboard — no manual registration required.

**Why this priority**: This is the entry point for every new user. Without it no one can access
the application. Correct first-time account creation underpins all future auth behaviour.

**Independent Test**: Can be fully tested by simulating a Discord callback with a previously
unseen Discord user ID and verifying a new application account is created and a secure session
is issued.

**Acceptance Scenarios**:

1. **Given** a Discord user who has no existing NajaEchoPortal account,
   **When** they complete the Discord OAuth consent screen and the callback is received,
   **Then** a new application account is created and linked to that Discord identity, and the
   user is redirected to the dashboard root with an active session. No return-URL
   mechanism is supported; post-login destination is always the dashboard root.

2. **Given** a Discord user who has no existing account,
   **When** the callback is received,
   **Then** Discord provider tokens are not exposed to the browser and do not appear in the
   session data returned to the frontend.

3. **Given** an error occurs during the callback (e.g. OAuth state mismatch or Discord error),
   **When** the callback is processed,
   **Then** the user is shown a safe, user-appropriate error message and no partial account is
   created.

---

### User Story 2 - Returning User Signs In With Discord (Priority: P1)

A user who already has an application account signs in with Discord again. The application
recognises their Discord identity, finds the existing account, and restores their session
without creating a duplicate account.

**Why this priority**: Equally critical as new-user sign-in; a returning user who is treated
as a new user each time would lose all their data and receive duplicate accounts.

**Independent Test**: Can be fully tested by completing the login flow twice with the same
Discord user ID and confirming only one application account exists after both attempts.

**Acceptance Scenarios**:

1. **Given** a returning Discord user who already has an application account,
   **When** they complete the Discord OAuth flow,
   **Then** their existing application account is used — no duplicate account is created — and
   they are redirected to the dashboard root with an active session.

2. **Given** a returning Discord user,
   **When** the session is issued,
   **Then** the session reflects the same application user identity as previous sessions.

---

### User Story 3 - Authenticated User Checks Session State (Priority: P2)

The frontend application queries the backend to determine whether the current browser session
is authenticated. The response tells the frontend who is signed in (or that no one is), so
it can decide whether to show protected routes or redirect to sign-in.

**Why this priority**: Required for the frontend auth guard. Without it the frontend cannot
distinguish authenticated from unauthenticated state.

**Independent Test**: Can be fully tested independently by calling the session/current-user
endpoint with and without an active session cookie and verifying the correct responses.

**Acceptance Scenarios**:

1. **Given** a browser session with an active application login,
   **When** the frontend requests the current-user/session endpoint,
   **Then** the response contains the authenticated user's application user ID, display name,
   and Discord username — and no Discord provider tokens.

2. **Given** a browser with no active application session,
   **When** the frontend requests the current-user/session endpoint,
   **Then** the response is `200 OK` with a body that explicitly indicates unauthenticated state
   (e.g., `{ "authenticated": false }`), not an HTTP error status.

---

### User Story 4 - Authenticated User Signs Out (Priority: P2)

A signed-in user chooses to sign out. The application clears the server-side session and the
browser cookie, leaving them in an unauthenticated state. Subsequent navigation to protected
routes is blocked.

**Why this priority**: Core session management. Users must be able to end their own session
securely, particularly on shared devices.

**Independent Test**: Can be fully tested by signing in, calling the sign-out endpoint, then
attempting to access a protected endpoint and confirming the unauthenticated response.

**Acceptance Scenarios**:

1. **Given** a signed-in user,
   **When** they call the sign-out endpoint,
   **Then** the application session is cleared on the server, the session cookie is expired in
   the browser, and subsequent requests to protected endpoints return the unauthenticated
   response.

2. **Given** a user who has signed out,
   **When** they navigate to a protected frontend route,
   **Then** they are redirected to the sign-in page or shown the appropriate unauthenticated
   state.

---

### User Story 5 - Authenticated Frontend Routes Remain Protected (Priority: P2)

All frontend routes that require authentication (dashboard and sub-pages) check session state
on load and deny access to unauthenticated visitors.

**Why this priority**: Prevents unauthenticated access to org-management content.

**Independent Test**: Can be tested by navigating directly to a protected route without a
session and confirming the auth guard redirects rather than rendering the protected content.

**Acceptance Scenarios**:

1. **Given** an unauthenticated user,
   **When** they navigate directly to any dashboard route,
   **Then** they are redirected to sign-in rather than seeing protected content.

2. **Given** an authenticated user,
   **When** they navigate to any dashboard route,
   **Then** the protected content is rendered without requiring re-authentication.

---

### Edge Cases

- What happens when the Discord OAuth `state` value is missing or does not match (CSRF
  protection)? → The callback must be rejected; the user is shown a safe error state; no
  account or session is created.
- What happens when Discord returns an error response on the callback (user denied authorisation)?
  → The user is returned to sign-in with a safe error message.
- What happens when a session cookie is present but the corresponding server-side session has
  expired or been invalidated? → The user is treated as unauthenticated; the stale cookie is
  cleared; they are prompted to sign in again.
- What happens if the sign-out endpoint is called when no session exists? → The request completes
  successfully (idempotent); nothing is logged or errored about the absence of a session.
- What happens if two concurrent first-time logins arrive for the same Discord identity? → Only
  one application account is created; the second request finds and uses the existing account.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST use a managed local user identity system as the source of truth
  for application users, supporting user creation, login linkage, and future roles and claims.
- **FR-002**: Discord MUST be supported as an external OAuth login provider. It is the only
  supported provider in this feature.
- **FR-003**: When a Discord user signs in for the first time, the application MUST automatically
  create a local application account and link it to the Discord identity.
- **FR-004**: When a Discord user who already has an application account signs in, the application
  MUST detect the existing account via the Discord provider key and use it — no duplicate accounts.
- **FR-005**: After a successful external login, the application MUST issue a server-managed
  browser session using a secure cookie.
- **FR-006**: The application MUST expose a current-user/session endpoint that always returns
  `200 OK`. When a valid session is present the response body contains the authenticated user's
  application user ID, display name, and Discord username — and no Discord provider tokens.
  When no session exists the response body explicitly indicates unauthenticated state
  (e.g., `{ "authenticated": false }`). The endpoint MUST NOT return a `401` status for a
  session check — `401` is reserved for requests to protected action endpoints.
- **FR-007**: The application MUST expose a sign-out endpoint that clears the server-side session
  and expires the browser session cookie.
- **FR-008**: The frontend MUST determine authentication state by querying the current-user/session
  endpoint — not by inspecting tokens or cookies directly.
- **FR-009**: The existing frontend auth guard behaviour (allow authenticated dashboard access,
  redirect unauthenticated users) MUST continue to work after the refactor.
- **FR-010**: Authentication error scenarios (OAuth errors, state mismatches, unexpected failures)
  MUST produce safe, user-appropriate error states rather than exposing internal details.
- **FR-011**: The application MUST NOT expose Discord access tokens, refresh tokens, or provider
  credentials to the frontend in any API response, session claim visible to the client, or
  browser storage.
- **FR-012**: Structured log output MUST include enough context to troubleshoot auth flow failures
  without logging OAuth codes, state values, access tokens, refresh tokens, authorisation
  headers, or session cookies.
- **FR-013**: The data model MUST be capable of storing roles and claims per user to support future
  authorisation features, even though no role-based access is enforced in this feature.

### Key Entities

- **Application User**: The local, application-owned identity record for a signed-in person.
  Attributes: unique application ID, display name (sourced from Discord at account creation as
  `global_name`, falling back to `username` when `global_name` is null), Discord username
  (the unique handle, stored for display and future use), and linkage to external login
  providers. Supports future role and claim assignment.
- **External Login Link**: A record tying an Application User to a specific external provider
  and provider user key (e.g., Discord provider + Discord user ID). One user may have multiple
  external login links in future; this feature creates exactly one (Discord).
- **Application Session**: A server-managed browser session cookie representing the authenticated
  Application User. Does not carry provider tokens. Issued on successful login; invalidated
  on sign-out or expiry.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new Discord user can complete sign-in and reach the authenticated dashboard in a
  single, uninterrupted flow — no extra registration step required.
- **SC-002**: A returning Discord user is never presented with a duplicate account; subsequent
  sign-ins always restore the same application identity.
- **SC-003**: The frontend auth guard correctly reflects authenticated state within one round-trip
  to the session endpoint, enabling seamless navigation to protected routes.
- **SC-004**: After sign-out, 100% of protected backend endpoints return the unauthenticated
  response for that browser session.
- **SC-005**: The automated test suite validates all auth flows — new user, returning user,
  session check, sign-out, and unauthenticated access — without any real Discord network calls.
- **SC-006**: No auth-flow log entry contains OAuth codes, state values, access tokens, refresh
  tokens, authorisation headers, or session cookies, as verified by automated tests.
- **SC-007**: A session that has been idle for more than 24 hours is no longer valid; a session
  in active use is not invalidated before its 7-day absolute maximum has elapsed.

## Clarifications

### Session 2026-06-12

- Q: When the frontend calls the current-user endpoint with no active session, what should the backend return? → A: `200 OK` with an explicit `{ "authenticated": false }` discriminated body — same HTTP status in all cases; frontend parses JSON to determine auth state; `401` is reserved for protected action endpoints.
- Q: After a successful Discord login, where is the user redirected? → A: Always to the dashboard root — no return-URL mechanism; post-login destination is fixed.
- Q: Which Discord name field is stored as the application user's display name? → A: `global_name` falling back to `username` when null — human-friendly, always populated.
- Q: What is the desired application session lifetime policy? → A: 7-day sliding window with 24-hour idle timeout — active sessions extend on use; idle sessions expire after 24 h; absolute max 7 days.
- Q: What fields does the current-user endpoint include in the authenticated response? → A: Application user ID + display name + Discord username — no avatar URL; no Discord provider tokens.

## Assumptions

- The existing Discord OAuth application credentials (client ID, client secret, redirect URI)
  remain valid and do not need to change for this refactor.
- The existing database is accessible and can be migrated forward; there is no requirement to
  preserve or migrate any pre-existing custom user records from the current auth implementation.
- Discord's `identify` scope (providing user ID and username) is sufficient for account creation;
  no additional Discord scopes (e.g. `email`, `guilds`) are required by this feature.
- The frontend auth guard currently works against a session-based mechanism; the refactor
  preserves that pattern, so no new frontend routing paradigm is introduced.
- The `__Host-` cookie prefix will be used in production where deployment infrastructure
  supports it; if a load balancer or reverse proxy strips the prefix, this is documented at
  deployment time and `SameSite=Lax` + `Secure` + `HttpOnly` remain non-negotiable.
- Existing Discord OAuth state/CSRF correlation is handled by the authentication middleware;
  this feature does not rewrite that mechanism, only integrates it with the new identity system.
- No admin or manual user provisioning workflow is required; all accounts originate from a
  Discord login.
- Session lifetime uses a 7-day sliding window with a 24-hour idle timeout: an active session
  is extended on each authenticated request up to a 7-day absolute maximum; a session with no
  activity for 24 hours is invalidated regardless of the absolute expiry. No "remember me" or
  long-lived token is issued in this feature.
