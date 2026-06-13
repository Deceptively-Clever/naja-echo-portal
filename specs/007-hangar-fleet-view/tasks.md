---

description: "Task list for Hangar feature (007-hangar-fleet-view)"
---

# Tasks: Hangar

**Input**: Design documents from `/specs/007-hangar-fleet-view/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/openapi.yaml ✅, quickstart.md ✅

**Tests**: Included — constitution principle II (Test-First / TDD) is NON-NEGOTIABLE for this project. All test tasks must be written and confirmed failing before their paired implementation tasks.

**Organization**: Tasks are grouped by user story (US1–US4) to enable independent implementation and validation of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no blocking dependencies on incomplete tasks)
- **[Story]**: Maps to user story from spec.md ([US1]–[US4])
- All test tasks MUST be written and failing before the paired implementation tasks run

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Wire the generation script, nav item, and folder scaffolding that all user stories share.

- [X] T001 Add `"gen:api:hangar"` openapi-typescript script to `frontend/package.json` pointing to `../specs/007-hangar-fleet-view/contracts/openapi.yaml -o src/lib/api/hangar.d.ts`
- [X] T002 [P] Add Hangar nav item (label, path `/hangar`, icon, group, access) to `frontend/src/features/dashboard/navigation/navItems.ts`
- [X] T003 [P] Scaffold empty feature folder structure at `frontend/src/features/hangar/` (pages/, components/, hooks/, schemas/, api/, __tests__/)
- [X] T004 [P] Scaffold empty backend feature folders at `backend/src/NajaEcho.Application/Features/Hangar/` (GetMyHangar/, GetOrgHangar/, SearchCatalogShips/, GetOwningMembers/, AddShipToHangar/, RemoveShipFromHangar/) and `backend/src/NajaEcho.Api/Features/Hangar/Contracts/`

**Checkpoint**: Folder structure in place; nav item appears in sidebar; generation script registered.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entity, EF configuration, DbSet registration, repository interface, and migration — must all exist before any query/command handler can be implemented.

**⚠️ CRITICAL**: No user story implementation can begin until this phase is complete.

- [X] T005 Create `HangarEntry` domain entity (`Id: Guid`, `UserId: Guid`, `ShipId: Guid`, `AddedAt: DateTimeOffset`) in `backend/src/NajaEcho.Domain/Hangar/HangarEntry.cs`
- [X] T006 Create `HangarEntryConfiguration : IEntityTypeConfiguration<HangarEntry>` (schema `sc`, table `hangar_entries`, unique index `ux_hangar_entries_user_ship` on `(user_id, ship_id)`, FK `ship_id → sc.ships(id)` restrict, FK `user_id → AspNetUsers(id)` cascade, indexes on `ship_id` and `user_id`) in `backend/src/NajaEcho.Infrastructure/Persistence/Configurations/HangarEntryConfiguration.cs`
- [X] T007 Register `DbSet<HangarEntry> HangarEntries` and apply `HangarEntryConfiguration` in `backend/src/NajaEcho.Infrastructure/Persistence/AppDbContext.cs`
- [X] T008 [P] Define `IHangarRepository` interface (methods: `GetMyHangarAsync`, `GetOrgHangarAsync`, `GetOwningMembersAsync`, `SearchCatalogAsync`, `AddAsync`, `RemoveAsync`) in `backend/src/NajaEcho.Application/Abstractions/IHangarRepository.cs`
- [X] T009 Create stub `HangarRepository : IHangarRepository` (all methods throw `NotImplementedException`) in `backend/src/NajaEcho.Infrastructure/Hangar/HangarRepository.cs`
- [X] T010 [P] Register `IHangarRepository` → `HangarRepository` in DI in `backend/src/NajaEcho.Api/Program.cs` (or the relevant service registration file)
- [X] T011 Generate EF migration `AddHangarEntries` (`dotnet ef migrations add AddHangarEntries`) from `backend/src/NajaEcho.Api` — verify the generated migration creates `sc.hangar_entries` with correct columns, FKs, and unique index

**Checkpoint**: `dotnet build` passes; migration file exists; `IHangarRepository` is wired in DI.

---

## Phase 3: User Story 1 — Browse My Hangar (Priority: P1) 🎯 MVP

**Goal**: Authenticated member navigates to Hangar and sees their own ships as visual cards with image backgrounds, name search, and empty state.

**Independent Test**: Navigate to `/hangar`; confirm My Hangar loads by default; own ships appear as cards with name top-left; search filters correctly; empty state shows when hangar is empty. No owner counts visible.

### Tests for User Story 1 — Write First, Verify Failing

> **NOTE: These tests MUST be written and confirmed failing before any US1 implementation tasks run.**

- [X] T012 [P] [US1] Write failing Application unit tests for `GetMyHangar` handler (ShipCard mapping, jsonb field extraction, name filter, paging) in `backend/tests/NajaEcho.Application.Tests/Features/Hangar/GetMyHangarHandlerTests.cs`
- [X] T013 [P] [US1] Write failing Infrastructure unit tests for `HangarRepository.GetMyHangarAsync` (verifies correct EF/SQL projection of `raw_data->>'url_photo'`, `raw_data->>'scu'`, `raw_data->>'crew'`, paged results, active-ship join) in `backend/tests/NajaEcho.Infrastructure.Tests/Hangar/HangarRepositoryGetMyHangarTests.cs`
- [X] T014 [P] [US1] Write failing API integration tests (Testcontainers) for `GET /api/hangar/mine` (200 with paged cards, 401 unauthenticated, search param filters by name) in `backend/tests/NajaEcho.Api.Tests/Features/Hangar/GetMyHangarEndpointTests.cs`

### Implementation for User Story 1

- [X] T015 [P] [US1] Create `ShipCard` read model + `GetMyHangarQuery` + `GetMyHangarHandler` in `backend/src/NajaEcho.Application/Features/Hangar/GetMyHangar/` (handler calls `IHangarRepository.GetMyHangarAsync`, returns `PagedResult<ShipCard>`)
- [X] T016 [US1] Implement `HangarRepository.GetMyHangarAsync` in `backend/src/NajaEcho.Infrastructure/Hangar/HangarRepository.cs` — EF/FromSql projection joining `sc.hangar_entries` ⨝ `sc.ships`, extracting `raw_data->>'url_photo'`, `(raw_data->>'scu')::numeric`, `raw_data->>'crew'`; name filter `ILIKE`; paged; only where `user_id = currentUserId`
- [X] T017 [US1] Add `GET /api/hangar/mine` endpoint to `backend/src/NajaEcho.Api/Features/Hangar/HangarEndpoints.cs`; add `PagedHangarShipCardsResponse` and `HangarShipCardDto` DTOs in `backend/src/NajaEcho.Api/Features/Hangar/Contracts/`; emit Serilog structured log per request; map 401
- [X] T018 [US1] Run `npm run gen:api:hangar` from `frontend/` to generate `frontend/src/lib/api/hangar.d.ts` (types from `contracts/openapi.yaml`)
- [X] T019 [P] [US1] Create `HangarShipCard` Zod schema (view-model) in `frontend/src/features/hangar/schemas/hangarShipCard.ts`
- [X] T020 [US1] Create hangar API client function `getMyHangar(params)` using generated types in `frontend/src/features/hangar/api/hangarApi.ts`
- [X] T021 [US1] Create `useMyHangar(search)` TanStack Query hook (`useQuery` for now; upgraded to `useInfiniteQuery` in US4) in `frontend/src/features/hangar/hooks/useMyHangar.ts`
- [X] T022 [P] [US1] Create `ShipCard` component (CSS background-image from `urlPhoto`; `onError` falls back to default bg; readability scrim overlay; ship name top-left) in `frontend/src/features/hangar/components/ShipCard.tsx`
- [X] T023 [US1] Create `ShipCardGallery` component (responsive grid, debounced search bar, empty-state message, renders `ShipCard[]`) in `frontend/src/features/hangar/components/ShipCardGallery.tsx`
- [X] T024 [US1] Create `MyHangarView` page (wraps `ShipCardGallery`, passes `search` state, renders Add Ship button placeholder, "add your first ship" empty state) in `frontend/src/features/hangar/pages/MyHangarView.tsx`
- [X] T025 [US1] Create `HangarPage` tab container (My Hangar / Org Hangar tabs, defaults to My Hangar) in `frontend/src/features/hangar/pages/HangarPage.tsx`
- [X] T026 [US1] Wire `/hangar` route pointing to `HangarPage` in `frontend/src/routes/AppRouter.tsx`
- [X] T027 [P] [US1] Write frontend component and hook tests for `ShipCard` (image bg, fallback on error, name rendered), `ShipCardGallery` (search filter, empty state), and `useMyHangar` hook with MSW in `frontend/src/features/hangar/__tests__/MyHangar.test.tsx`

**Checkpoint**: Navigate to `/hangar` — My Hangar renders own ships as cards; search bar filters by name within 500ms; empty state shown for empty hangar; no pagination controls visible.

---

## Phase 4: User Story 2 — Add and Remove Ships (Priority: P2)

**Goal**: Member can add ships from the catalog (with duplicate prevention and `alreadyOwned` marking) and remove ships from their hangar via hover overlay, with confirm prompt.

**Independent Test**: Open Add Ship dialog; search catalog; add an un-owned ship; confirm it appears in My Hangar. Attempt to add the same ship again — it is disabled/marked as already owned. Hover a card; remove the ship; confirm it disappears. Test 409 response.

### Tests for User Story 2 — Write First, Verify Failing

- [X] T028 [P] [US2] Write failing Application unit tests for `AddShipToHangar` (validator rejects missing `shipId`, handler returns 409 on duplicate, handler returns 404 for inactive/missing catalog ship) in `backend/tests/NajaEcho.Application.Tests/Features/Hangar/AddShipToHangarHandlerTests.cs`
- [X] T029 [P] [US2] Write failing Application unit tests for `SearchCatalogShips` (results only include `Active` ships, `alreadyOwned` flag is set correctly for current member) in `backend/tests/NajaEcho.Application.Tests/Features/Hangar/SearchCatalogShipsHandlerTests.cs`
- [X] T030 [P] [US2] Write failing Application unit tests for `RemoveShipFromHangar` (removes existing entry, is idempotent for non-existent entry) in `backend/tests/NajaEcho.Application.Tests/Features/Hangar/RemoveShipFromHangarHandlerTests.cs`
- [X] T031 [P] [US2] Write failing API integration tests (Testcontainers) for `POST /api/hangar/mine` (201 created, 409 duplicate, 404 inactive ship, 401), `DELETE /api/hangar/mine/{shipId}` (204, 401), and `GET /api/hangar/catalog/search` (200 paged, `alreadyOwned` flag, only Active ships, 401) in `backend/tests/NajaEcho.Api.Tests/Features/Hangar/AddRemoveShipEndpointTests.cs`

### Implementation for User Story 2

- [X] T032 [P] [US2] Create `CatalogSearchRow` read model + `SearchCatalogShipsQuery` + `SearchCatalogShipsHandler` in `backend/src/NajaEcho.Application/Features/Hangar/SearchCatalogShips/` (Active ships only; `alreadyOwned` = current user owns it; paged)
- [X] T033 [P] [US2] Create `AddShipToHangarCommand` + `AddShipToHangarHandler` + `AddShipToHangarValidator` (FluentValidation; guard: 409 if `(userId, shipId)` already exists; 404 if ship not Active) in `backend/src/NajaEcho.Application/Features/Hangar/AddShipToHangar/`
- [X] T034 [P] [US2] Create `RemoveShipFromHangarCommand` + `RemoveShipFromHangarHandler` (delete entry idempotently) in `backend/src/NajaEcho.Application/Features/Hangar/RemoveShipFromHangar/`
- [X] T035 [US2] Implement `HangarRepository.SearchCatalogAsync`, `AddAsync`, and `RemoveAsync` in `backend/src/NajaEcho.Infrastructure/Hangar/HangarRepository.cs`
- [X] T036 [US2] Add `GET /api/hangar/catalog/search`, `POST /api/hangar/mine`, and `DELETE /api/hangar/mine/{shipId}` endpoints to `backend/src/NajaEcho.Api/Features/Hangar/HangarEndpoints.cs`; add `AddShipRequestDto`, `CatalogSearchItemDto`, `PagedCatalogSearchItemsResponse` DTOs in `Contracts/`; map 400/404/409 to ProblemDetails; Serilog logging
- [X] T037 [P] [US2] Create `CatalogSearchItem` and `AddShipRequest` Zod schemas in `frontend/src/features/hangar/schemas/catalogSearchItem.ts` and `frontend/src/features/hangar/schemas/addShipRequest.ts`
- [X] T038 [US2] Add `searchCatalog(params)`, `addShip(body)`, and `removeShip(shipId)` functions to `frontend/src/features/hangar/api/hangarApi.ts`
- [X] T039 [P] [US2] Create `useCatalogSearch(search)` TanStack Query hook (debounced) in `frontend/src/features/hangar/hooks/useCatalogSearch.ts`
- [X] T040 [P] [US2] Create `useAddShip()` TanStack mutation hook (invalidates `useMyHangar` on success) in `frontend/src/features/hangar/hooks/useAddShip.ts`
- [X] T041 [P] [US2] Create `useRemoveShip()` TanStack mutation hook (invalidates `useMyHangar` on success) in `frontend/src/features/hangar/hooks/useRemoveShip.ts`
- [X] T042 [US2] Create `AddShipDialog` component (search input, paged results list, `alreadyOwned` rows disabled + marked, success toast + dialog stays open, error toast + stays open, React Hook Form + Zod) in `frontend/src/features/hangar/components/AddShipDialog.tsx`
- [X] T043 [US2] Create `RemoveShipButton` hover overlay component (icon hidden by default, shown on card hover, confirm prompt before mutation fires, error toast on failure) in `frontend/src/features/hangar/components/RemoveShipButton.tsx`
- [X] T044 [US2] Wire `AddShipDialog` (triggered by Add Ship button) and `RemoveShipButton` (overlaid on each card) into `frontend/src/features/hangar/pages/MyHangarView.tsx` and `frontend/src/features/hangar/components/ShipCard.tsx`
- [X] T045 [P] [US2] Write frontend component tests for `AddShipDialog` (search results render, `alreadyOwned` disabled, success stays open, error stays open) and `RemoveShipButton` (hidden until hover, confirm prompt, removal flow) with MSW in `frontend/src/features/hangar/__tests__/AddRemoveShip.test.tsx`

**Checkpoint**: Full add/remove round-trip works; Add Ship dialog stays open after success; duplicate blocked with clear feedback; remove hover overlay is hidden at rest; 409 produces user-facing error message.

---

## Phase 5: User Story 3 — Browse Org Hangar (Priority: P3)

**Goal**: Member switches to Org Hangar, sees ships grouped by model with owner count badges (hover reveals owner names), plus My Ships toggle and member filter (only members owning ≥1 ship).

**Independent Test**: Switch to Org Hangar; two members who both own the same ship — model appears once with owner count 2; hover shows both names. My Ships toggle shows only current member's ships. Member filter shows only owning members; selecting one filters gallery; selecting All Members clears filter.

### Tests for User Story 3 — Write First, Verify Failing

- [X] T046 [P] [US3] Write failing Application unit tests for `GetOrgHangar` handler (grouping by `shipId`, correct `ownerCount`, correct `owners` list, `mine` flag filter, `memberId` filter, `memberId` overrides `mine`) in `backend/tests/NajaEcho.Application.Tests/Features/Hangar/GetOrgHangarHandlerTests.cs`
- [X] T047 [P] [US3] Write failing Application unit tests for `GetOwningMembers` handler (only members with ≥1 entry returned) in `backend/tests/NajaEcho.Application.Tests/Features/Hangar/GetOwningMembersHandlerTests.cs`
- [X] T048 [P] [US3] Write failing API integration tests (Testcontainers) for `GET /api/hangar/org` (200 paged, grouping correct, `mine` param, `memberId` param, empty state, 401) and `GET /api/hangar/org/members` (200 array, only owning members, 401) in `backend/tests/NajaEcho.Api.Tests/Features/Hangar/OrgHangarEndpointTests.cs`

### Implementation for User Story 3

- [X] T049 [P] [US3] Create `OrgShipCard` read model + `GetOrgHangarQuery` + `GetOrgHangarHandler` in `backend/src/NajaEcho.Application/Features/Hangar/GetOrgHangar/` (handler accepts `search`, `mine`, `memberId`, `page`, `pageSize`; returns `PagedResult<OrgShipCard>`)
- [X] T050 [P] [US3] Create `OwningMember` read model + `GetOwningMembersQuery` + `GetOwningMembersHandler` in `backend/src/NajaEcho.Application/Features/Hangar/GetOwningMembers/`
- [X] T051 [US3] Implement `HangarRepository.GetOrgHangarAsync` in `backend/src/NajaEcho.Infrastructure/Hangar/HangarRepository.cs` — SQL group by `ship_id`, `COUNT(DISTINCT user_id)` for `ownerCount`, JSON aggregation of `owners[]` with `DisplayName`; `mine` filter (`user_id = currentUser`); `memberId` filter (overrides `mine`); name ILIKE; paged; jsonb field extraction
- [X] T052 [US3] Implement `HangarRepository.GetOwningMembersAsync` in `backend/src/NajaEcho.Infrastructure/Hangar/HangarRepository.cs` — join `HangarEntries` ⨝ `AspNetUsers`, distinct, only members with ≥1 entry
- [X] T053 [US3] Add `GET /api/hangar/org` and `GET /api/hangar/org/members` endpoints to `backend/src/NajaEcho.Api/Features/Hangar/HangarEndpoints.cs`; add `OrgHangarShipCardDto`, `HangarOwnerDto`, `PagedOrgHangarShipCardsResponse`, `OwningMemberDto` DTOs in `Contracts/`; Serilog logging; 401 mapping
- [X] T054 [P] [US3] Create `OrgHangarShipCard` and `OwningMember` Zod schemas in `frontend/src/features/hangar/schemas/orgHangarShipCard.ts` and `frontend/src/features/hangar/schemas/owningMember.ts`
- [X] T055 [US3] Add `getOrgHangar(params)` and `getOwningMembers()` functions to `frontend/src/features/hangar/api/hangarApi.ts`
- [X] T056 [P] [US3] Create `useOrgHangar(search, mine, memberId)` TanStack Query hook (`useQuery` for now; upgraded in US4) in `frontend/src/features/hangar/hooks/useOrgHangar.ts`
- [X] T057 [P] [US3] Create `useOwningMembers()` TanStack Query hook in `frontend/src/features/hangar/hooks/useOwningMembers.ts`
- [X] T058 [US3] Create `OwnerCountBadge` component (owner count + person icon bottom-right; hover tooltip listing `owners[].displayName`) in `frontend/src/features/hangar/components/OwnerCountBadge.tsx`
- [X] T059 [US3] Create `OrgHangarView` page (search bar, My Ships toggle, member filter dropdown populated from `useOwningMembers` — "All Members" resets; selecting a member clears My Ships; renders `ShipCardGallery` with `OrgShipCard` data including `OwnerCountBadge`; no Add Ship button) in `frontend/src/features/hangar/pages/OrgHangarView.tsx`
- [X] T060 [US3] Wire `OrgHangarView` into the Org Hangar tab in `frontend/src/features/hangar/pages/HangarPage.tsx`
- [X] T061 [P] [US3] Write frontend component tests for `OwnerCountBadge` (count renders, hover list shows owner names), `OrgHangarView` (member filter, My Ships toggle logic, empty state, no Add Ship button) with MSW in `frontend/src/features/hangar/__tests__/OrgHangar.test.tsx`

**Checkpoint**: Org Hangar tab shows grouped ships with owner counts; hovering badge reveals owner names; My Ships/member filters work; no Add Ship button in Org view.

---

## Phase 6: User Story 4 — Infinite Scroll (Priority: P4)

**Goal**: Both gallery views load additional cards automatically as the member scrolls to the bottom; no pagination controls are ever visible; changing search or filters resets to page 1.

**Independent Test**: Seed more ships than one page (>25) in My Hangar or Org Hangar; scroll to bottom; additional cards appear without any Next/Previous button. Change search text; gallery resets to first page of filtered results.

- [X] T062 [US4] Upgrade `useMyHangar` to `useInfiniteQuery` (page-based, advance on `getNextPageParam`) in `frontend/src/features/hangar/hooks/useMyHangar.ts`
- [X] T063 [US4] Upgrade `useOrgHangar` to `useInfiniteQuery` (page-based, advance on `getNextPageParam`) in `frontend/src/features/hangar/hooks/useOrgHangar.ts`
- [X] T064 [US4] Add `IntersectionObserver` scroll sentinel element to `ShipCardGallery` (triggers `fetchNextPage` when sentinel enters viewport; reset on `search`/filter change by resetting query key) in `frontend/src/features/hangar/components/ShipCardGallery.tsx`
- [X] T065 [P] [US4] Write frontend infinite scroll tests for `ShipCardGallery` (sentinel triggers next page, no pagination controls rendered, search reset triggers page 1) in `frontend/src/features/hangar/__tests__/InfiniteScroll.test.tsx`

**Checkpoint**: Scroll through >25 ships in either view — new cards load automatically; no pagination controls visible at any viewport; filter/search changes restart from page 1.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Logging, error handling audit, readability check, and end-to-end validation.

- [X] T066 [P] Audit all `HangarEndpoints.cs` for complete Serilog structured logging (correlation ID, user ID, operation, result status) — no sensitive auth data logged; confirm each endpoint logs at appropriate level in `backend/src/NajaEcho.Api/Features/Hangar/HangarEndpoints.cs`
- [X] T067 [P] Verify all ProblemDetails error responses (400 validator errors, 404 inactive ship, 409 duplicate, 500 removal failure) surface the correct `title`/`detail` in the frontend via toast/inline message — trace through `AddShipDialog` and `RemoveShipButton` error handlers
- [X] T068 [P] Verify `ShipCard` readability scrim/overlay provides sufficient text contrast over any background image (dark gradient preferred) — spot-check with a high-contrast and a low-contrast ship image in `frontend/src/features/hangar/components/ShipCard.tsx`
- [X] T069 Run all backend tests (`dotnet test` from `backend/`) — all Application, Infrastructure, and API integration tests passing; verify Testcontainers integration tests use the real PostgreSQL schema
- [X] T070 Run all frontend tests (`npm run test:run` from `frontend/`) — all Vitest + RTL + MSW tests passing for the hangar feature
- [X] T071 Execute the full `quickstart.md` validation checklist end-to-end: navigate Hangar → add ship → image fallback → search → Add Ship dialog → remove ship → Org Hangar grouping + hover list → filters → infinite scroll

**Checkpoint**: All tests green; quickstart validation complete; feature ready for review.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — **BLOCKS all user story phases**
- **US1 (Phase 3)**: Depends on Phase 2 completion — no dependency on US2/US3/US4
- **US2 (Phase 4)**: Depends on Phase 2 completion; integrates with US1 frontend components (AddShipDialog wired into MyHangarView)
- **US3 (Phase 5)**: Depends on Phase 2 completion; builds on ShipCard/ShipCardGallery from US1 frontend
- **US4 (Phase 6)**: Depends on US1 and US3 (upgrades their hooks to `useInfiniteQuery`)
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Independent after Phase 2
- **US2 (P2)**: Independent after Phase 2; wires into US1 frontend views but does not block US1 test criteria
- **US3 (P3)**: Independent after Phase 2; reuses `ShipCard` / `ShipCardGallery` components from US1
- **US4 (P4)**: Modifies US1 + US3 hooks in-place; depends on both being complete

### Within Each User Story

1. Write tests first — confirm they fail
2. Backend: read models + handlers → repository implementation → endpoint registration
3. Frontend: types generated → Zod schemas → API client → hooks → components → pages → routing
4. Run tests; confirm passing before moving to next story

### Parallel Opportunities

- T002, T003, T004 can all run after T001 (or alongside if gen:api:hangar not yet needed)
- T005–T008 can run in parallel within Phase 2
- T012, T013, T014 can run in parallel (all test-writing, different files)
- T015, T019, T022 can run in parallel within US1
- T028, T029, T030, T031 can run in parallel (all failing test files)
- T032, T033, T034 can run in parallel within US2 backend (different handlers)
- T039, T040, T041 can run in parallel within US2 frontend (different hook files)
- T046, T047, T048 can run in parallel within US3 test phase
- T049, T050 can run in parallel within US3 backend (different handlers)
- T056, T057 can run in parallel within US3 frontend (different hook files)

---

## Parallel Example: User Story 1 Backend

```bash
# Write failing tests in parallel first:
Task T012: GetMyHangar handler unit tests
Task T013: HangarRepository jsonb extraction tests
Task T014: GET /api/hangar/mine integration tests (Testcontainers)

