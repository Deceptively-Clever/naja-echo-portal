# Tasks: Admin Users Page

**Feature**: `017-admin-users-page` | **Branch**: `017-admin-users-page`

**Input**: Design documents from `/specs/017-admin-users-page/`

**Tech Stack**: .NET 10 / ASP.NET Core Minimal APIs + EF Core + PostgreSQL | React 19 + Vite + TanStack Query 5 + React Hook Form + Zod + shadcn/ui

**Tests**: Included — required by constitution (TDD, write failing first per Plan §Testing and Constitution II)

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to
- No story label: Setup / Foundational / Polish phase task

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish directory scaffolding; no new dependencies or migration required.

- [ ] T001 Create directory stubs: `backend/src/NajaEcho.Application/Features/Admin/Users/GetUsers/`, `backend/src/NajaEcho.Application/Features/Admin/Users/AddCharacterForUser/`, `backend/src/NajaEcho.Api/Features/Admin/Users/Contracts/`, and `frontend/src/features/admin/lib/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared DTOs, new exception types, and the `IUserRepository` interface extension that both user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T002 [P] Create `AdminUserDto.cs` (Id, AuthName, Roles[], Characters[]) and `AdminUserCharacterDto.cs` (Id, Name, Handle) in `backend/src/NajaEcho.Application/Features/Admin/Users/GetUsers/`
- [ ] T003 [P] Create `UserNotFoundException.cs` (new 404 failure) in `backend/src/NajaEcho.Application/Features/Admin/Users/AddCharacterForUser/UserNotFoundException.cs`
- [ ] T004 [P] Create `CharacterNameUnavailableException.cs` (new 422 failure — FR-009) in `backend/src/NajaEcho.Application/Features/Admin/Users/AddCharacterForUser/CharacterNameUnavailableException.cs`
- [ ] T005 Add `GetUsersWithRolesAndCharactersAsync()` returning `Task<IReadOnlyList<AdminUserDto>>` to the `IUserRepository` interface in `backend/src/NajaEcho.Application/Abstractions/IUserRepository.cs`

**Checkpoint**: Foundational types in place — user story implementation can begin.

---

## Phase 3: User Story 1 — View All Users with Characters and Roles (Priority: P1) 🎯 MVP

**Goal**: Admin can navigate to `/dashboard/admin/users` and see a table of all members with roles (friendly names) and characters, filterable by auth name / character name / role in real time. Non-admins are denied access.

**Independent Test**: Navigate to `/dashboard/admin/users` as admin and confirm all members appear with correct character and role data; confirm non-admin is redirected and `GET /api/admin/users` returns 403.

### Tests for User Story 1 ⚠️ — Write these FIRST; ensure they FAIL before implementation

- [ ] T006 [P] [US1] Write `GetUsersHandlerTests.cs` — unit tests: returns all members; member with 0 characters/0 roles yields empty arrays (FR-010); roles and characters grouped correctly per member in `backend/tests/NajaEcho.Application.Tests/Features/Admin/Users/GetUsersHandlerTests.cs`
- [ ] T007 [P] [US1] Write Testcontainers integration test for joined users-with-roles-and-characters read (real schema; correct groupings; empty role/character sets) in `backend/tests/NajaEcho.Infrastructure.Tests/Identity/UserRepositoryIntegrationTests.cs`
- [ ] T008 [P] [US1] Write API auth tests: `GET /api/admin/users` returns **403** for non-admin and **401** for unauthenticated (FR-001, SC-005) in `backend/tests/NajaEcho.Api.Tests/Features/Admin/Users/UserAdminEndpointTests.cs`
- [ ] T009 [P] [US1] Write frontend tests in `frontend/src/features/admin/__tests__/adminUsers.test.tsx`: table renders one row per member with auth name, friendly roles (FR-011), character name+handle; empty character/role cells show clear empty state (FR-010); single filter narrows by auth name, character name, and role; zero results show empty-state message (FR-003); non-admin navigating to `/dashboard/admin/users` is redirected (MSW)

### Implementation for User Story 1

- [ ] T010 [P] [US1] Create `GetUsersQuery.cs` (marker query record) in `backend/src/NajaEcho.Application/Features/Admin/Users/GetUsers/GetUsersQuery.cs`
- [ ] T011 [US1] Implement `GetUsersHandler.cs` — dispatches to `IUserRepository.GetUsersWithRolesAndCharactersAsync()` and returns the DTO list in `backend/src/NajaEcho.Application/Features/Admin/Users/GetUsers/GetUsersHandler.cs`
- [ ] T012 [US1] Implement `GetUsersWithRolesAndCharactersAsync` in `UserRepository.cs` using `db.Database.SqlQuery<...>` with `LEFT JOIN AspNetUserRoles/AspNetRoles/characters` on `AspNetUsers`; group by user into `AdminUserDto` (follow `MaterialInventoryRepository` raw-SQL pattern) in `backend/src/NajaEcho.Infrastructure/Identity/UserRepository.cs`
- [ ] T013 [US1] Create `AdminUserListResponse.cs` defining `AdminUserResponse` (Id, AuthName, Roles[], Characters[]) and `AdminUserCharacterResponse` (Id, Name, Handle) in `backend/src/NajaEcho.Api/Features/Admin/Users/Contracts/AdminUserListResponse.cs`
- [ ] T014 [US1] Create `UserAdminEndpoints.cs` — `MapGroup("/api/admin/users").RequireAuthorization(AuthorizationPolicies.Admin)`; wire `GET /` to `GetUsersHandler`; emit Serilog structured log with outcome (mirror `ShipAdminEndpoints`) in `backend/src/NajaEcho.Api/Features/Admin/Users/UserAdminEndpoints.cs`
- [ ] T015 [US1] Register `GetUsersHandler` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs` and call `app.MapUserAdminEndpoints()` in `backend/src/NajaEcho.Api/Program.cs`
- [ ] T016 [P] [US1] Create `roleDisplayNames.ts` — static map `{ Admin: 'Administrator', Quartermaster: 'Quartermaster' }`; unknown roles fall back to raw value (FR-011) in `frontend/src/features/admin/lib/roleDisplayNames.ts`
- [ ] T017 [P] [US1] Create `userSchemas.ts` — Zod schemas hand-derived from `contracts/openapi.yaml`: `adminUserCharacterSchema`, `adminUserSchema`, `adminUserListResponseSchema` in `frontend/src/features/admin/schemas/userSchemas.ts`
- [ ] T018 [P] [US1] Create `userKeys.ts` TanStack Query key factory (`adminUsers.all`, `adminUsers.list()`) in `frontend/src/features/admin/hooks/userKeys.ts`
- [ ] T019 [US1] Create `usersApi.ts` — `getAdminUsers()` apiFetch wrapper for `GET /api/admin/users` in `frontend/src/features/admin/api/usersApi.ts`
- [ ] T020 [US1] Create `useAdminUsers.ts` TanStack Query hook (`useQuery` calling `getAdminUsers`; key from `userKeys`) in `frontend/src/features/admin/hooks/useAdminUsers.ts`
- [ ] T021 [P] [US1] Create `UsersFilter.tsx` — single controlled `<Input>` that emits the filter string; no network call; uses existing shadcn `Input` in `frontend/src/features/admin/components/UsersFilter.tsx`
- [ ] T022 [P] [US1] Create `UsersTable.tsx` — renders shadcn Table with columns (auth name, roles via `roleDisplayNames`, characters name+handle); empty-state cells for zero roles/characters; accepts filtered rows prop in `frontend/src/features/admin/components/UsersTable.tsx`
- [ ] T023 [US1] Create `AdminUsersPage.tsx` — thin route page: fetches via `useAdminUsers`, maintains filter state, applies client-side filter across auth name / character name / role (FR-003), composes `UsersFilter` + `UsersTable`; shows loading/error states; renders a zero-results empty-state message when the filter matches no rows in `frontend/src/features/admin/pages/AdminUsersPage.tsx`
- [ ] T024 [US1] Add Users nav entry `{ label: 'Users', path: '/dashboard/admin/users', icon: Users, access: 'admin', group: 'Admin' }` in `frontend/src/features/dashboard/navigation/navItems.ts`
- [ ] T025 [US1] Add `<Route path="/dashboard/admin/users" element={<AdminUsersPage />} />` under `<AdminRoute>` in `frontend/src/routes/AppRouter.tsx`

