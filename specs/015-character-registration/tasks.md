# Tasks: Character Registration & RSI Verification

**Input**: Design documents from `/specs/015-character-registration/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/openapi.yaml ✅, quickstart.md ✅

**TDD**: All test tasks MUST be written and confirmed failing before the implementation tasks in the same phase begin (Constitution Principle II, plan.md).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no incomplete-task dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in every task description

---

## Phase 1: Setup

**Purpose**: Install the one new backend dependency required before any implementation begins.

- [X] T001 Add AngleSharp NuGet package to `backend/src/NajaEcho.Infrastructure/NajaEcho.Infrastructure.csproj`; confirm MIT licence compatibility and record the security posture review as complete in the PR description per constitution Development Workflow

**Checkpoint**: `dotnet restore` succeeds and AngleSharp is resolvable.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, Application ports, EF Core configuration, and the migration — required by every user story.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 Create `Character.cs` entity in `backend/src/NajaEcho.Domain/Characters/Character.cs` (Id, OwnerUserId, Name, Handle, CreatedAt — matching data-model.md)
- [X] T003 Create `PendingCharacterRegistration.cs` entity in `backend/src/NajaEcho.Domain/Characters/PendingCharacterRegistration.cs` with `static Create(Guid ownerUserId, DateTimeOffset now)` factory (generates high-entropy `naja-` prefixed token via `RandomNumberGenerator`, sets `ExpiresAt = now + ValidityWindow`; `ValidityWindow` constant = 30 min) and `bool IsExpired(DateTimeOffset now)` method (research R2, R3)
- [X] T004 [P] Create `ICharacterRepository.cs` in `backend/src/NajaEcho.Application/Abstractions/ICharacterRepository.cs` (methods: `Task<bool> HandleExistsAsync(string handle)` case-insensitive, `Task AddAsync(Character character)`, `Task<IReadOnlyList<Character>> GetByOwnerAsync(Guid ownerUserId)`)
- [X] T005 [P] Create `IPendingRegistrationRepository.cs` in `backend/src/NajaEcho.Application/Abstractions/IPendingRegistrationRepository.cs` (methods: `Task<PendingCharacterRegistration?> GetByOwnerAsync(Guid ownerUserId)`, `Task UpsertAsync(PendingCharacterRegistration pending)`, `Task RemoveByOwnerAsync(Guid ownerUserId)`)
- [X] T006 [P] Create `IRsiCitizenClient.cs` in `backend/src/NajaEcho.Application/Abstractions/IRsiCitizenClient.cs` — port returning a discriminated outcome: `RsiCitizenPage { string Content, string? DisplayName }` on 200, `RsiProfileNotFound` on 404, `RsiUnreachable` on timeout/5xx/network error (research R4); include the outcome record/class definitions in the same file or a nearby `RsiCitizenClientOutcome.cs`
- [X] T007 Create `CharacterConfiguration.cs` in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/CharacterConfiguration.cs` — table `characters`, snake_case columns, `ux_characters_handle_lower` unique index on `lower(handle)` (raw SQL via `HasDatabaseName`/`HasFilter`), `ix_characters_owner_user_id` non-unique index, FK to `AspNetUsers` with `OnDelete Cascade`
- [X] T008 Create `PendingCharacterRegistrationConfiguration.cs` in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/PendingCharacterRegistrationConfiguration.cs` — table `pending_character_registrations`, snake_case columns, `ux_pending_character_registrations_owner_user_id` unique index on `owner_user_id`, FK to `AspNetUsers` with `OnDelete Cascade`
- [X] T009 Add `DbSet<Character>` and `DbSet<PendingCharacterRegistration>` to `AppDbContext` in `backend/src/NajaEcho.Infrastructure/Persistence/AppDbContext.cs`
- [X] T010 Generate EF Core migration `AddCharacterRegistration` via `dotnet ef migrations add AddCharacterRegistration` and verify the migration SQL creates both tables, the `lower(handle)` functional unique index, and the one-per-owner unique index

**Checkpoint**: `./migrate.sh` applies cleanly; `\d characters` and `\d pending_character_registrations` in psql show both tables with correct columns and indexes.

---

## Phase 3: User Story 1 — Start Character Registration (Priority: P1) 🎯 MVP

**Goal**: Authenticated member clicks "Register Character", receives a unique verification token with copy-to-clipboard and a 30-minute countdown. Re-initiating before expiry returns the same token; after expiry, a fresh one.

**Independent Test**: Click "Register Character" on the Profile page — confirm a token appears with copy and countdown. Reload — confirm the same token is shown. Wait for expiry (or adjust `ValidityWindow` temporarily) — confirm a fresh token is issued.

### Tests for User Story 1 ⚠️ Write FIRST — confirm FAIL before implementing

- [X] T011 [P] [US1] Write failing unit tests for `StartRegistrationHandler` in `backend/tests/NajaEcho.Application.Tests/Features/Characters/StartRegistrationHandlerTests.cs` — cover: (a) no existing pending → creates fresh token; (b) non-expired pending exists → returns same token (FR-010 / US1.2); (c) expired pending exists → generates fresh token (US1.3)
- [X] T012 [P] [US1] Write failing unit tests for `GetRegistrationHandler` in `backend/tests/NajaEcho.Application.Tests/Features/Characters/GetRegistrationHandlerTests.cs` — cover: (a) non-expired pending → returns `PendingRegistrationDto`; (b) expired pending → returns null; (c) no pending → returns null
- [X] T013 [P] [US1] Create `backend/tests/NajaEcho.Api.Tests/Features/Characters/CharacterEndpointTests.cs` and write failing endpoint tests for `POST /api/characters/registration` (200 with `PendingRegistrationResponse` shape) and `GET /api/characters/registration` (200 with token or null) — fake `IPendingRegistrationRepository`, assert status codes and response structure
- [X] T014 [P] [US1] Write failing frontend tests in `frontend/src/features/characters/__tests__/RegistrationTokenCard.test.tsx` — cover: token value rendered; copy-to-clipboard button present; countdown displays remaining time from `expiresAt`

### Implementation for User Story 1

- [X] T015 [US1] Implement `StartRegistration` command + `StartRegistrationHandler` in `backend/src/NajaEcho.Application/Features/Characters/StartRegistration/` (reuse-or-create logic per data-model.md lifecycle; return `PendingRegistrationDto { Token, ExpiresAt }`)
- [X] T016 [US1] Implement `GetRegistration` query + `GetRegistrationHandler` + `PendingRegistrationDto` in `backend/src/NajaEcho.Application/Features/Characters/GetRegistration/` (return null when none or expired)
- [X] T017 [US1] Implement `PendingRegistrationRepository` in `backend/src/NajaEcho.Infrastructure/Characters/PendingRegistrationRepository.cs` — `GetByOwnerAsync` (LINQ), `UpsertAsync` (AddOrUpdate by owner_user_id unique index), `RemoveByOwnerAsync`
- [X] T018 [US1] Create `CharacterEndpoints.cs` in `backend/src/NajaEcho.Api/Features/Characters/CharacterEndpoints.cs` — map group `/api/characters` with `.RequireAuthorization()`; add `GET /registration` and `POST /registration` handlers (resolve owner via `TryGetUserId`, dispatch queries/commands, Serilog log per handler, `Results.Problem` on exception); create `CharacterDtos.cs` with `StartRegistrationResponse` and `PendingRegistrationResponse` records
- [X] T019 [US1] Register `IPendingRegistrationRepository → PendingRegistrationRepository` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`; call `app.MapCharacterEndpoints()` in API composition (edit `backend/src/NajaEcho.Api/Program.cs` or endpoint wiring file alongside existing groups)
- [X] T020 [P] [US1] Create `frontend/src/features/characters/api/charactersApi.ts` — `apiFetch` wrappers for `startRegistration()` (POST /api/characters/registration) and `getRegistration()` (GET /api/characters/registration)
- [X] T021 [P] [US1] Create `frontend/src/features/characters/hooks/characterQueryKeys.ts` (typed key factory); create `useRegistration.ts` (TanStack Query hook — GET pending token, used for countdown rehydrate on page load); create `useStartRegistration.ts` (mutation hook — POSTs to start/resume registration, stores returned token)
- [X] T022 [P] [US1] Create `frontend/src/features/characters/schemas/characterSchemas.ts` — Zod schemas for `PendingRegistrationResponse` (`token: z.string()`, `expiresAt: z.string().datetime()`)
- [X] T023 [US1] Create `frontend/src/features/characters/components/RegistrationTokenCard.tsx` — renders token value, copy-to-clipboard button, live 30-minute countdown (derived from `expiresAt`); uses existing `card`, `button`, `badge` shadcn/ui primitives
- [X] T024 [US1] Create `frontend/src/features/characters/components/CharacterRegistrationSection.tsx` — top-level section component embedded into ProfilePage; renders "Register Character" button → calls `useStartRegistration`; shows `<RegistrationTokenCard />` when token is present; rehydrates in-flight token on load via `useRegistration`
- [X] T025 [US1] Render `<CharacterRegistrationSection />` below the Account card in `frontend/src/features/dashboard/pages/ProfilePage.tsx`

