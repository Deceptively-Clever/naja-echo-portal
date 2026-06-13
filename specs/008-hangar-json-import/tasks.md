# Tasks: Hangar JSON Import

**Input**: Design documents from `/specs/008-hangar-json-import/`

**Branch**: `008-hangar-json-import`

**Constitution note**: Test-First (TDD) is non-negotiable per constitution Principle II. Test tasks
within each user story phase MUST be written and confirmed failing before the implementation tasks
that make them pass.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no shared dependencies)
- **[Story]**: Which user story this task belongs to
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Generate frontend types from the new OpenAPI contract so all downstream type
references resolve. The contract is already authored (`specs/008-hangar-json-import/contracts/openapi.yaml`).

- [X] T001 Run `npm run gen:api:hangar-import` in `frontend/` to generate `frontend/src/lib/api/hangar-import.d.ts` from `specs/008-hangar-json-import/contracts/openapi.yaml`

**Checkpoint**: `frontend/src/lib/api/hangar-import.d.ts` exists with `ImportHangarRequest`, `ImportShipRecord`, and `ImportHangarResult` types.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Application-layer types, repository port extension, and frontend Zod schema — every
user story phase depends on these. **No user story implementation can begin until this phase is
complete.**

⚠️ **CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Create `ImportShipRecord` application type in `backend/src/NajaEcho.Application/Features/Hangar/ImportHangar/ImportShipRecord.cs` — record with `Name` (string, required), `ShipName` (string?), `Unidentified` (string?) matching the JSON fields from the HangarXPLOR export
- [X] T003 [P] Create `ImportHangarCommand` in `backend/src/NajaEcho.Application/Features/Hangar/ImportHangar/ImportHangarCommand.cs` — record with `UserId` (Guid) and `Items` (IReadOnlyList<ImportShipRecord>)
- [X] T004 [P] Create `ImportHangarResult` in `backend/src/NajaEcho.Application/Features/Hangar/ImportHangar/ImportHangarResult.cs` — record with `TotalRecords` (int), `ImportedShips` (int), `UnmatchedRecords` (int), `UnmatchedShipNames` (IReadOnlyList<string>)
- [X] T005 Add `Task ReplaceFromImportAsync(Guid userId, IReadOnlyList<Guid> shipIds, CancellationToken ct)` to `backend/src/NajaEcho.Application/Abstractions/IHangarRepository.cs` — atomic delete-all-then-insert for a user's hangar entries
- [X] T006 [P] Create `frontend/src/features/hangar/schemas/hangarImport.ts` with Zod schemas: `importShipRecordSchema` (lenient — extra fields allowed, `name` required), `importHangarResultSchema` (totalRecords, importedShips, unmatchedRecords, unmatchedShipNames), and exported TypeScript types

**Checkpoint**: Foundation complete — T002–T006 done; handler and frontend hook can now be implemented in parallel.

---

## Phase 3: User Story 1 — Import Ship List from HangarXPLOR Export (Priority: P1) 🎯 MVP

**Goal**: Authenticated member can click Import, acknowledge the destructive warning, select a
HangarXPLOR JSON file, and see their hangar atomically replaced with matched ships plus a summary.

**Independent Test**: Upload a valid HangarXPLOR export → hangar refreshes to matched ships only;
import/skipped summary visible; Org Hangar and catalog `alreadyOwned` flag reflect new state.

### Tests for User Story 1 (TDD — write FIRST, confirm failing)

> **Write these tests FIRST, ensure they FAIL before writing any implementation.**

