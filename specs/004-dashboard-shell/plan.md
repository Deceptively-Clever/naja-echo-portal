# Implementation Plan: Dashboard Shell

**Branch**: `004-dashboard-shell` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-dashboard-shell/spec.md`

## Summary

Create the first authenticated **dashboard shell** — the shared, responsive, dark-first application frame that every signed-in page renders inside. The shell owns the header (brand + account area), persistent desktop navigation, a mobile navigation drawer, and a main content outlet with a reusable page-header pattern. Navigation is data-driven from a single source of truth and consumed identically by desktop and mobile. Three routes render inside the shell: a dashboard home (welcome + placeholder cards), a Profile placeholder, and a Settings placeholder. Existing authentication is preserved unchanged — the shell nests **inside** the existing `ProtectedRoute` guard.

**No API contract changes required.** This is a frontend-only layout feature: no new or changed backend HTTP behaviour, no OpenAPI changes, no backend changes.

**Technical approach**: The frontend is React 19 + TypeScript (strict) on Vite, routed with `react-router-dom` v7, styled with Tailwind CSS v4 using the semantic theme tokens already defined in `src/index.css` (spec 003). shadcn-style primitives are **hand-authored** in `src/components/ui/` (no shadcn CLI in this repo) following the existing `cva` + `cn` + `forwardRef` pattern. The plan adds a `DashboardLayout` route-element component that renders the shell and an `<Outlet />`, nested between the existing `ProtectedRoute` and the dashboard page routes. Five generic primitives are added to `components/ui/` (Sheet, DropdownMenu, Separator, Badge, Skeleton); application-specific shell composition lives under `features/dashboard/`.

## Technical Context

**Language/Version**: TypeScript ~5.8 (strict), React 19.2

**Primary Dependencies**: `react-router-dom` v7 (data router via `BrowserRouter`/`Routes`), Tailwind CSS v4 (`@tailwindcss/vite`), `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react`, `@tanstack/react-query` v5. **New** (to add): `@radix-ui/react-dialog` (Sheet), `@radix-ui/react-dropdown-menu` (account menu), `@radix-ui/react-separator` (dividers) — same MIT-licensed Radix family already in use.

**Storage**: N/A (no persistence; placeholder content is static)

**Testing**: Vitest + React Testing Library + jsdom; MSW for session mocking. `npm run test:run`, typecheck via `tsc -b` (part of `npm run build`), lint via `npm run lint`.

**Target Platform**: Modern evergreen browsers (SPA served by Vite)

**Project Type**: Web application — frontend only for this feature (`frontend/` workspace)

**Performance Goals**: Shell renders immediately on authenticated navigation; no measurable bundle/runtime regression beyond three small Radix primitives.

**Constraints**: WCAG AA contrast (inherited from theme tokens); dark theme is the default; no horizontal scroll from 320 px to desktop; semantic tokens only (no raw hex in layout/feature components); preserve existing auth/session behaviour; no theme-switcher UI; no backend/OpenAPI changes.

**Scale/Scope**: 1 shell layout + ~6 shell sub-components, 1 navigation model, 3 routed pages (1 home with placeholder cards, 2 placeholders), 5 new generic UI primitives, 3 new Radix dependencies, frontend tests for shell render / nav active-state / mobile open-close / child-in-shell / unauthenticated block.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. API-Contract-First | ✅ PASS | **No API contract changes required** — recorded explicitly. UI-only feature; introduces no new/changed backend HTTP behaviour, no OpenAPI edits. |
| II. Test-First / TDD | ✅ PASS | Frontend component/hook tests added for new user-facing behaviour (shell render, active nav, mobile drawer, child-in-shell, unauthenticated redirect). Existing tests must stay green (SC-006). Uses existing Vitest/RTL/MSW stack — no new test infra. |
| III. Frontend/Backend Separation | ✅ PASS | Frontend-only; no backend, DB, or shared-type changes. Account area consumes the existing `/api/auth/me` session shape — no new contract. |
| IV. Simplicity / YAGNI | ✅ PASS | Navigation `access` field is a single optional placeholder, not an authorization system. Profile/Settings are placeholders. Only primitives genuinely used by the shell are added; no speculative components. |
| V. Observability | ✅ N/A | No backend/log surface in this feature. |
| VI. Modular Monolith + Clean Architecture | ✅ PASS | Generic primitives stay in `components/ui/` and remain application-agnostic. Shell composition lives in `features/dashboard/`. Route components stay thin (page bodies delegate to shell + feature components). No raw hex; semantic tokens only. |

**Frontend Conventions check**:
- shadcn/ui ownership respected — new primitives are generic, no feature behaviour embedded. ✅
- API client/type generation — no API types touched; session type reused. ✅
- TanStack Query — account area reuses existing `useCurrentUser`/`useSignOut`; no new server state invented. ✅
- Forms — none in this feature. ✅
- **Dashboard shell & navigation** — authenticated routes render inside the shell; shell owns header/nav/mobile-nav/account/outlet; navigation is data-driven from a single source of truth with label/path/icon/active-matching + optional access field; desktop and mobile consume the same model. ✅ (This feature *implements* that convention.)

**Gate result: PASS.** No violations. Complexity Tracking not required.

**Post-Design re-check (after Phase 1)**: ✅ PASS — design artifacts (data-model, UI contract, quickstart) introduce no new violations. The three Radix dependencies are MIT-licensed, in the already-approved Radix family, and each is justified by a concrete shell requirement (mobile drawer, account menu, dividers).

## Project Structure

### Documentation (this feature)

```text
specs/004-dashboard-shell/
├── plan.md              # This file
├── research.md          # Phase 0 output — decisions & rationale
├── data-model.md        # Phase 1 output — NavItem model, shell region model
├── quickstart.md        # Phase 1 output — validation/run guide
├── contracts/
│   └── dashboard-shell.md   # Phase 1 output — UI contract (component props/regions/nav model)
├── checklists/
│   └── requirements.md  # Spec quality checklist (already created by /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
frontend/
├── src/
│   ├── components/
│   │   └── ui/                          # generic primitives — application-agnostic
│   │       ├── button.tsx               # EXISTS — reused (variants: default/secondary/outline/ghost/destructive)
│   │       ├── card.tsx                 # EXISTS — reused for placeholder cards
│   │       ├── avatar.tsx               # EXISTS — reused in account area
│   │       ├── alert.tsx                # EXISTS — available for error/empty feedback
│   │       ├── sheet.tsx                # NEW — mobile nav drawer (wraps @radix-ui/react-dialog)
│   │       ├── dropdown-menu.tsx        # NEW — account menu (wraps @radix-ui/react-dropdown-menu)
│   │       ├── separator.tsx            # NEW — dividers (wraps @radix-ui/react-separator)
│   │       ├── badge.tsx                # NEW — "placeholder/coming soon" labels (pure CSS)
│   │       └── skeleton.tsx             # NEW — loading feedback placement (pure CSS)
│   └── features/
│       ├── auth/
│       │   ├── ProtectedRoute.tsx       # EXISTS — UNCHANGED (outer auth guard preserved)
│       │   ├── hooks/useCurrentUser.ts  # EXISTS — reused by account area
│       │   ├── hooks/useSignOut.ts      # EXISTS — reused by account menu
│       │   └── components/SignOutButton.tsx  # EXISTS — reused or composed into account menu
│       └── dashboard/
│           ├── navigation/
│           │   └── navItems.ts          # NEW — single source of truth (NavItem[])
│           ├── components/
│           │   ├── DashboardLayout.tsx  # NEW — shell root; renders header/nav/<Outlet/>
│           │   ├── DashboardHeader.tsx  # NEW — brand + mobile trigger + account area
│           │   ├── DashboardSidebar.tsx # NEW — persistent desktop nav (consumes navItems)
│           │   ├── DashboardMobileNav.tsx # NEW — Sheet drawer (consumes navItems)
│           │   ├── DashboardNav.tsx      # NEW — shared nav list (NavLink active matching)
│           │   ├── AccountMenu.tsx       # NEW — avatar dropdown + sign-out (reuses useSignOut)
│           │   ├── PageHeader.tsx        # NEW — title + optional description + optional actions
│           │   └── UserBadge.tsx         # EXISTS — reused in account area
│           └── pages/
│               ├── DashboardPage.tsx     # EXISTS → refactor to home (welcome + placeholder cards)
│               ├── ProfilePage.tsx       # NEW — placeholder (PageHeader + "coming soon")
│               └── SettingsPage.tsx      # NEW — placeholder (PageHeader + "coming soon")
│   └── routes/
│       └── AppRouter.tsx                 # CHANGE — nest DashboardLayout inside ProtectedRoute; add child routes
└── (no backend / OpenAPI / contract changes)
```

**Structure Decision**: Frontend-only change confined to the `frontend/` workspace. Generic primitives are added to `components/ui/` and kept application-agnostic (Principle VI); all application-specific shell composition lives under `features/dashboard/`. The existing `ProtectedRoute` is **not modified** — the shell nests inside it, preserving auth behaviour exactly. The single source of navigation truth is `features/dashboard/navigation/navItems.ts`.

## Key Technical Decisions

1. **Layout nesting (preserve auth)**: Introduce `DashboardLayout` as a route-element that renders the shell chrome + `<Outlet />`. Wire it as a nested layout route *inside* the existing `ProtectedRoute` route in `AppRouter.tsx`. `ProtectedRoute` stays byte-for-byte unchanged, so the existing redirect/guard behaviour and its tests are preserved. Child pages render into the shell's `<main>` outlet.

2. **Route structure**: Adopt `/dashboard`, `/dashboard/profile`, `/dashboard/settings`. The app currently has only `/dashboard`; the new nested children extend it without renaming the existing route.

3. **Data-driven navigation**: `navItems.ts` exports a typed `NavItem[]` with `{ label, path, icon, end?, access? }`. `end` controls exact-match for the index route (`/dashboard`). `access` is a single optional, non-enforced placeholder field (e.g. `access?: string`) so future authorization can filter the list without a permissions system now. Both desktop sidebar and mobile drawer map the same array via a shared `DashboardNav` renderer using `NavLink` for active matching.

4. **Active state without color-only**: `NavLink`'s `isActive` applies both a background/text token change **and** a non-color affordance (left border/indicator bar and `aria-current="page"`), satisfying FR-005 and accessibility "not color alone".

5. **New primitives, hand-authored**: No shadcn CLI in this repo (primitives are owned source). Author `sheet.tsx`, `dropdown-menu.tsx`, `separator.tsx` over their Radix packages, plus pure-CSS `badge.tsx` and `skeleton.tsx`, matching the existing `cva`/`cn`/`forwardRef` house style and using semantic tokens only.

6. **Account area**: `AccountMenu` uses DropdownMenu triggered by the user avatar (initials fallback via existing `UserBadge`/`Avatar`), showing `displayName` + `discordUsername` and a Sign-out item wired to the existing `useSignOut` mutation. No tokens or auth internals exposed; no new account behaviour.

7. **Dashboard home refactor**: The current `DashboardPage` (a centered card showing user + sign-out) is refactored into the shell-hosted home page — account/sign-out move to the shell's account area, and the page body becomes a welcome section + placeholder summary cards (Org overview, Upcoming operations, Member activity, Getting started) built on `Card`, each clearly marked placeholder via `Badge`. Its test is updated to assert the new home content; account-area assertions move to a shell test.

8. **Global feedback placement**: The shell's main region reserves placement for loading (`Skeleton`), empty, and error (`Alert`) states. This feature wires the structural slots and uses `Skeleton` for the in-shell loading affordance; real per-feature data states are added when real features arrive.

9. **Responsive model**: Tailwind `md` breakpoint is the desktop threshold — sidebar persistent at `md+`, mobile header trigger + `Sheet` drawer below `md`. No fixed pixel dimensions that break reflow; content uses fluid widths with `max-w` containers. Mobile drawer closes on navigation (`onClick`/route change) and is dismissible via Escape and overlay (Radix Dialog defaults).

## Complexity Tracking

No constitution violations. Section intentionally empty.
