# Tasks: Dashboard Shell

**Input**: Design documents from `/specs/004-dashboard-shell/`

**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/dashboard-shell.md ✅ | quickstart.md ✅

**No API contract changes required.** Frontend-only feature.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Maps to user story from spec.md (US1–US5)
- All paths relative to repository root

---

## Phase 1: Setup

**Purpose**: Install the three new Radix packages required by the shell primitives.

- [x] T001 Install `@radix-ui/react-dialog`, `@radix-ui/react-dropdown-menu`, and `@radix-ui/react-separator` in `frontend/package.json` via `npm install` in `frontend/`

**Checkpoint**: `npm install` succeeds; new Radix packages appear in `frontend/package.json` and `frontend/node_modules/`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared infrastructure that every user story depends on. No story work begins until this phase is complete.

**⚠️ CRITICAL**: All Phase 3+ phases depend on the navigation model, generic UI primitives, and page stubs created here.

- [x] T002 Create navigation source of truth `frontend/src/features/dashboard/navigation/navItems.ts` — export typed `NavItem` interface (`label`, `path`, `icon: LucideIcon`, `end?: boolean`, `access?: string`) and `navItems: NavItem[]` array with three entries: Dashboard (`/dashboard`, `end: true`), Profile (`/dashboard/profile`), Settings (`/dashboard/settings`)
- [x] T003 [P] Create `frontend/src/components/ui/sheet.tsx` — hand-authored generic Sheet/Drawer primitive over `@radix-ui/react-dialog`; expose `Sheet`, `SheetTrigger`, `SheetContent`, `SheetHeader`, `SheetTitle`, `SheetClose`; side prop (default `"left"`); semantic tokens only; follows existing `cva`/`cn`/`forwardRef` house style
- [x] T004 [P] Create `frontend/src/components/ui/dropdown-menu.tsx` — hand-authored generic DropdownMenu primitive over `@radix-ui/react-dropdown-menu`; expose `DropdownMenu`, `DropdownMenuTrigger`, `DropdownMenuContent`, `DropdownMenuItem`, `DropdownMenuSeparator`, `DropdownMenuLabel`; semantic tokens only; follows existing `cva`/`cn`/`forwardRef` house style
- [x] T005 [P] Create `frontend/src/components/ui/separator.tsx` — hand-authored generic Separator primitive over `@radix-ui/react-separator`; horizontal/vertical orientations; uses `border-border`; follows existing `cva`/`cn`/`forwardRef` house style
- [x] T006 [P] Create `frontend/src/components/ui/badge.tsx` — pure-CSS generic Badge using `cva`; variants: `default` (`bg-primary text-primary-foreground`), `secondary` (`bg-secondary text-secondary-foreground`), `outline` (`border-border text-foreground`), `muted` (`bg-muted text-muted-foreground`); no Radix dependency
- [x] T007 [P] Create `frontend/src/components/ui/skeleton.tsx` — pure-CSS Skeleton using `cn`; renders an animated `bg-muted` pulse block; accepts `className`; no Radix dependency
- [x] T008 [P] Create stub `frontend/src/features/dashboard/pages/ProfilePage.tsx` — minimal component: `export function ProfilePage() { return null }` (will be fleshed out in Phase 7)
- [x] T009 [P] Create stub `frontend/src/features/dashboard/pages/SettingsPage.tsx` — minimal component: `export function SettingsPage() { return null }` (will be fleshed out in Phase 7)

**Checkpoint**: `npm run build` compiles with the new files; the five new UI components exist with correct exports; navItems.ts exports a valid typed array; stub page files are importable.

---

## Phase 3: User Story 1 — Authenticated Shell Renders (Priority: P1) 🎯 MVP

**Goal**: An authenticated user opens any dashboard route and sees the consistent shell frame (header, navigation, account area, main content outlet) around the page content.

**Independent Test**: Render `DashboardLayout` with a mock authenticated session; assert header, nav region, and outlet slot all render. Render a child page inside the shell; assert child content appears.

### Tests for User Story 1 (TDD — write first, confirm failure, then implement)

- [x] T010 Write failing test `frontend/src/features/dashboard/components/DashboardLayout.test.tsx` — test "renders header, nav, and main outlet for authenticated user" using `createWrapper` + MSW default authenticated handler; assert `<header>`, `<nav>`, and `<main>` landmarks are present
- [x] T011 [P] Write failing test in `DashboardLayout.test.tsx` — test "renders child route content inside shell" by passing a child element via `MemoryRouter` `<Route element={<DashboardLayout />}>`; assert child text appears inside the shell

### Implementation for User Story 1