- [X] T007 [US1] Write `ImportHangarHandlerTests` in `backend/tests/NajaEcho.Application.Tests/Features/Hangar/ImportHangar/ImportHangarHandlerTests.cs` covering: all records match → `importedShips` = matched count; `ship_name` preferred over `name`; `name` used as fallback when `shipName` is absent; case-insensitive match; duplicate records resolving to same ship → single entry; no matches → `importedShips = 0`; all records unidentified → `importedShips = 0`
- [X] T008 [P] [US1] Write `ImportHangarEndpointTests` (Testcontainers) in `backend/tests/NajaEcho.Api.Tests/Features/Hangar/ImportHangar/ImportHangarEndpointTests.cs` covering: POST /api/hangar/mine/import with valid body → 200 + result; unauthorized → 401; hangar rows match expected set after import
- [X] T009 [P] [US1] Write `ImportHangar.test.tsx` in `frontend/src/features/hangar/__tests__/ImportHangar.test.tsx` covering: Import button visible on MyHangar; clicking Import shows warning dialog; file picker not shown until warning confirmed; confirming + selecting valid file calls API; success summary shown; My Hangar query invalidated after success

### Backend Implementation for User Story 1

- [X] T010 [US1] Implement `ImportHangarHandler` in `backend/src/NajaEcho.Application/Features/Hangar/ImportHangar/ImportHangarHandler.cs` — filter `unidentified` records, resolve effective name (`ShipName ?? Name`), match against Active ships via catalog lookup (name ILIKE), de-duplicate resolved ship IDs, call `repository.ReplaceFromImportAsync`, build and return `ImportHangarResult`
- [X] T011 [US1] Implement `ReplaceFromImportAsync` in `backend/src/NajaEcho.Infrastructure/Hangar/HangarRepository.cs` — open explicit transaction, delete all `sc.hangar_entries` for `userId`, insert one `HangarEntry` per `shipId` in `shipIds` with `AddedAt = now`, `SaveChangesAsync`, `CommitAsync`; on any exception the transaction rolls back leaving hangar unchanged; include `GetShipIdsByNamesAsync` helper (single query, case-insensitive `ILIKE` equality, Active only, returns `Dictionary<string, Guid>`)
- [X] T012 [P] [US1] Add import DTOs to `backend/src/NajaEcho.Api/Features/Hangar/Contracts/HangarDtos.cs` — `ImportShipRecordDto` (Name, ShipName?, Unidentified?), `ImportHangarRequestDto` (IReadOnlyList<ImportShipRecordDto> Items), `ImportHangarResultDto` (TotalRecords, ImportedShips, UnmatchedRecords, IReadOnlyList<string> UnmatchedShipNames)
- [X] T013 [US1] Add `POST /api/hangar/mine/import` route to `backend/src/NajaEcho.Api/Features/Hangar/HangarEndpoints.cs` — maps `ImportHangarRequestDto` → `ImportHangarCommand`, calls `ImportHangarHandler`, returns `200 ImportHangarResultDto`; use `TryGetUserId` helper; add Serilog log at start (count received) and end (counts imported/unmatched)
- [X] T014 [US1] Register `ImportHangarHandler` with `services.AddScoped<ImportHangarHandler>()` in `backend/src/NajaEcho.Infrastructure/DependencyInjection.cs`

### Frontend Implementation for User Story 1

- [X] T015 [US1] Add `importHangar(items: ImportShipRecord[]): Promise<ImportHangarResult>` to `frontend/src/features/hangar/api/hangarApi.ts` — POST `{ items }` to `/api/hangar/mine/import`, parse response with `importHangarResultSchema`
- [X] T016 [US1] Create `frontend/src/features/hangar/hooks/useImportHangar.ts` — TanStack Query mutation wrapping `importHangar`; on success invalidate query keys for `hangarQueryKeys.mine`, `hangarQueryKeys.org`, `hangarQueryKeys.members`, and `hangarQueryKeys.catalogSearch` (all) to force refetch
- [X] T017 [US1] Create `frontend/src/features/hangar/components/ImportHangarDialog.tsx` — three-step modal: (1) **Warning** step: "Importing will replace ALL ships in your hangar. This cannot be undone." with Cancel and Confirm buttons; (2) **File** step: file input (accept `.json`, 5 MB max), parse with `importShipRecordSchema` (array), submit triggers `useImportHangar` mutation; (3) **Summary** step: shows imported count, total unmatched count, and list of unmatched ship names; close button returns to My Hangar
- [X] T018 [US1] Add **Import** button to `frontend/src/features/hangar/pages/MyHangarView.tsx` beside the existing Add Ship button — opens `ImportHangarDialog`

