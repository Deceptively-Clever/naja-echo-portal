# Phase 0 Research: Character Registration & RSI Verification

All unknowns below were resolved before Phase 1. No open `NEEDS CLARIFICATION` items remain.

## R1 — How is the character `name` collected? (resolved by product owner)

**Decision**: The character `name` is **scraped from the RSI citizen page** during verification — it is
the RSI **Community Moniker** (display name), not entered by the member. The member supplies only the
**handle**; verification reads the same page it scrapes for the token and extracts the moniker as the
stored `name`. If the moniker cannot be parsed, fall back to storing the `handle` as the `name`.

**Worked example** (product owner): for handle `g8r`
(`https://robertsspaceindustries.com/en/citizens/g8r`), the page labels `G8R` as the **"Handle name"**
and shows **`G8trdone`** as the display name / Community Moniker (the primary heading). So `handle = g8r`,
`name = G8trdone`.

**Rationale**: The product owner directed that the name come from RSI, which makes the character record
reflect the real in-game identity rather than a free-form member label, and removes a form field from the
verify step (the member only ever types a handle, matching the user scenarios). The moniker lives on the
*same* page already being fetched for the token, so no extra request is needed.

**Scope note / spec deviation**: The spec's Assumptions said character name was a member-chosen label and
its out-of-scope list said "syncing additional RSI profile data … is out of scope." This decision
narrowly overrides both, per explicit product-owner direction: we scrape exactly **one** additional
field — the display name — as part of identity confirmation. No avatar/org/account-age sync is added;
the broader out-of-scope boundary still holds. Update the spec's Assumptions to match.

**Alternatives considered**: Member-entered name (rejected — product owner wants the authoritative RSI
moniker); a separate request for profile metadata (rejected — the moniker is on the page already fetched).

## R2 — Token generation (entropy & format)

**Decision**: Generate a high-entropy token via `System.Security.Cryptography.RandomNumberGenerator`
(16 random bytes → URL-safe Base64, ~22 chars), with a short human-readable prefix (e.g. `naja-`) so the
member recognizes what they pasted. Generation lives in a `PendingCharacterRegistration.Create(ownerId, now)`
domain factory.

**Rationale**: 128 bits of entropy makes accidental/adversarial false-positive matches in scraped HTML
negligible (spec edge case + SC-003). `RandomNumberGenerator` is BCL, so the Domain layer stays
dependency-free (Principle VI). A factory keeps the token+expiry invariant in one place.

**Alternatives considered**: `Guid.NewGuid()` (rejected — only 122 bits and visually ambiguous as a
"paste this" value); a shorter 6-digit code (rejected — too low-entropy for an HTML substring scan).

## R3 — Pending-registration lifecycle & token reuse (FR-010)

**Decision**: At most **one** pending registration row per member, enforced by a unique index on
`owner_user_id`. `StartRegistration` reads the caller's row: if it exists and `expires_at > now`, return
the existing token; otherwise create/replace it with a fresh token and a `now + 30 min` expiry. On
successful verification the pending row is **deleted**.

**Rationale**: Directly satisfies US1 scenarios 2–3 (reuse non-expired, regenerate expired) and FR-010
without orphaning tokens. The unique index makes "one pending per member" a database invariant, not just
application logic. Deletion-on-success keeps the table to in-flight rows only.

**Alternatives considered**: Keep historical pending rows (rejected — YAGNI, no requirement); a
background expiry sweep (rejected — expiry is checked at read/verify time; stale rows are harmless and
overwritten on next start). A periodic cleanup job can be added later if the table ever grows, but is out
of scope for v1.

## R4 — RSI page fetch, token matching, and moniker extraction

**Decision**: A typed `HttpClient` (`RsiCitizenClient : IRsiCitizenClient`) GETs
`/en/citizens/{handle}` against base address `https://robertsspaceindustries.com/` with a bounded
timeout (≈10 s). Map transport results to a small outcome: **200** → parse the page and return an
`RsiCitizenPage { string Content, string? DisplayName }`; **404** → profile-not-found; **timeout /
network error / 5xx** → unreachable. `DisplayName` is the parsed Community Moniker (the profile's primary
heading, e.g. `G8trdone`); `Content` is the page text used for the token scan. The `VerifyCharacter`
handler does a plain case-sensitive substring search for the token in `Content` (token is high-entropy,
so case folding is unnecessary and substring is sufficient) and stores `DisplayName` as the character
`name`, falling back to the submitted handle when `DisplayName` is null/blank.

**Parsing approach**: Use **AngleSharp** to extract the moniker from a known element rather than regex.
Now that R1 requires structured extraction of a specific field (not just a boolean token presence), a
real DOM parser is the robust choice and is appropriately justified (Principle IV — the requirement is
concrete, not speculative). **AngleSharp is a new dependency**: subject to the constitution's
licence/security review (MIT-licensed, widely used). The token presence check remains a substring scan
over the page text — no parser needed for that half.

