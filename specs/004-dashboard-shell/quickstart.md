# Quickstart & Validation: Dashboard Shell

**Feature**: 004-dashboard-shell | **Date**: 2026-06-13

A validation guide proving the dashboard shell works end-to-end. Implementation details live in `tasks.md` / the implementation phase; this file is how you confirm it.

## Prerequisites

- Node + the repo's frontend deps installed.
- Working directory: `frontend/`.
- New dependencies present (added during implementation):
  `@radix-ui/react-dialog`, `@radix-ui/react-dropdown-menu`, `@radix-ui/react-separator`.

```bash
cd frontend
npm install
```

## Validation commands

```bash
# Type-check + build
npm run build        # runs tsc -b then vite build

# Lint
npm run lint

# Unit/component tests (CI mode)
npm run test:run
```

All three MUST pass. Existing tests MUST remain green (SC-006).

## Manual run

```bash
cd frontend
npm run dev
```

Then exercise the scenarios below in the browser.

## Acceptance validation scenarios

Map to spec acceptance criteria. ✅ = expected outcome.

### 1. Authenticated shell renders (US1, FR-001/002)
- Sign in, open `/dashboard`.
- ✅ Header with brand, persistent desktop navigation, account area, and main content area all visible.
- ✅ Page content sits inside the shell (header/nav not duplicated by the page).

### 2. Unauthenticated access blocked (US2, FR-003, SC-002)
- While signed out, navigate directly to `/dashboard`, `/dashboard/profile`, `/dashboard/settings`.
- ✅ Each redirects to `/` (existing `ProtectedRoute` behaviour, unchanged).

### 3. Active navigation state (US3, FR-005, SC-003)
- Click Dashboard, then Profile, then Settings.
- ✅ The current item shows an active style **and** a non-color indicator, with `aria-current="page"`.
- ✅ Exactly one item is active; `/dashboard` is not active when on a child route (`end` matching).

### 4. Desktop persistent nav (FR-008)
- At `md+` width.
- ✅ Primary navigation is visible without any menu interaction.

### 5. Mobile navigation (US4, FR-009/010, FR-017)
- Narrow viewport (e.g., 375 px, then 320 px).
- ✅ Sidebar is hidden; a labelled menu button is visible in the header.
- ✅ Clicking it opens the drawer with all nav items.
- ✅ Drawer closes on: item selection, Escape key, and overlay click.
- ✅ No horizontal scrolling at 320 px.

### 6. Account area (FR-011)
- Open the account menu.
- ✅ Shows display name + Discord username and a working Sign-out action (existing behaviour).
- ✅ No tokens/auth internals exposed.

### 7. Dashboard home placeholder (US5, FR-013)
- On `/dashboard`.
- ✅ Welcome section + placeholder summary cards (e.g., Org overview, Upcoming operations, Member activity, Getting started), each clearly marked placeholder.

### 8. Page header pattern (FR-014)
- Observe each page's header region.
- ✅ Title shows; description and actions render when provided.

### 9. Extensibility (SC-004, SC-007)
- (Dev check) Append one `NavItem` to `navItems.ts` and register one child route.
- ✅ New destination appears in desktop + mobile nav and renders inside the shell with no shell edits.

### 10. Theming & a11y (FR-012/015/016/018)
- ✅ Dark theme by default.
- ✅ Tab through the shell: every nav link and account control is reachable with a visible focus ring.
- ✅ No raw hex in layout/feature components (semantic tokens only).

## Automated test coverage (Vitest + RTL + MSW)

The following are covered by component tests using the existing `createWrapper` / MSW handlers:

- Shell renders for an authenticated session (header, nav, main).
- A child page renders inside the shell (child-in-shell).
- Initial nav items render (Dashboard, Profile, Settings).
- Active nav state reflects the current route (incl. `aria-current`).
- Mobile drawer open/close (trigger opens; Escape/selection closes) — where jsdom supports the interaction.
- Unauthenticated access redirects (existing `ProtectedRoute` test remains green).

## Confirmation: no backend changes

- ✅ No files changed under the backend solution.
- ✅ No OpenAPI/contract edits (`gen:api` output unchanged).
- ✅ No new or changed HTTP endpoints. The shell consumes only the existing `/api/auth/me` and `/api/auth/signout`.