**Checkpoint**: US1 complete — happy path import works end-to-end; hangar refreshes; summary visible.

---

## Phase 4: User Story 2 — Partial Match with Unrecognized Ships (Priority: P2)

**Goal**: When a file contains unidentified or unmatched ships, the import proceeds for all
recognized ships and the user sees which ship names were skipped.

**Independent Test**: Import file with ≥1 `unidentified` record and/or ≥1 name absent from catalog
→ recognized ships appear in hangar; summary lists each skipped name; no crash.

### Tests for User Story 2

- [X] T019 [US2] Extend `ImportHangar.test.tsx` in `frontend/src/features/hangar/__tests__/ImportHangar.test.tsx` with tests: API returns `unmatchedShipNames: ["A.T.L.S.", "Unknown"]` → dialog summary step lists both names; `unmatchedRecords = 0` → no unmatched section shown

### Implementation for User Story 2

- [X] T020 [US2] Verify `ImportHangarDialog.tsx` (`frontend/src/features/hangar/components/ImportHangarDialog.tsx`) summary step renders the `unmatchedShipNames` list with a visible heading (e.g. "Unrecognized ships skipped:") when the list is non-empty; renders nothing for that section when the list is empty — extend T017 output if not already present

**Checkpoint**: US2 complete — unmatched ship names visible in the summary; empty list case is clean.

---

## Phase 5: User Story 3 — Invalid File Handling (Priority: P3)

**Goal**: Non-JSON or wrong-shape files are rejected client-side with a clear message, without
touching the hangar or making any API call. Malformed bodies sent directly to the API receive `400`.

**Independent Test**: Select a `.txt` file → inline error shown, no API call made, hangar unchanged.
Select JSON that is not an array of records → same. Send malformed JSON to the endpoint → 400.

### Tests for User Story 3

- [X] T021 [US3] Extend `ImportHangar.test.tsx` in `frontend/src/features/hangar/__tests__/ImportHangar.test.tsx` with tests: selecting a non-JSON file → error message shown, `importHangar` API function NOT called; selecting JSON missing `name` field → error message shown, no API call; file exceeding 5 MB → size error shown, no API call
- [X] T022 [P] [US3] Extend `ImportHangarEndpointTests` in `backend/tests/NajaEcho.Api.Tests/Features/Hangar/ImportHangar/ImportHangarEndpointTests.cs` with test: POST with missing `items` field → 400 ProblemDetails, hangar rows unchanged

### Implementation for User Story 3

- [X] T023 [US3] Add client-side file validation to `frontend/src/features/hangar/components/ImportHangarDialog.tsx` file step: check file size ≤ 5 MB before reading; `JSON.parse` the text (catch SyntaxError → "File is not valid JSON"); validate parsed value with `importShipRecordSchema.array()` Zod parse (catch ZodError → "File does not match the expected format"); display inline error below the file input; block form submission on any error — no API call is made when validation fails
- [X] T024 [US3] Verify `backend/src/NajaEcho.Api/Features/Hangar/HangarEndpoints.cs` import route returns `400 ProblemDetails` when `items` is missing or not an array — add explicit null/empty-collection check at the route handler before calling the handler, or rely on ASP.NET Core model-binding validation of the required `items` property on `ImportHangarRequestDto`

**Checkpoint**: US3 complete — all three stories independently testable; no invalid file ever mutates the hangar.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Logging completeness, spec reconciliation note, and final validation run.