**Checkpoint**: US1 tests green. Profile page shows token with countdown after clicking "Register Character". Reload returns same token. T011–T014 suites all pass.

---

## Phase 4: User Story 2 — Verify RSI Handle Ownership (Priority: P1)

**Goal**: Member enters their RSI handle and clicks "Verify". Backend fetches the RSI page, finds the token, creates the character (name = scraped Community Moniker, fallback to handle), deletes the pending row. All error cases surface distinct, actionable messages (422, 409×2, 404, 502).

**Independent Test**: Place the token in an RSI bio, submit the handle, confirm the character appears in the list. Test each error scenario per quickstart.md Scenario C.

### Tests for User Story 2 ⚠️ Write FIRST — confirm FAIL before implementing

- [X] T026 [P] [US2] Write failing unit tests for `VerifyCharacterHandler` in `backend/tests/NajaEcho.Application.Tests/Features/Characters/VerifyCharacterHandlerTests.cs` using fake `IRsiCitizenClient` and fake repos — cover: (a) success: token found in a realistic RSI HTML fixture file (add a minimal static HTML file to the test project representing a known RSI citizen page layout, with the heading selector used to parse the Community Moniker); character created, name = scraped moniker, pending removed; (b) moniker absent in fixture → name falls back to handle; (c) token expired / no pending → `TokenExpiredException`; (d) duplicate handle (before RSI fetch) → `HandleAlreadyClaimedException`; (e) RSI 404 → `RsiProfileNotFoundException`; (f) RSI unreachable → `RsiUnreachableException`; (g) token not found in page content → `TokenNotFoundException`
- [X] T027 [P] [US2] Write failing Testcontainers integration tests in `backend/tests/NajaEcho.Infrastructure.Tests/Characters/CharacterRegistrationIntegrationTests.cs` — cover: (a) inserting two characters with same handle (different casing) violates `ux_characters_handle_lower`; (b) inserting a second pending row for the same owner violates `ux_pending_character_registrations_owner_user_id`; (c) happy-path add + list returns the character; (d) same owner adds two characters with distinct handles and `GetByOwnerAsync` returns both — directly asserting FR-007 multi-character support
- [X] T028 [P] [US2] Add failing endpoint tests to `backend/tests/NajaEcho.Api.Tests/Features/Characters/CharacterEndpointTests.cs` for `POST /api/characters/verify` — assert: 201 + `CharacterResponse` shape on success; 422 on `TokenNotFoundException`; 409 on `TokenExpiredException`; 409 on `HandleAlreadyClaimedException`; 404 on `RsiProfileNotFoundException`; 502 on `RsiUnreachableException`; 400 on empty handle; all problem responses are RFC-7807 `application/problem+json`
- [X] T029 [P] [US2] Write failing frontend tests in `frontend/src/features/characters/__tests__/VerifyCharacterForm.test.tsx` (MSW) — cover: (a) submit success → success message shown; (b) 422 → "Token not found on your RSI profile" displayed; (c) 409 token-expired → "Token expired — please start a new registration"; (d) 409 handle-claimed → "This handle is already claimed"; (e) 404 → "RSI citizen profile not found for that handle"; (f) 502 → "Could not reach RSI — please try again shortly"

