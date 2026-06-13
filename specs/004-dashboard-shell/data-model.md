# Phase 1 Data Model: Dashboard Shell

**Feature**: 004-dashboard-shell | **Date**: 2026-06-13

This feature is frontend-only with **no persisted data** and **no API entities**. The "model" here is the in-app view model for navigation and the structural regions of the shell. No database, no migrations, no OpenAPI types.

## Entity: NavItem (navigation source of truth)

The single typed model consumed by both desktop and mobile navigation. Lives in `features/dashboard/navigation/navItems.ts`.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `label` | `string` | yes | Visible nav label and accessible name (e.g., "Dashboard"). |
| `path` | `string` | yes | Route path the item links to (e.g., `/dashboard/profile`). |
| `icon` | `LucideIcon` | yes | Lucide React icon component reference for the item. |
| `end` | `boolean` | no | When `true`, active matching is exact (used for the index route `/dashboard` so it isn't active on child routes). |
| `access` | `string` | no | **Non-enforced** placeholder for future authorization. Not evaluated anywhere in this feature. |

### Validation / rules

- The array MUST contain at least the three initial items: Dashboard (`/dashboard`, `end: true`), Profile (`/dashboard/profile`), Settings (`/dashboard/settings`).
- `path` values MUST correspond to routes registered in `AppRouter.tsx`.
- Active matching is delegated to react-router `NavLink` (`end` maps to `NavLink`'s exact-match behaviour).
- Adding a future destination (Admin, Organizations, Billing, Notifications, Help) MUST be possible by appending one entry to this array with no other file changes (SC-004).

### Initial data (illustrative shape, not final code)

```text
[
  { label: "Dashboard", path: "/dashboard",          icon: LayoutDashboard, end: true },
  { label: "Profile",   path: "/dashboard/profile",  icon: User },
  { label: "Settings",  path: "/dashboard/settings", icon: Settings },
]
```

## Structural model: Shell regions

The `DashboardLayout` composes these regions. They are structural (landmarks/slots), not data entities.

| Region | Landmark | Owner component | Notes |
|--------|----------|-----------------|-------|
| App header | `<header>` | `DashboardHeader` | Brand/logo + mobile nav trigger + account area. |
| Brand/logo | — | `DashboardHeader` | Naja Echo identity using `text-brand`/semantic tokens; links to `/dashboard`. |
| Desktop navigation | `<nav>` | `DashboardSidebar` → `DashboardNav` | Persistent at `md+`. |
| Mobile navigation | `<nav>` inside `Sheet` | `DashboardMobileNav` → `DashboardNav` | Drawer below `md`; dismissible by Escape/overlay; closes on selection. |
| Account area | — | `AccountMenu` | Avatar (initials) + name + sign-out via `useSignOut`. |
| Main content outlet | `<main>` | `DashboardLayout` | Renders `<Outlet />`; hosts page header + page body. |
| Page header | — | `PageHeader` | `title` (required), `description?`, `actions?`. |
| Global feedback slot | — | main region | Loading (`Skeleton`), empty, error (`Alert`) placement. |

## Component view-model: PageHeader props

| Prop | Type | Required | Description |
|------|------|----------|-------------|
| `title` | `string` | yes | Page title rendered as the page's primary heading. |
| `description` | `string` | no | Supporting subtitle in `text-muted-foreground`. |
| `actions` | `ReactNode` | no | Optional right-aligned action slot (buttons, etc.). |

## Reused entities (no change)

- **SessionState** (`sessionStateSchema.ts`) — consumed read-only by `AccountMenu` via `useCurrentUser`. Not modified. Fields used: `user.displayName`, `user.discordUsername`.

## Out of model (explicitly)

- No metrics/analytics entities (placeholder cards are static).
- No profile/settings form models.
- No permission/role entities (the `access` field is an inert placeholder).