- [X] T025 Verify structured Serilog logs in `backend/src/NajaEcho.Api/Features/Hangar/HangarEndpoints.cs` import handler: log `UserId`, `ReceivedCount` at request start; log `ImportedShips`, `UnmatchedRecords` at completion; confirm no file content or PII in log fields
- [X] T026 Update `specs/008-hangar-json-import/spec.md` FR-009: change "duplicate ship names produce multiple hangar entries" to "duplicate import records resolving to the same catalog ship collapse into a single hangar entry" (documents the accepted deviation from original wording per research R4)
- [X] T027 Run all quickstart.md validation scenarios from `specs/008-hangar-json-import/quickstart.md` and confirm each scenario produces the expected outcome

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — run first; unblocks type-checked frontend work.
- **Phase 2 (Foundational)**: Depends on Phase 1. **Blocks all user story phases.**
- **Phase 3 (US1)**: Depends on Phase 2 completion. TDD: T007–T009 before T010–T018.
- **Phase 4 (US2)**: Depends on Phase 3 (dialog exists, API shape settled). Light — extends T017/T019.
- **Phase 5 (US3)**: Depends on Phase 3 (dialog file step exists). Extends validation only.
- **Phase 6 (Polish)**: Depends on Phases 3–5.

### User Story Dependencies

- **US1 (P1)**: Foundational complete — core backend + frontend pipeline. MVP.
- **US2 (P2)**: US1 complete — verifies + extends summary display (no new API surface).
- **US3 (P3)**: US1 complete — adds file validation guard inside the dialog.

### Within Each User Story

1. Tests written and confirmed **failing** first (constitution Principle II)
2. Application types / schemas before service/handler
3. Handler before endpoint
4. Endpoint/DI before frontend API function
5. Hook before component
6. Component before page integration

### Parallel Opportunities

- T002, T003, T004, T006 can all run in parallel (different new files)
- T007, T008, T009 can run in parallel (different test files, same phase)
- T012, T015 can run in parallel with T010/T011 when schemas are settled
- T021, T022 can run in parallel (different test files)

---

## Parallel Example: User Story 1

```bash
# Phase 2 — can all run together:
Task T002: Create ImportShipRecord.cs
Task T003: Create ImportHangarCommand.cs
Task T004: Create ImportHangarResult.cs
Task T006: Create hangarImport.ts Zod schema

# Phase 3 tests — all at once (TDD — write first):
Task T007: Backend handler unit tests
Task T008: Backend endpoint integration tests
Task T009: Frontend component tests

# After T010 (handler) + T011 (repo) done — run together:
Task T012: Add import DTOs to HangarDtos.cs
Task T015: Add importHangar to hangarApi.ts
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001)
2. Complete Phase 2: Foundational (T002–T006) — blocks everything else
3. Complete Phase 3: US1 (T007–T018) — write tests first, then implement
4. **STOP and VALIDATE**: test all 5 happy-path scenarios from quickstart.md
5. Demo/deploy the MVP: Import button on My Hangar, full replace-all working

### Incremental Delivery

1. Setup + Foundational → types and schemas ready
2. US1 → core import working, happy path tested → Deploy MVP
3. US2 → unmatched names displayed in summary → Deploy
4. US3 → file validation guard added → Deploy
5. Polish → logging verified, spec FR-009 updated

---

## Notes

- No EF migration is required — reuses `sc.hangar_entries` from feature 007.
- `ux_hangar_entries_user_ship` unique constraint enforces de-duplication (R4); do not attempt to insert duplicates.
- Match is against `sc.ships.name` case-insensitively with `=` semantics (not wildcard), `status = Active` only.
- The `gen:api:hangar-import` script is already added to `frontend/package.json`.
- CLAUDE.md SPECKIT marker already updated to this plan.
- FR-009 deviation (de-duplication) is documented in research R4 and plan Summary; T026 updates the spec to reflect it.