### Implementation for User Story 2

- [X] T030 [US2] Create domain exception classes in `backend/src/NajaEcho.Application/Features/Characters/VerifyCharacter/`: `TokenExpiredException.cs`, `TokenNotFoundException.cs`, `HandleAlreadyClaimedException.cs`, `RsiProfileNotFoundException.cs`, `RsiUnreachableException.cs`
- [X] T031 [US2] Implement `VerifyCharacter` command + `VerifyCharacterHandler` in `backend/src/NajaEcho.Application/Features/Characters/VerifyCharacter/` — execution order: (1) get pending, throw `TokenExpiredException` if absent/expired; (2) `HandleExistsAsync(handle)`, throw `HandleAlreadyClaimedException` if true; (3) call `IRsiCitizenClient` — map outcomes to exceptions; (4) substring token scan on `Content`, throw `TokenNotFoundException` if absent; (5) name = `DisplayName ?? handle`; (6) `AddAsync(Character{...})`; (7) `RemoveByOwnerAsync(ownerId)`; return `CharacterDto`
- [X] T032 [US2] Implement `RsiCitizenClient` in `backend/src/NajaEcho.Infrastructure/Characters/RsiCitizenClient.cs` — `HttpClient` fetch of `/en/citizens/{handle}`: 200 → AngleSharp parse Community Moniker from primary heading selector, return `RsiCitizenPage { Content = response text, DisplayName = parsed moniker }`; 404 → `RsiProfileNotFound`; timeout/5xx/network error → `RsiUnreachable`; bounded timeout ≈ 10 s
- [X] T033 [US2] Implement `CharacterRepository` in `backend/src/NajaEcho.Infrastructure/Characters/CharacterRepository.cs` — `HandleExistsAsync`: `db.Characters.AnyAsync(c => c.Handle.ToLower() == handle.ToLower())`; `AddAsync`: `db.Characters.Add` + `SaveChangesAsync` (map `DbUpdateException` with unique-constraint violation to `HandleAlreadyClaimedException`); `GetByOwnerAsync`: LINQ filter + `ToListAsync`
- [X] T034 [US2] Add `POST /api/characters/verify` handler to `CharacterEndpoints.cs` in `backend/src/NajaEcho.Api/Features/Characters/CharacterEndpoints.cs` — accept `VerifyCharacterRequest { string Handle }`, validate non-empty (400 on fail), dispatch `VerifyCharacterHandler`, per-exception `catch` blocks returning `Results.Problem(detail, statusCode: ..., title: ...)` per research R6 mapping; add `VerifyCharacterRequest` and `CharacterResponse` records to `CharacterDtos.cs`
- [X] T035 [US2] Register `ICharacterRepository → CharacterRepository` and `IRsiCitizenClient → RsiCitizenClient` (via `AddHttpClient<IRsiCitizenClient, RsiCitizenClient>(client => { client.BaseAddress = new Uri("https://robertsspaceindustries.com/"); client.Timeout = TimeSpan.FromSeconds(10); })`) in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs` *(depends on T019 complete — both tasks edit this file; do not run in parallel)*
- [X] T036 [P] [US2] Add `VerifyCharacterRequest` Zod schema (`handle: z.string().min(1).max(100)`) and `CharacterResponse` schema (`id`, `name`, `handle`, `createdAt`) to `frontend/src/features/characters/schemas/characterSchemas.ts`
- [X] T037 [P] [US2] Add `verifyCharacter(handle: string)` API wrapper (POST /api/characters/verify) to `frontend/src/features/characters/api/charactersApi.ts`
- [X] T038 [P] [US2] Create `useVerifyCharacter.ts` mutation hook in `frontend/src/features/characters/hooks/useVerifyCharacter.ts` — on success: invalidates character list query key, surfaces success state; maps API error `title`/`detail` to display message
- [X] T039 [US2] Create `VerifyCharacterForm.tsx` in `frontend/src/features/characters/components/VerifyCharacterForm.tsx` — React Hook Form + Zod; handle field only (no name field — plan constraint); calls `useVerifyCharacter`; renders distinct error message per failure case (FR-009); shows disabled/loading states during submission
- [X] T040 [US2] Integrate `<VerifyCharacterForm />` into `CharacterRegistrationSection.tsx` in `frontend/src/features/characters/components/CharacterRegistrationSection.tsx` — shown below `<RegistrationTokenCard />` when a token is active

**Checkpoint**: US2 tests green (T026–T029 suites pass). Verify flow completes end-to-end: token on RSI bio → submit handle → character created with scraped moniker. All error scenarios display correct messages.

---

## Phase 5: User Story 3 — View Registered Characters (Priority: P2)

**Goal**: Member sees all verified characters listed on the Profile page with name and handle. Empty state shown when none are registered.

**Independent Test**: Register one or more characters; reload Profile page and confirm all appear with correct name and handle. A fresh account shows the empty state.

### Tests for User Story 3 ⚠️ Write FIRST — confirm FAIL before implementing

- [X] T041 [P] [US3] Write failing unit tests for `GetCharactersHandler` in `backend/tests/NajaEcho.Application.Tests/Features/Characters/GetCharactersHandlerTests.cs` — cover: (a) owner with two characters returns both `CharacterDto`s in the list; (b) owner with no characters returns empty list
- [X] T042 [P] [US3] Add failing endpoint test to `backend/tests/NajaEcho.Api.Tests/Features/Characters/CharacterEndpointTests.cs` for `GET /api/characters` — assert 200 + `CharacterListResponse { characters: [...] }` shape; assert 401 when unauthenticated
- [X] T043 [P] [US3] Write failing frontend tests in `frontend/src/features/characters/__tests__/CharacterList.test.tsx` (MSW) — cover: (a) list of characters renders each with name and handle; (b) empty array → empty state prompt renders

### Implementation for User Story 3

- [X] T044 [US3] Implement `GetCharacters` query + `GetCharactersHandler` + `CharacterDto { Id, Name, Handle, CreatedAt }` in `backend/src/NajaEcho.Application/Features/Characters/GetCharacters/` — calls `ICharacterRepository.GetByOwnerAsync(ownerId)`, maps to list of `CharacterDto`
- [X] T045 [US3] Add ascending `created_at` ordering to `GetByOwnerAsync` in `backend/src/NajaEcho.Infrastructure/Characters/CharacterRepository.cs` — ensure the LINQ query includes `.OrderBy(c => c.CreatedAt)` so characters display in registration order (US3 list display, FR-008)
- [X] T046 [US3] Add `GET /api/characters` handler to `CharacterEndpoints.cs` in `backend/src/NajaEcho.Api/Features/Characters/CharacterEndpoints.cs` — resolve owner, dispatch `GetCharactersHandler`, return 200 `CharacterListResponse`; add `CharacterListResponse` record to `CharacterDtos.cs`
- [X] T047 [P] [US3] Add `getCharacters()` API wrapper (GET /api/characters) to `frontend/src/features/characters/api/charactersApi.ts`; add `CharacterListResponse` Zod schema to `frontend/src/features/characters/schemas/characterSchemas.ts`
- [X] T048 [P] [US3] Create `useCharacters.ts` TanStack Query hook in `frontend/src/features/characters/hooks/useCharacters.ts` — query key from `characterQueryKeys`; fetches and returns character list
- [X] T049 [US3] Create `CharacterList.tsx` in `frontend/src/features/characters/components/CharacterList.tsx` — renders each character's name and handle using `card` and `badge` primitives; empty state with prompt to register first character (US3.2)
- [X] T050 [US3] Integrate `<CharacterList />` into `CharacterRegistrationSection.tsx` in `frontend/src/features/characters/components/CharacterRegistrationSection.tsx` — renders above the registration form; list is refreshed automatically when `useVerifyCharacter` mutation succeeds (via query invalidation in T038)

**Checkpoint**: US3 tests green (T041–T043 suites pass). Profile page lists all verified characters. Empty state visible for new accounts. All prior user stories still work.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Full-suite validation, observability verification, and quickstart sign-off.

- [X] T051 [P] Run full backend test suite (`dotnet test backend/tests/NajaEcho.Application.Tests`, `dotnet test backend/tests/NajaEcho.Infrastructure.Tests`, `dotnet test backend/tests/NajaEcho.Api.Tests`) and confirm all green; fix any failures
- [X] T052 [P] Run full frontend test suite (`cd frontend && npm test`) and confirm all green; fix any failures
- [X] T053 Validate that no verification token value or RSI response HTML appears in server logs during a successful verify and during each error scenario — inspect Serilog structured output against research R7 (Principle V)
- [X] T054 Run quickstart.md Scenarios A–D end-to-end: happy path (scraped moniker as name), token reuse, all error cases (Scenario C table), and multiple-character listing (Scenario D); tick off quickstart.md acceptance checklist

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — blocks all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — no dependency on US2 or US3
- **US2 (Phase 4)**: Depends on Phase 2 — no dependency on US1 or US3 (CharacterEndpoints.cs is extended, not replaced)
- **US3 (Phase 5)**: Depends on Phase 2 — CharacterRepository.GetByOwnerAsync implemented in US2 (T033), so US3 should follow US2 in practice
- **Polish (Phase 6)**: Depends on all user story phases

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational — independently testable
- **US2 (P1)**: Can start after Foundational — depends on `CharacterRepository` stub from T033; test-write tasks (T026–T029) can overlap with US1 implementation
- **US3 (P2)**: Can start after US2 completes T033 (`CharacterRepository.GetByOwnerAsync`) — or implement T045 as a no-op stub first if parallelizing

### Within Each User Story

- Test tasks [P] MUST be written and FAIL before implementation tasks begin (Constitution Principle II)
- Domain exceptions (T030) before VerifyCharacterHandler (T031)
- Repositories before handlers that use them
- Handlers before endpoints
- API wrappers and schemas [P] before hooks
- Hooks before components

---

## Parallel Example: User Story 2

```bash
# Write all US2 test tasks simultaneously (different files):
Task T026: VerifyCharacterHandlerTests.cs
Task T027: CharacterRegistrationIntegrationTests.cs
Task T028: CharacterEndpointTests.cs (additions)
Task T029: VerifyCharacterForm.test.tsx