# Then implement (T015 and T019/T022 run in parallel):
Task T015: ShipCard read model + GetMyHangarQuery/Handler
Task T019: HangarShipCard Zod schema
Task T022: ShipCard component

# Then sequential implementation:
Task T016: HangarRepository.GetMyHangarAsync
Task T017: GET /api/hangar/mine endpoint
Task T020: hangarApi.ts getMyHangar function
Task T021: useMyHangar hook
Task T023: ShipCardGallery component
Task T024: MyHangarView page
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational — **critical, blocks everything**
3. Write US1 failing tests (T012–T014)
4. Implement US1 backend + frontend (T015–T026)
5. Write US1 frontend tests (T027)
6. **STOP and VALIDATE**: Navigate to `/hangar`; confirm My Hangar renders own ships, search works, empty state shown
7. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. US1 → My Hangar browsing works (MVP)
3. US2 → Members can add and remove ships (core value)
4. US3 → Org Hangar visible (collective fleet view)
5. US4 → Infinite scroll (polish)
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers (after Phase 2 is complete):

- Developer A: US1 backend (T015–T017), then US2 backend (T032–T036)
- Developer B: US1 frontend (T018–T026), then US2 frontend (T037–T044)
- Developer C: US3 backend (T049–T053) in parallel once US1 backend unblocks shared patterns

---

## Notes

- `[P]` tasks operate on different files with no dependency on incomplete sibling tasks
- `[Story]` label maps each task to a specific user story for traceability
- Constitution principle II (TDD) requires tests written and confirmed failing **before** implementation
- `raw_data` jsonb extraction (`url_photo`, `scu`, `crew`) belongs in the Infrastructure query — never send the full blob to the client
- `alreadyOwned` flag for the Add Ship dialog is computed per-user in the catalog search query
- Soft-deleted catalog ships: Add Ship search must filter to `Active` only; existing hangar entries retain their ship data even if the catalog ship is later soft-deleted
- Member filter in Org Hangar lists only users with ≥1 hangar entry (see R9)
- `scu` stored as string in `raw_data` — cast to numeric at SQL extraction; `crew` stays a string per R2