- [x] T012 [P] [US1] Create `frontend/src/features/dashboard/components/DashboardNav.tsx` — renders `navItems` as a list of `NavLink` elements; accepts `items: NavItem[]` and optional `onNavigate?: () => void`; each `NavLink` shows icon + label; applies `aria-current="page"` and token-based active styles on active items (basic active class via react-router `isActive`; non-color indicator styling will be completed in Phase 5)
- [x] T013 [P] [US1] Create `frontend/src/features/dashboard/components/AccountMenu.tsx` — `DropdownMenu` triggered by `Avatar` (initials from `useCurrentUser`); content shows `displayName`, `discordUsername`, a `Separator`, and a "Sign out" `DropdownMenuItem` that calls existing `useSignOut`; semantic tokens; no token/auth internals exposed
- [x] T014 [US1] Create `frontend/src/features/dashboard/components/DashboardHeader.tsx` — `<header>` landmark; left: brand text linking to `/dashboard` (`text-brand` token); centre/right: `AccountMenu`; includes a `Button` icon-trigger (aria-label "Open navigation") wired to a `mobileNavOpen` state prop or lifted callback (stub for now — wired in Phase 6); uses `bg-card border-b border-border`
- [x] T015 [P] [US1] Create `frontend/src/features/dashboard/components/DashboardSidebar.tsx` — persistent sidebar at `md+` (`hidden md:flex`); renders `<nav aria-label="Primary navigation">` wrapping `<DashboardNav items={navItems} />`; uses `bg-card border-r border-border` with comfortable padding
- [x] T016 [P] [US1] Create `frontend/src/features/dashboard/components/PageHeader.tsx` — accepts `title: string`, `description?: string`, `actions?: ReactNode`; renders `<div>` with `text-foreground` heading, `text-muted-foreground` description, and right-aligned `actions` slot; uses semantic tokens only
- [x] T017 [US1] Create `frontend/src/features/dashboard/components/DashboardLayout.tsx` — root authenticated shell; renders `<div>` with `bg-background` containing `<DashboardHeader>`, a flex row of `<DashboardSidebar>` + `<main className="flex-1 overflow-y-auto">` holding `<Outlet />`; exports `DashboardLayout`
- [x] T018 [US1] Update `frontend/src/routes/AppRouter.tsx` — import `DashboardLayout`, `ProfilePage`, `SettingsPage`; nest `<Route element={<DashboardLayout />}>` directly inside the existing `<Route element={<ProtectedRoute />}>` group; add child routes `/dashboard/profile` and `/dashboard/settings`; keep `ProtectedRoute` and `/dashboard` route unchanged
- [x] T019 [US1] Update `frontend/src/features/dashboard/pages/DashboardPage.test.tsx` — remove assertions for `UserBadge`/account area (these move to the shell); update to assert the dashboard home renders something (can be a simple smoke test until Phase 7 adds real home content); run tests and confirm they pass

**Checkpoint**: US1 is independently testable. `DashboardLayout.test.tsx` passes. An authenticated user visiting `/dashboard` in the dev server sees a header, a sidebar with nav items, and a main content area.

---

## Phase 4: User Story 2 — Unauthenticated Access Blocked (Priority: P1)

**Goal**: Unauthenticated users hitting any dashboard route are redirected to `/` (sign-in). Preserves existing `ProtectedRoute` behaviour unchanged.

**Independent Test**: In `ProtectedRoute.test.tsx`, verify that an unauthenticated session still redirects to `/` when routed through the new nested `DashboardLayout` level.

- [x] T020 [US2] Update `frontend/src/features/auth/ProtectedRoute.test.tsx` — add a test case "redirects anonymous user to / when DashboardLayout is nested inside ProtectedRoute" using the new `<Route element={<ProtectedRoute />}><Route element={<DashboardLayout />}><Route path="/dashboard" element={<div>Protected</div>} /></Route></Route>` nesting shape; assert redirect to `/` on anonymous session; confirm all existing tests still pass

**Checkpoint**: US2 confirmed. Existing guard tests pass; new nesting test passes. `ProtectedRoute.tsx` itself remains byte-for-byte unchanged.

---

## Phase 5: User Story 3 — Active Navigation State (Priority: P2)

**Goal**: The currently active navigation section is visually distinguished with a non-color indicator and `aria-current="page"`, and is reachable/activatable by keyboard.

**Independent Test**: Render `DashboardNav` at `/dashboard/profile`; assert the Profile item has `aria-current="page"` and a non-color active class; assert Dashboard item does not.

### Tests for User Story 3

- [x] T021 [US3] Write failing test in `frontend/src/features/dashboard/components/DashboardNav.test.tsx` — test "marks the active route item with aria-current and a non-color indicator"; render `DashboardNav` inside `MemoryRouter` at `/dashboard/profile`; assert Profile link has `aria-current="page"`; assert Dashboard link does not

### Implementation for User Story 3

