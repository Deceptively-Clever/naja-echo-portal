# Quickstart & Validation: Character Registration & RSI Verification

Run/validate the feature end-to-end. References: [plan.md](./plan.md), [data-model.md](./data-model.md),
[contracts/openapi.yaml](./contracts/openapi.yaml).

## Prerequisites

- .NET SDK (`net10.0`) and Node toolchain installed; PostgreSQL 16 reachable (see `docker-compose.yml`).
- An authenticated session (Discord sign-in) — every `/api/characters` endpoint requires it.
- Outbound network access to `https://robertsspaceindustries.com` for live verification (mocked in tests).

## Setup

```bash
# from repo root
./migrate.sh                              # apply the new AddCharacterRegistration migration
dotnet run --project backend/src/NajaEcho.Api
# in another shell
cd frontend && npm install && npm run dev
```

## Automated tests (write first — TDD, Principle II)

```bash
# Backend
dotnet test backend/tests/NajaEcho.Application.Tests      # StartRegistration + VerifyCharacter handlers (fake IRsiCitizenClient)
dotnet test backend/tests/NajaEcho.Infrastructure.Tests   # Testcontainers: lower(handle) unique + one-pending-per-user
dotnet test backend/tests/NajaEcho.Api.Tests              # status codes + RFC-7807 mapping
# Frontend
cd frontend && npm test                                   # features/characters component/hook tests (MSW)
```

Expected coverage (each starts red): token reuse vs fresh; token-found success **stores the scraped
moniker as the name**; **moniker-missing falls back to the handle**; token-not-found (422); token-expired
(409); duplicate-handle before RSI fetch (409); RSI-not-found (404); RSI-unreachable (502); RSI HTML →
moniker parse (AngleSharp, fixture page); list + empty state; token display, copy, and 30-min countdown.

## Manual end-to-end validation

### Scenario A — Happy path (US1 + US2 + US3, SC-001)
1. Sign in, open **Profile** (`/dashboard/profile`). The **Characters** section shows an empty state.
2. Click **Register Character** → a token appears with copy-to-clipboard and a 30-minute countdown
   (US1.1, FR-001, FR-002).
3. Paste the token into the bio of your RSI citizen profile and save it on robertsspaceindustries.com.
4. Enter your handle (handle only — no name field), click **Verify**.
5. **Expected**: success confirmation; the character appears immediately in the list with its **scraped
   RSI moniker as the name** and the handle you entered — e.g. handle `g8r` → name `G8trdone` (US2.1,
   US3.1, FR-004, FR-008). Total time < 5 min excluding bio editing (SC-001).

### Scenario B — Token reuse (US1.2 / FR-010)
1. With a non-expired token showing, reload the page or click **Register Character** again.
2. **Expected**: the **same** token is shown (no new token generated).

### Scenario C — Error cases (FR-009, SC-004) — each shows a distinct, actionable message
| Action | Expected message | Status |
|--------|------------------|--------|
| Verify without placing the token in the bio | "Token not found on your RSI profile" | 422 |
| Verify after the 30-min window elapses | "Token expired — please start a new registration" | 409 |
| Verify a handle already claimed (any account) | "This handle is already claimed" | 409 |
| Verify a non-existent RSI handle | "RSI citizen profile not found for that handle" | 404 |
| Verify while RSI is unreachable | "Could not reach RSI — please try again shortly" (token stays valid) | 502 |

### Scenario D — Multiple characters (FR-007, SC-005)
Repeat Scenario A with a second handle. **Expected**: both characters list together within one page load.

## Acceptance checklist
- [ ] Token is high-entropy, copyable, and shows a live 30-min countdown.
- [ ] Re-initiating before expiry returns the same token; after expiry, a fresh one.
- [ ] Successful verify creates exactly one `characters` row (name = scraped RSI moniker, handle = input)
      and deletes the pending row; an unparseable moniker falls back to the handle.
- [ ] No two accounts can hold the same handle (case-insensitive) — even under concurrent verifies (SC-002).
- [ ] Every failure case renders a clear message; the token survives an RSI-unreachable failure.
- [ ] No verification token or raw RSI HTML appears in server logs (Principle V).
- [ ] All backend + frontend tests green in CI.