**Checkpoint**: User Story 1 fully functional — admin can view the users table with filter; non-admin is blocked.

---

## Phase 4: User Story 2 — Add a Character to a User (Priority: P2)

**Goal**: Admin can open an "Add Character" dialog on any member's row, enter an RSI handle, and have the character scraped and linked to that member — skipping the token flow. All 6 failure modes (blank handle, unknown user, already claimed, RSI not-found, RSI unreachable, name not extractable) surface distinct errors.

**Independent Test**: From the users table, invoke Add Character for a specific member, enter a valid unregistered handle, confirm the character appears in that member's row. Then add the same handle again — confirm 409 blocked with "already claimed" error.

### Tests for User Story 2 ⚠️ — Write these FIRST; ensure they FAIL before implementation

- [ ] T026 [P] [US2] Write `AddCharacterForUserHandlerTests.cs` — unit tests with fake `IRsiCitizenClient` and fake `ICharacterRepository`: happy path creates character linked to target user; blank handle rejected (400); unknown target user → `UserNotFoundException` (404); duplicate handle → `HandleAlreadyClaimedException` (409); RSI not-found → `RsiProfileNotFoundException` (404); RSI unreachable → `RsiUnreachableException` (502); RSI 200 + blank `DisplayName` → `CharacterNameUnavailableException` (422 — FR-009) in `backend/tests/NajaEcho.Application.Tests/Features/Admin/Users/AddCharacterForUserHandlerTests.cs`
- [ ] T027 [P] [US2] Add Testcontainers test: admin insert persists a `Character` row with `OwnerUserId = targetUserId`; second insert of same handle throws due to `ux_characters_handle_lower` unique index in `backend/tests/NajaEcho.Infrastructure.Tests/Identity/UserRepositoryIntegrationTests.cs`
- [ ] T028 [P] [US2] Add API tests for `POST /api/admin/users/{userId}/characters`: **403** non-admin, **401** unauthenticated; **201** success; **400** blank handle; **409** already claimed; **404** RSI not-found; **502** RSI unreachable; **422** name-not-extractable — verify RFC-7807 problem body on each failure in `backend/tests/NajaEcho.Api.Tests/Features/Admin/Users/UserAdminEndpointTests.cs`
- [ ] T029 [P] [US2] Add frontend tests to `frontend/src/features/admin/__tests__/adminUsers.test.tsx`: Add Character dialog — blank handle shows inline error before any fetch (US2 #5); successful add shows new character in member's row (MSW success); duplicate returns "already claimed" message; RSI not-found, unreachable, and no-name each surface their distinct error string

### Implementation for User Story 2

- [ ] T030 [US2] Create `AddCharacterForUserCommand.cs` (record: `TargetUserId` Guid, `Handle` string) in `backend/src/NajaEcho.Application/Features/Admin/Users/AddCharacterForUser/AddCharacterForUserCommand.cs`
- [ ] T031 [US2] Implement `AddCharacterForUserHandler.cs` — validate handle non-empty (400); confirm `IUserRepository.ExistsAsync(TargetUserId)` (→ `UserNotFoundException`); `ICharacterRepository.HandleExistsAsync(handle)` (→ `HandleAlreadyClaimedException`); call `IRsiCitizenClient.FetchCitizenAsync(handle)` mapping results per Decision 3; check `DisplayName` non-blank (→ `CharacterNameUnavailableException`); call `ICharacterRepository.AddAsync`; emit Serilog structured log; return `AdminUserCharacterDto` in `backend/src/NajaEcho.Application/Features/Admin/Users/AddCharacterForUser/AddCharacterForUserHandler.cs`
- [ ] T032 [US2] Create `AddCharacterRequest.cs` (Handle string) in `backend/src/NajaEcho.Api/Features/Admin/Users/Contracts/AddCharacterRequest.cs`
- [ ] T033 [US2] Add `POST /{userId}/characters` to `UserAdminEndpoints.cs` — validate empty handle (400); dispatch `AddCharacterForUserCommand`; map exceptions to RFC-7807 `Results.Problem`: `UserNotFoundException` → 404 with `type: urn:najaecho:error:user-not-found`, `HandleAlreadyClaimedException` → 409, `RsiProfileNotFoundException` → 404 with `type: urn:najaecho:error:rsi-handle-not-found`, `RsiUnreachableException` → 502, `CharacterNameUnavailableException` → 422; emit Serilog log with caller id, target user id, handle, and outcome in `backend/src/NajaEcho.Api/Features/Admin/Users/UserAdminEndpoints.cs`
- [ ] T034 [US2] Register `AddCharacterForUserHandler` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`
- [ ] T035 [US2] Add `addCharacterForUser(userId, handle)` apiFetch wrapper for `POST /api/admin/users/{userId}/characters` to `frontend/src/features/admin/api/usersApi.ts`
- [ ] T036 [US2] Create `useAddCharacterForUser.ts` TanStack Query mutation — on success invalidates `userKeys.adminUsers.list()` to refresh the roster in `frontend/src/features/admin/hooks/useAddCharacterForUser.ts`
- [ ] T037 [US2] Create `AddCharacterDialog.tsx` — shadcn Dialog + React Hook Form + Zod (`handle` non-empty inline validation before submit); calls `useAddCharacterForUser`; maps each API error to its distinct message: 400 blank handle, 409 already-claimed, 404 with `type: urn:najaecho:error:rsi-handle-not-found` → "handle not found", 404 with `type: urn:najaecho:error:user-not-found` → generic not-found fallback, 502 unreachable, 422 no-name (FR-009 specific wording); closes on success in `frontend/src/features/admin/components/AddCharacterDialog.tsx`
- [ ] T038 [US2] Wire `AddCharacterDialog` into `AdminUsersPage.tsx` — Add Character button per row; pass `userId` to dialog; dialog success triggers roster refresh via mutation invalidation in `frontend/src/features/admin/pages/AdminUsersPage.tsx`

**Checkpoint**: User Stories 1 and 2 both fully functional and independently testable.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [ ] T039 [P] Verify Serilog structured logs in `GetUsersHandler` and `AddCharacterForUserHandler` emit caller id, target user id (where applicable), and outcome string (plan Observability §V); also confirm the existing correlation ID middleware propagates a traceable request ID through both `GET /api/admin/users` and `POST /api/admin/users/{userId}/characters` (Constitution V) — document confirmation in PR description
- [ ] T040 Run full backend test suite (`dotnet test backend`) and frontend tests (`npm run test -- adminUsers`); resolve any failures
- [ ] T041 Manual walkthrough per `specs/017-admin-users-page/quickstart.md` — both US1 (view users, filter, non-admin block) and US2 (add character, each error path)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — **blocks all user story phases**
- **User Story 1 (Phase 3)**: Depends on Phase 2 completion — no dependency on US2
- **User Story 2 (Phase 4)**: Depends on Phase 2 completion — depends on US1 (dialog triggered from table; roster invalidated on success)
- **Polish (Phase 5)**: Depends on Phases 3 and 4

### User Story Dependencies

- **US1 (P1)**: Independently testable once Phase 2 is done
- **US2 (P2)**: Depends on Phase 2; integrates with US1 page (dialog is embedded, roster refresh via mutation)

### Within Each User Story

- Tests (T006–T009, T026–T029) MUST be written and **fail** before implementation tasks begin (constitution II)
- Application DTOs / queries before handlers
- Handlers before endpoints
- API contract types before endpoint wiring
- Frontend schemas/keys before hooks, hooks before components, components before page composition
- Backend DI registration after handler implementation

### Parallel Opportunities (Phase 3)

```bash
# Run these tests in parallel — all different files:
T006  GetUsersHandlerTests.cs
T007  UserRepositoryIntegrationTests.cs (US1 portion)
T008  UserAdminEndpointTests.cs (GET auth)
T009  adminUsers.test.tsx (table/filter/redirect)

# After T011+T012 complete, run implementation in parallel:
T016  roleDisplayNames.ts
T017  userSchemas.ts
T018  userKeys.ts
T021  UsersFilter.tsx      # depends on T016
T022  UsersTable.tsx       # depends on T016, T017
```

### Parallel Opportunities (Phase 4)

```bash
# Run these tests in parallel — all different files:
T026  AddCharacterForUserHandlerTests.cs
T027  UserRepositoryIntegrationTests.cs (US2 portion)
T028  UserAdminEndpointTests.cs (POST auth + errors)
T029  adminUsers.test.tsx (dialog tests)

# After T035, run in parallel:
T036  useAddCharacterForUser.ts    # depends on T035
T037  AddCharacterDialog.tsx       # depends on T036
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 (tests first → implementation)
4. **STOP and VALIDATE**: Run `dotnet test backend/tests/NajaEcho.Application.Tests` + `npm run test -- adminUsers`; do manual US1 walkthrough
5. Demo / deploy if ready

### Incremental Delivery

1. Setup + Foundational → shared types in place
2. User Story 1 → admin can view the roster and filter — MVP shipped
3. User Story 2 → admin can add characters without database access (SC-006)
4. Polish → observability verified, full test suite green

---

## Notes

- No new backend or frontend packages (Decision 8)
- No EF Core migration (Decision 1) — all tables exist from features 002 and 015
- Reused exceptions: `HandleAlreadyClaimedException`, `RsiProfileNotFoundException`, `RsiUnreachableException` from `Features/Characters/VerifyCharacter/`
- New exceptions: `UserNotFoundException` (T003), `CharacterNameUnavailableException` (T004)
- Friendly role names are **frontend-only** (Decision 5) — API returns raw role strings
- Filtering is **client-side** (Decision 6) — no server-side search endpoint
- `MapUserAdminEndpoints()` in `Program.cs` (T015) covers both GET (US1) and POST (US2); T033 extends the same endpoint class
