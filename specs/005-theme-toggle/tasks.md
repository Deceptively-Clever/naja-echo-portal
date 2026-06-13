# Tasks: Dark/Light Theme Toggle

**Input**: Design documents from `/specs/005-theme-toggle/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/theme-toggle.md ✅, quickstart.md ✅

**Constitution**: TDD is NON-NEGOTIABLE (Principle II). Test tasks are required — tests must be written and confirmed to **fail** before production code is written.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Install the new dependency, add the jsdom `matchMedia` mock, and create the `features/theme/` folder skeleton so all subsequent phases can proceed.

- [x] T001 Install `@radix-ui/react-toggle` in `frontend/` (`npm install @radix-ui/react-toggle`)
- [x] T002 Add `window.matchMedia` mock to `frontend/src/tests/setup.ts` (default `matches: false`; supports per-test override)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core theme primitives that every user story depends on — the `Toggle` UI primitive, the storage helper, the React context/hook, and the no-FOUC boot script. All user story phases are blocked until this is complete.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T003 Author the generic `Toggle` primitive in `frontend/src/components/ui/toggle.tsx` — wraps `@radix-ui/react-toggle`, styled with `cva`/`cn`/`forwardRef` using semantic tokens, variants: `default`/`outline`, sizes: `default`/`sm`/`lg`/`icon` (house style matching existing primitives)
- [x] T004 [P] Write failing unit tests for `themeStorage` in `frontend/src/features/theme/themeStorage.test.ts` — cover: `getStoredTheme` returns `null` for missing/invalid values; valid `'dark'`/`'light'` returned as-is; `setStoredTheme` writes to localStorage; `resolveInitialTheme` order (stored > system > `'dark'` flagship default); `applyTheme('dark')` adds `dark` class, `applyTheme('light')` removes it
- [x] T005 Implement `themeStorage.ts` in `frontend/src/features/theme/themeStorage.ts` — exports: `Theme`, `STORAGE_KEY`, `getStoredTheme()`, `setStoredTheme()`, `resolveInitialTheme()`, `applyTheme()` per the data-model and UI contract; confirm T004 tests now pass
- [x] T006 [P] Write failing unit tests for `ThemeProvider` in `frontend/src/features/theme/ThemeProvider.test.tsx` — cover: initializes `theme` from `resolveInitialTheme()`; `toggleTheme()` flips state and triggers `applyTheme` + `setStoredTheme` side effects; `setTheme()` drives both side effects; `useTheme()` outside provider throws descriptive error
- [x] T007 Implement `ThemeProvider.tsx` and `useTheme.ts` in `frontend/src/features/theme/` — provider initializes from `resolveInitialTheme()`, calls `applyTheme`+`setStoredTheme` on every theme change; `useTheme` consumes context and throws if used outside provider; confirm T006 tests now pass
- [x] T008 Add the no-FOUC blocking inline script to `frontend/index.html` `<head>` (synchronous, before the module script) — logic: stored `theme` key → `prefers-color-scheme: light` → flagship `'dark'`; mirrors `resolveInitialTheme()` logic; `class="dark"` remains the static fallback on `<html>`
- [x] T009 Wrap `<AppRouter />` with `<ThemeProvider>` in `frontend/src/App.tsx` — `ThemeProvider` is the innermost wrapper around `AppRouter`, inside `QueryClientProvider`

**Checkpoint**: Foundation ready — `Toggle` primitive, storage helper, provider/hook, boot script, and app wiring are all in place. User story implementation can now begin.

---

## Phase 3: User Story 1 — User Switches Theme Preference (Priority: P1) 🎯 MVP

**Goal**: Deliver a working single-icon theme toggle in the dashboard header that immediately switches the full UI between dark and light themes.

**Independent Test**: Open the app, sign in, click the Sun/Moon icon in the header — the entire UI switches theme instantly. Both themes are visually correct. Keyboard activation (Tab to toggle → Enter/Space) also works.

### Tests for User Story 1

> **Write these tests FIRST and confirm they FAIL before implementing T011.**

- [x] T010 [P] [US1] Write failing component tests for `ThemeToggle` in `frontend/src/features/theme/ThemeToggle.test.tsx` — cover: renders a single button; shows Moon icon when theme is `light`, Sun icon when theme is `dark`; `aria-label` is `"Switch to dark theme"` in light / `"Switch to light theme"` in dark; click calls `toggleTheme` (verify class + localStorage + label update); keyboard Enter/Space activation works; `aria-pressed` reflects pressed state (`true` when dark)

### Implementation for User Story 1

- [x] T011 [US1] Implement `ThemeToggle.tsx` in `frontend/src/features/theme/ThemeToggle.tsx` — composes generic `Toggle` (`size="icon"`), `pressed={theme === 'dark'}`, `onPressedChange={(on) => setTheme(on ? 'dark' : 'light')}`, lucide-react `Moon` icon in light / `Sun` icon in dark (both `aria-hidden`), dynamic `aria-label`; confirm T010 tests now pass
- [x] T012 [US1] Add `<ThemeToggle />` to `frontend/src/features/dashboard/components/DashboardHeader.tsx` — render beside `<AccountMenu />` in the right-hand control group; import from `features/theme/ThemeToggle`; no other logic changes in the header

**Checkpoint**: User Story 1 is fully functional and independently testable. The toggle appears in the header, switches the full UI immediately, and is keyboard-operable.

---

## Phase 4: User Story 2 — Theme Preference Persists (Priority: P1)

**Goal**: Theme preference survives page reloads and new sessions via localStorage, with no flash of incorrect theme on load.

**Independent Test**: Switch to light theme → reload → app loads in light (no dark flash). Close and reopen → still light. Set localStorage `theme` to a garbage value → reload → graceful fallback, no crash. System `prefers-color-scheme` respected when no stored value.

> **Note**: US2 depends on the foundational storage helper and boot script (both complete after Phase 2). No additional implementation is needed — persistence is a built-in property of `themeStorage.ts` (T005) and the inline boot script (T008). This phase provides dedicated verification tests and confirms the integration works end-to-end.

### Tests for User Story 2

> **Write these tests FIRST and confirm they FAIL (or would catch regressions) before sign-off.**

- [x] T013 [P] [US2] Write/extend tests in `frontend/src/features/theme/themeStorage.test.ts` for persistence scenarios — cover: `resolveInitialTheme` with stored `'light'` returns `'light'`; stored `'dark'` returns `'dark'`; missing key + system light preference → `'light'`; missing key + no system preference → `'dark'` flagship default; corrupted/invalid stored value (`'purple'`, `''`, `null`) → falls back to system/default and does not throw; `setStoredTheme` + `getStoredTheme` round-trips correctly
- [x] T014 [P] [US2] Write integration test in `frontend/src/features/theme/ThemeProvider.test.tsx` — confirm that when `ThemeProvider` mounts with a stored `'light'` preference, the `dark` class is **absent** from `document.documentElement` (verifying no flash on a stored-light reload); and when stored `'dark'`, `dark` class is present

### Implementation for User Story 2

> No new source files needed — persistence is delivered by Phase 2 foundations. Confirm the following integration points are correct.

- [x] T015 [US2] Verify `frontend/index.html` inline script handles all resolution edge cases (missing key, system preference, flagship dark default, corrupted value) — manual smoke test per quickstart.md US2 scenarios; adjust inline script if any edge case is not covered
- [x] T016 [US2] Confirm all T013–T014 tests pass; run full test suite (`npm run test:run` in `frontend/`) to ensure no regressions

**Checkpoint**: User Stories 1 and 2 are both complete. Switching, persistence, no-FOUC, and fallbacks all work.

---

## Phase 5: User Story 3 — Accessible Theme Toggle (Priority: P2)

**Goal**: The toggle is fully keyboard-navigable with screen-reader-compatible ARIA labelling; `aria-pressed` correctly reflects the two-state choice.

**Independent Test**: Tab to toggle — visible focus ring present. Enter/Space activates. Screen reader announces the control with action + state. `aria-pressed` attribute toggles with theme.

> **Note**: The Radix `Toggle` primitive (T003) and the `ThemeToggle` component (T011, T012) already provide most accessibility behaviour out of the box (`aria-pressed`, keyboard activation, focus ring). This phase adds a dedicated test pass to assert all ARIA contract items explicitly, and ensures the `aria-label` wording meets the spec.

### Tests for User Story 3

> **Write/extend these tests and confirm they FAIL (or catch future regressions) before sign-off.**

- [x] T017 [P] [US3] Extend `frontend/src/features/theme/ThemeToggle.test.tsx` with explicit accessibility assertions — cover: button has visible focus ring class (from `Toggle` primitive); `aria-pressed="true"` when dark, `aria-pressed="false"` when light; icon has `aria-hidden="true"`; `aria-label` updates on every toggle; button is reachable by Tab order (not `tabIndex=-1`); activation via `userEvent.keyboard('{Enter}')` and `userEvent.keyboard(' ')` both trigger `toggleTheme`

### Implementation for User Story 3

> The Radix Toggle primitive handles `aria-pressed` automatically. Verify wording and test coverage.

- [x] T018 [US3] Review `ThemeToggle.tsx` `aria-label` wording — confirm it describes the *action* ("Switch to dark/light theme"), not just the current state, and that it updates correctly after each toggle; adjust if needed
- [x] T019 [US3] Confirm all T017 tests pass; run `npm run test:run` + `npm run lint` + `npm run build` to verify zero regressions, clean lint, and clean typecheck

**Checkpoint**: All three user stories are complete. The toggle switches themes (US1), persists (US2), and is fully accessible (US3).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation run, build verification, and ensuring existing tests remain green.

- [x] T020 [P] Run the full existing test suite to verify no regressions in dashboard-shell or auth tests: `npm run test:run` in `frontend/`
- [x] T021 [P] Run typecheck and lint: `npm run build` (includes `tsc -b`) and `npm run lint` in `frontend/`
- [x] T022 Run quickstart.md manual validation scenarios (US1, US2, US3) against the running dev server (`npm run dev`) — verify all done-when criteria are met

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 (T001 must complete before T003; T002 before T004/T006) — **blocks all user story phases**
- **Phase 3 (US1)**: Depends on Phase 2 complete
- **Phase 4 (US2)**: Depends on Phase 2 complete (can run in parallel with Phase 3 after Phase 2)
- **Phase 5 (US3)**: Depends on Phase 3 complete (T011 must exist for T017 to extend)
- **Phase 6 (Polish)**: Depends on Phases 3, 4, and 5

### Within Phase 2 (Foundational)

```
T001 → T003 (toggle.tsx needs dependency installed)
T002 → T004, T006 (matchMedia mock needed before storage/provider tests)
T004 → T005 (write failing tests, then implement)
T006 → T007 (write failing tests, then implement)
T005, T007 → T008 (storage helper must exist before wiring boot script)
T007 → T009 (provider must exist before wiring App.tsx)
```

### Within Phase 3 (US1)

```
T010 → T011 (write failing tests, then implement ThemeToggle)
T011 → T012 (ThemeToggle must exist before placing in DashboardHeader)
```

### Parallel Opportunities

Within Phase 2 (after T001, T002 complete): T003, T004, and T006 can all run in parallel.
Within Phase 3: T010 (test write) runs before T011; T011 must precede T012.
Within Phase 4: T013 and T014 can run in parallel.
After Phase 2: Phases 3 and 4 can run in parallel (different files, no conflicts).
Within Phase 6: T020 and T021 can run in parallel.

---

## Parallel Example: Phase 2 Foundational

```
# After T001 (npm install) and T002 (matchMedia mock) complete:

Parallel batch 1:
  Task T003: Author Toggle primitive in frontend/src/components/ui/toggle.tsx
  Task T004: Write failing themeStorage tests in frontend/src/features/theme/themeStorage.test.ts
  Task T006: Write failing ThemeProvider tests in frontend/src/features/theme/ThemeProvider.test.tsx

Then sequentially:
  T005: Implement themeStorage.ts (confirm T004 passes)
  T007: Implement ThemeProvider.tsx + useTheme.ts (confirm T006 passes)
  T008: Add inline script to frontend/index.html
  T009: Wire ThemeProvider in frontend/src/App.tsx
```

---

## Implementation Strategy

### MVP (User Stories 1 + 2 together — both are P1)

Both P1 user stories (switch + persist) share the same foundational layer and are not meaningfully separable for delivery — persistence is wired automatically once the storage helper and boot script exist. The practical MVP is:

1. Complete Phase 1 (Setup)
2. Complete Phase 2 (Foundational) — delivers Toggle primitive, storage, provider, boot script
3. Complete Phase 3 (US1) — delivers the visible toggle in the header
4. Complete Phase 4 (US2) — confirms persistence and edge-case coverage (mostly tests)
5. **STOP and VALIDATE**: run quickstart.md US1 + US2 scenarios

### Incremental Delivery

1. Phase 1 + 2 → foundation ready (no visible UI change yet)
2. Phase 3 → toggle appears and works in the header (US1 done)
3. Phase 4 → persistence confirmed and edge cases covered (US2 done) — **MVP deliverable**
4. Phase 5 → explicit accessibility test pass (US3 done)
5. Phase 6 → polish, full regression, build clean

---

## Notes

- **TDD is mandatory** (Constitution Principle II): every T00X test task must be written and confirmed to **fail** before the implementation task that follows it
- **No API contract changes** — this is a frontend-only feature; the backend is not touched
- `[P]` tasks = operate on different files with no dependency on an incomplete earlier task — safe to run in parallel
- Each user story phase is independently testable: US1 (toggle works), US2 (persistence works), US3 (accessibility assertions pass)
- New dependency `@radix-ui/react-toggle` must be installed (T001) before authoring `toggle.tsx` (T003)
- The inline boot script (T008) and the React provider (T007/T009) share the same resolution logic — keep them in sync; `themeStorage.resolveInitialTheme()` is the authoritative reference
- Commit after each checkpoint (end of Phase 2, end of each story phase) for clean bisect history