- [x] T022 [US3] Update `frontend/src/features/dashboard/components/DashboardNav.tsx` — enhance NavLink `className` callback to apply non-color active indicator when `isActive` is true: left border stripe (`border-l-2 border-primary` or equivalent using `ring`/`accent` tokens), background token (`bg-accent/20` or `bg-muted`), text token (`text-foreground`), **plus** pass `aria-current="page"` on the active link; ensure inactive items use `text-muted-foreground`; run test from T021 and confirm it passes

**Checkpoint**: US3 confirmed. Active nav item has visible non-color indicator + `aria-current`. Keyboard tab reaches each nav link with a visible `ring-ring` focus ring.

---

## Phase 6: User Story 4 — Mobile Navigation (Priority: P2)

**Goal**: On narrow viewports, navigation collapses behind a menu button in the header. Opening it reveals a `Sheet` drawer with all nav items. The drawer closes on item selection, Escape, and overlay click.

**Independent Test**: Render `DashboardHeader` with mobile drawer wired; simulate clicking the trigger; assert Sheet content is visible; simulate Escape; assert Sheet is closed.

### Tests for User Story 4

- [x] T023 [US4] Write failing test in `frontend/src/features/dashboard/components/DashboardMobileNav.test.tsx` — test "opens and closes the mobile navigation drawer"; use RTL `userEvent` to click the mobile trigger; assert nav items are visible; press Escape; assert nav items are no longer in the document (if jsdom supports focus/key events with Radix Dialog; note in test file if not fully automatable)

### Implementation for User Story 4

- [x] T024 [US4] Create `frontend/src/features/dashboard/components/DashboardMobileNav.tsx` — `Sheet` wrapping `<nav aria-label="Mobile navigation">` containing `<DashboardNav items={navItems} onNavigate={close} />`; controlled open state via props (`open: boolean`, `onOpenChange: (v: boolean) => void`); `SheetHeader` with `SheetTitle` "Navigation"; displayed below `md` only when triggered
- [x] T025 [US4] Update `frontend/src/features/dashboard/components/DashboardHeader.tsx` — add `mobileNavOpen` local state; pass `open` and `onOpenChange` to `<DashboardMobileNav>`; show the mobile trigger `Button` at `md:hidden`; hide the trigger at `md+`; `DashboardMobileNav` renders adjacent to (not inside) the header element

**Checkpoint**: US4 confirmed. On < `md` viewport the sidebar is hidden, a menu button is visible, clicking it opens the Sheet drawer with nav items, and the drawer closes on Escape / overlay / item selection. Width 320 px has no horizontal scroll.

---

## Phase 7: User Story 5 — Dashboard Home Placeholder (Priority: P3)

**Goal**: The dashboard home page shows a welcoming landing experience with placeholder summary cards. Profile and Settings pages show placeholder content via `PageHeader`. All three are clearly temporary and easy to replace.

**Independent Test**: Render `DashboardPage` (home); assert a welcome heading and at least one placeholder card are visible. Render `ProfilePage` and `SettingsPage`; assert `PageHeader` titles render and no real data is fetched.

### Tests for User Story 5

- [x] T026 [P] [US5] Write failing test in `frontend/src/features/dashboard/pages/DashboardPage.test.tsx` — test "renders welcome section and placeholder cards"; assert heading containing "Welcome" or equivalent; assert at least one placeholder card element; ensure no account-area assertions remain (moved to shell test)

### Implementation for User Story 5

- [x] T027 [US5] Refactor `frontend/src/features/dashboard/pages/DashboardPage.tsx` — remove the existing centered card layout, `UserBadge`, and `SignOutButton` (now owned by shell); replace with a welcome section (`PageHeader` with title "Dashboard", description text) and four placeholder `Card` components: "Org Overview", "Upcoming Operations", "Member Activity", "Getting Started"; each card includes a `Badge variant="muted"` labelled "Coming soon"; no API calls; pure static content
- [x] T028 [P] [US5] Flesh out `frontend/src/features/dashboard/pages/ProfilePage.tsx` — `PageHeader` with title "Profile", description "Manage your account"; a placeholder `Card` with "Coming soon" `Badge`; no form or API calls
- [x] T029 [P] [US5] Flesh out `frontend/src/features/dashboard/pages/SettingsPage.tsx` — `PageHeader` with title "Settings", description "Configure your preferences"; a placeholder `Card` with "Coming soon" `Badge`; no form or API calls

**Checkpoint**: US5 confirmed. Dashboard home shows welcome + four placeholder cards. Profile and Settings show "Coming soon" placeholders. All three render inside the shell. `DashboardPage.test.tsx` passes.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Validation, cleanup, and confirmation that no backend/contract changes were introduced.