**Rationale**: Mirrors the existing UEX typed-`HttpClient` + Application-port pattern (`AddHttpClient<TI,TImpl>`),
keeping outbound HTTP **and HTML parsing** in Infrastructure behind a port the handler fakes in tests
(Principle II/VI — the handler never sees HTML). A bounded timeout converts RSI slowness into a clean
"try again" error (Performance Goals, edge case). Substring match on a 128-bit token gives zero practical
false positives (SC-003). The moniker-parse fallback-to-handle keeps verification resilient to RSI markup
changes — a layout change degrades the *name* to the handle but never blocks a valid registration.

**Alternatives considered**: Regex moniker extraction (rejected — brittle against RSI markup vs. a DOM
query); a second request for profile metadata (rejected — the moniker is on the page already fetched);
RSI's authenticated API (rejected — assumption says no API key/auth; out of scope).

**Risk noted in spec**: A token could appear elsewhere on the page (ads/injected content). The high token
entropy makes this negligible; accepted per the spec's edge-case note.

## R5 — Handle uniqueness (global, case-insensitive) — FR-005, SC-002

**Decision**: Enforce uniqueness in two layers. (1) Application pre-check in `VerifyCharacter`: query
`characters` for any row whose handle matches case-insensitively (`lower(handle) = lower(@input)`),
**before** the RSI fetch, returning `HandleAlreadyClaimed` if found. (2) A database **unique index on
`lower(handle)`** as the race guard; a unique-violation on insert is also mapped to `HandleAlreadyClaimed`.
The handle is stored exactly as the member typed it.

**Rationale**: The pre-check gives a clean, specific error message (FR-009) and satisfies US2 scenario 4's
"regardless of whether the token was found" by running before the scrape. The functional `lower(handle)`
unique index guarantees SC-002 ("100% uniquely owned") even under concurrent verifies, where two
app-level checks could both pass. Storing the original casing satisfies the edge case ("stored exactly as
provided").

**Alternatives considered**: App-check only (rejected — racy, can't guarantee SC-002); store a normalized
lowercased handle column (rejected — loses original casing; the functional index achieves the same
guarantee without a redundant column).

## R6 — Error → HTTP status mapping (RFC-7807, FR-009)

**Decision**: Follow the existing endpoint `catch`-to-`Results.Problem` convention. Each Application
exception maps to a status + title; the frontend renders the `title`/`detail` as the member-facing
message.

| Condition | Exception | Status | Title (member message) |
|-----------|-----------|--------|--------------------------|
| Token absent from RSI page | `TokenNotFoundException` | 422 | Token not found on your RSI profile |
| Token expired / no pending | `TokenExpiredException` | 409 | Token expired — please start a new registration |
| Handle already claimed (any account) | `HandleAlreadyClaimedException` | 409 | This handle is already claimed |
| RSI profile not found (404) | `RsiProfileNotFoundException` | 404 | RSI citizen profile not found for that handle |
| RSI unreachable (timeout/5xx/network) | `RsiUnreachableException` | 502 | Could not reach RSI — please try again shortly |
| Malformed input (empty handle, etc.) | `ArgumentException` | 400 | Validation error |

**Rationale**: Reuses the warehouse endpoints' proven per-exception `Results.Problem(detail, statusCode, title)`
pattern, so each failure case has a distinct, human-readable message (SC-004). 422 distinguishes "valid
request, token simply not present yet" (retryable by re-pasting) from 4xx input errors; 502 marks the RSI
dependency as the failing party while keeping the token valid for retry (edge case).

## R7 — Observability: keep the token & scraped HTML out of logs (Principle V)

**Decision**: Log the operation, caller id, handle, and outcome only. **Never** log the token value, the
raw RSI response body, or full HTML. Verification token is treated as sensitive auth data.

**Rationale**: Principle V mandates scrubbing sensitive auth data; the token is the verification secret
and the RSI body is large/noisy. Logging handle + outcome is enough for incident diagnosis.

## R8 — Frontend placement & state

**Decision**: A new `features/characters/` folder owns all logic; `ProfilePage` (in `features/dashboard`)
renders a single `<CharacterRegistrationSection />` below the Account card and stays thin. Server state
uses TanStack Query with a `characterQueryKeys` factory; the verify mutation invalidates the character
list on success; the start mutation surfaces the token, and `useRegistration` rehydrates an in-flight
token + countdown on reload. Form uses React Hook Form + Zod. Copy-to-clipboard and the 30-minute
countdown live in `RegistrationTokenCard`.

**Rationale**: Matches Principle VI (feature-owned logic, thin routes), the Frontend Conventions
(TanStack Query for server state, RHF+Zod for forms, centralized typed query keys), and the existing
`features/warehouse` precedent. No nav change is needed since Profile is reached via the account menu.

**Alternatives considered**: Putting registration logic directly in `ProfilePage` (rejected — violates
thin-route / feature-owned-logic rules); a dedicated `/characters` route (rejected — spec says the
section lives on the existing profile page).