# After tests are failing — implement backend in order:
Task T030 → T031 → T032 → T033 → T034 → T035

# Frontend tasks can run in parallel with backend (different files):
Task T036: characterSchemas.ts (additions)   ┐
Task T037: charactersApi.ts (additions)      ├ parallel
Task T038: useVerifyCharacter.ts             ┘
# Then:
Task T039 → T040
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002–T010)
3. Complete Phase 3: User Story 1 (T011–T025)
4. **STOP and VALIDATE**: Token displays, copies, counts down; token reuse works; US1 tests all green
5. Demo / deploy if ready

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. Add US1 → test independently (token display + countdown) → MVP demo
3. Add US2 → test independently (verify flow + all error cases) → usable feature
4. Add US3 → test independently (character list + empty state) → feature complete
5. Polish → quickstart sign-off → PR ready

---

## Notes

- `[P]` tasks touch different files with no dependency on incomplete tasks in the same phase
- Every user story is independently completable and testable
- Verification token must never appear in logs — check every Serilog call site
- `lower(handle)` unique index is the race guard for SC-002; the application pre-check in T031 is for clean error messaging
- `CharacterEndpoints.cs` grows across US1, US2, and US3 — implement handlers sequentially within each phase
- `DependencyInjection.cs` is edited in T019 (US1) and T035 (US2) — sequence within phases, not in parallel
