# Phase 0 Research: Dashboard Shell

**Feature**: 004-dashboard-shell | **Date**: 2026-06-13

This document records the investigation of the existing frontend and the decisions resolving each planning unknown. All findings are from direct inspection of the `frontend/` workspace.

## Codebase Findings (inspection)

| Area | Finding |
|------|---------|
| Router | `react-router-dom` v7. `AppRouter.tsx` uses `BrowserRouter` + `Routes`/`Route`. Protected area is `<Route element={<ProtectedRoute />}>` wrapping `<Route path="/dashboard" element={<DashboardPage />} />`. |
| Auth guard | `features/auth/ProtectedRoute.tsx` — calls `useCurrentUser()` (TanStack Query → `/api/auth/me`), shows a centered "Loading…" during `isLoading`, redirects to `/` via `<Navigate replace>` when `!session.authenticated`, otherwise renders `<Outlet />`. |
| Session shape | `sessionStateSchema.ts` — discriminated union on `authenticated`. Authenticated user = `{ id (uuid), displayName, discordUsername }`. **No avatar URL.** |
| Existing UI primitives | `components/ui/`: `button.tsx`, `card.tsx`, `avatar.tsx`, `alert.tsx`. Authored by hand with `cva` + `cn` + `forwardRef`. |
| Button variants | `default`, `secondary`, `outline`, `ghost`, `destructive`; sizes `default`, `sm`, `lg`, `icon`. Already semantic-token based. |
| Radix deps present | `@radix-ui/react-alert-dialog`, `@radix-ui/react-avatar`. |
| Theme | `src/index.css` fully defines light (`:root`) + dark (`.dark`) token sets and registers them via `@theme inline` (spec 003). `index.html` has `<html class="dark">` — **dark is the default**. `lucide-react` available for icons. |
| Account building blocks | `UserBadge.tsx` (avatar initials + name, accessible label), `SignOutButton.tsx` (uses `useSignOut` mutation), `useCurrentUser`, `useSignOut`. |
| Test stack | Vitest + RTL + jsdom + MSW. `tests/testUtils.tsx` exports `createWrapper(initialEntries)` (QueryClient + `MemoryRouter`). `tests/handlers.ts` defaults `/api/auth/me` → authenticated "Test User". |
| `cn` util | `lib/utils.ts` — `twMerge(clsx(...))`. |

## Decisions

### D1 — How to insert the shell without changing auth behaviour

- **Decision**: Add a `DashboardLayout` route-element rendering the shell + `<Outlet />`, nested **inside** the existing `ProtectedRoute` route. `ProtectedRoute.tsx` is not modified.
- **Rationale**: Preserves the guard and its passing tests exactly (FR-003, SC-002, SC-006). Nested layout routes are the idiomatic react-router v7 pattern for shared chrome.
- **Alternatives considered**: (a) Merge shell into `ProtectedRoute` — rejected: couples auth with layout, churns guard tests. (b) Render shell per page — rejected: violates "no duplicated shell" (FR-002, SC-007).

### D2 — Route names

- **Decision**: `/dashboard` (home), `/dashboard/profile`, `/dashboard/settings`.
- **Rationale**: Matches the brief's suggested structure; the existing app already uses `/dashboard`, so children extend it with no rename. Avoids breaking the existing route/tests.
- **Alternatives considered**: Flatter top-level `/profile`, `/settings` — rejected: the brief groups them under the dashboard shell and they should sit behind the same guard/layout.

### D3 — Which shadcn primitives to add, and how