- [x] T030 Run `npm run test:run` from `frontend/`; fix any test failures introduced by this feature; all pre-existing tests must remain green (SC-006)
- [x] T031 [P] Run `npm run build` from `frontend/` (`tsc -b && vite build`); resolve all TypeScript errors; confirm zero type regressions
- [x] T032 [P] Run `npm run lint` from `frontend/`; resolve all lint warnings/errors introduced by new files
- [x] T033 Manually exercise the quickstart.md validation scenarios (`npm run dev` in `frontend/`): authenticated shell renders, nav active state, mobile drawer, account menu, unauthenticated redirect, placeholder home, profile/settings pages
- [x] T034 [P] Confirm no backend/OpenAPI changes: run `git diff --name-only` and assert no files outside `frontend/` are modified; verify `specs/002-identity-refactor/contracts/openapi.yaml` is unchanged

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 (Radix packages installed). **Blocks all story phases.**
- **US1 (Phase 3)**: Depends on Phase 2. **Blocks US2, US3, US4, US5** (shell must exist first).
- **US2 (Phase 4)**: Depends on Phase 3 (routing update in T018 must exist).
- **US3 (Phase 5)**: Depends on Phase 3 (DashboardNav.tsx must exist from T012).
- **US4 (Phase 6)**: Depends on Phase 3 (DashboardHeader.tsx stub mobile trigger from T014).
- **US5 (Phase 7)**: Depends on Phase 3 (DashboardLayout, PageHeader, AppRouter wiring).
- **Polish (Phase 8)**: Depends on all story phases being complete.

### User Story Dependencies (summary)

| Story | Depends on | Can parallel with |
|-------|-----------|-------------------|
| US1 (P1) | Phase 2 complete | — (must finish before others) |
| US2 (P1) | US1 complete | — |
| US3 (P2) | US1 complete | US4 (different files) |
| US4 (P2) | US1 complete | US3 (different files) |
| US5 (P3) | US1 complete | After US3/US4 preferable |

### Within Each Phase

- TDD order: tests written and **confirmed failing** before implementation tasks.
- Within implementation: nav model / primitives before shell components; DashboardNav before DashboardHeader/Sidebar; components before the layout that composes them; layout before router update.
- Each phase ends at a named **Checkpoint** — validate independently before advancing.

---

## Parallel Execution Examples

### Phase 2 — Parallel primitives (T003–T009)

All six primitive/stub files are independent of each other:

```
T003 sheet.tsx        ──┐
T004 dropdown-menu.tsx  │
T005 separator.tsx      │── run in parallel
T006 badge.tsx          │
T007 skeleton.tsx       │
T008 ProfilePage stub   │
T009 SettingsPage stub ─┘
```

### Phase 3 — Parallel shell components after tests pass

```
T010 DashboardLayout test        ─┐ (write tests first)
T011 child-in-shell test         ─┘

T012 DashboardNav.tsx   ──┐
T013 AccountMenu.tsx     │── run in parallel (different files)
T015 DashboardSidebar.tsx │
T016 PageHeader.tsx      ─┘

T014 DashboardHeader.tsx (depends T013 AccountMenu)
T017 DashboardLayout.tsx (depends T012, T013, T014, T015, T016)
T018 AppRouter.tsx update (depends T017)
```

### Phase 6 — US3 and US4 in parallel (separate files)

```
Phase 5 (US3 active state): DashboardNav.tsx enhancements
Phase 6 (US4 mobile nav):   DashboardMobileNav.tsx + DashboardHeader.tsx update
```

---

## Implementation Strategy

### MVP First (US1 + US2 only)

1. Complete Phase 1: Install Radix packages.
2. Complete Phase 2: Nav model, primitives, stubs.
3. Complete Phase 3 (US1): Shell renders for authenticated user. ← **MVP deliverable**
4. Complete Phase 4 (US2): Confirm unauthenticated redirect preserved.
5. **Stop and validate**: dev server, quickstart scenarios 1–2, full test run.

### Incremental Delivery

1. **After Phase 4**: Authenticated shell with basic nav is live. ← Demo-able.
2. **After Phase 5 (US3)**: Active nav state with accessibility. ← Keyboard users satisfied.
3. **After Phase 6 (US4)**: Mobile navigation fully operational. ← Mobile users satisfied.
4. **After Phase 7 (US5)**: Dashboard home + Profile/Settings placeholders complete. ← Full feature shipped.
5. **After Phase 8**: Polish green — ready for PR.

---

## Notes

- `[P]` tasks touch different files and have no blocking dependencies within their phase.
- Each `[Story]` label maps to a user story in `specs/004-dashboard-shell/spec.md`.
- The existing `ProtectedRoute.tsx` is **not modified** at any point — it is only tested.
- The three new Radix packages are MIT-licensed, in the same Radix family already approved in this project.
- Placeholder cards and `Badge variant="muted"` make it visually obvious to future developers that US5 content is temporary.
- No backend, OpenAPI, or shared-type files should be touched at any point. T034 confirms this.