- **Decision**: Hand-author five new primitives in `components/ui/`: `sheet.tsx` (over `@radix-ui/react-dialog`), `dropdown-menu.tsx` (over `@radix-ui/react-dropdown-menu`), `separator.tsx` (over `@radix-ui/react-separator`), plus pure-CSS `badge.tsx` and `skeleton.tsx`. Add the three Radix packages as dependencies.
- **Rationale**: There is **no shadcn CLI** wired in this repo — primitives are owned source authored by hand (confirmed by inspection). The "established workflow" here is hand-authoring in the existing `cva`/`cn`/`forwardRef` style with semantic tokens. Sheet is required for the mobile drawer; DropdownMenu for the account menu; Separator for clean dividers; Badge to mark placeholders clearly temporary; Skeleton for the loading-feedback slot. Each is concretely used by this feature (YAGNI satisfied).
- **Dependency review (Principle: dependency additions reviewed)**: `@radix-ui/react-dialog`, `@radix-ui/react-dropdown-menu`, `@radix-ui/react-separator` are MIT-licensed, maintained, and in the same Radix family already approved in this repo. Low risk.
- **Alternatives considered**: (a) Custom drawer without Radix — rejected: re-implements focus trap, scroll lock, Escape/overlay dismissal and accessibility that Radix Dialog provides for free (FR-010, FR-015). (b) Skip Separator/Badge/Skeleton — partially viable, but each maps to an explicit brief/spec need (dividers, "coming soon" labelling, loading feedback) at trivial cost.

### D4 — Active navigation state that is not color-only

- **Decision**: Use `NavLink` from react-router; in the active branch apply a token-based background/text change **and** a non-color indicator (left border/indicator bar) plus `aria-current="page"`.
- **Rationale**: Satisfies FR-005 / accessibility "active state must not rely on color alone" and gives assistive tech a programmatic current-page signal.
- **Alternatives considered**: Color-only highlight — rejected: fails accessibility requirement and SC-003.

### D5 — Single source of navigation truth

- **Decision**: `features/dashboard/navigation/navItems.ts` exports a typed `NavItem[]`. Desktop sidebar and mobile drawer both render from this one array via a shared `DashboardNav` component.
- **Rationale**: FR-006/FR-007, SC-004 (add a destination by editing one file). Same model both viewports (constitution Frontend Conventions).
- **Alternatives considered**: Separate desktop/mobile lists — rejected: duplication and drift risk.

### D6 — Minimal, non-enforced access placeholder

- **Decision**: `NavItem.access?` is a single optional field (e.g. `access?: string`) that is **not** evaluated anywhere yet.
- **Rationale**: Lets a future authorization layer filter items without building permissions now (YAGNI; spec out-of-scope "full permissions system").
- **Alternatives considered**: Role arrays + guard logic — rejected: out of scope, premature.

### D7 — Account area data source

- **Decision**: `AccountMenu` reuses `useCurrentUser` for `displayName`/`discordUsername`, an avatar with initials fallback (no avatar URL in the session), and `useSignOut` for the sign-out action.
- **Rationale**: Reuses existing session data and sign-out behaviour; exposes no tokens/auth internals; adds no backend behaviour (FR-011, spec account-area constraints).
- **Alternatives considered**: New profile fetch — rejected: no new backend, none needed.

### D8 — Dashboard home refactor & test migration

- **Decision**: Refactor the existing `DashboardPage` into the shell-hosted home page (welcome + placeholder cards). The account/sign-out it currently renders move to the shell account area. Update `DashboardPage.test.tsx` to assert new home content; move account-area assertions (user name, sign-out) into a new shell test.
- **Rationale**: Avoids two competing "user + sign-out" surfaces; keeps the home route thin and placeholder-based (spec US5). Keeps SC-006 (existing suite stays green after intended updates).
- **Alternatives considered**: Leave `DashboardPage` as-is and add account area in shell too — rejected: duplicate sign-out controls, confusing UX.

### D9 — Responsive breakpoint & mobile dismissal

- **Decision**: Tailwind `md` is the desktop threshold. Persistent sidebar at `md+`; below `md`, a header menu button opens a `Sheet` drawer. Drawer closes on item selection and is dismissible via Escape + overlay (Radix Dialog defaults).
- **Rationale**: FR-008/FR-009/FR-010/FR-017; no horizontal overflow down to 320 px via fluid widths and `max-w` containers.
- **Alternatives considered**: Off-canvas custom CSS — rejected: reinvents Radix Dialog behaviour.

## Resolved Unknowns

All Technical Context items are resolved; **no NEEDS CLARIFICATION remain**. The theme, auth guard, session shape, router, and primitive house-style are all confirmed by inspection, and the spec's assumptions (dark default, reuse `ProtectedRoute`, placeholder Profile/Settings) hold against the actual codebase.
