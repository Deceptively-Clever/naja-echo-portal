# UI Contract: Dashboard Shell

**Feature**: 004-dashboard-shell | **Date**: 2026-06-13

**No API contract changes required.** This feature exposes **no HTTP/backend interface**. The "contract" below is the **UI contract**: the component surfaces, routing structure, navigation model, and accessibility guarantees that downstream feature pages depend on. This is the stable surface future authenticated features build against.

## Routing contract

Authenticated routes render inside the shell, nested within the existing auth guard:

```text
<Route element={<ProtectedRoute />}>        # EXISTING — unchanged
  <Route element={<DashboardLayout />}>     # NEW — shell + <Outlet/>
    <Route path="/dashboard"          element={<DashboardPage />} />   # home (index)
    <Route path="/dashboard/profile"  element={<ProfilePage />} />     # placeholder
    <Route path="/dashboard/settings" element={<SettingsPage />} />    # placeholder
  </Route>
</Route>
```

**Guarantees**:
- Unauthenticated access to any of the above paths redirects to `/` (via existing `ProtectedRoute`).
- A new authenticated page is added by registering one child `<Route>` inside `DashboardLayout` — no shell code duplication (SC-007).

## Navigation model contract

Single source of truth: `features/dashboard/navigation/navItems.ts`.

```ts
export interface NavItem {
  label: string          // visible + accessible name
  path: string           // must match a registered route
  icon: LucideIcon       // lucide-react icon component
  end?: boolean          // exact active matching (index route)
  access?: string        // RESERVED — not enforced in this feature
}

export const navItems: NavItem[]   // ordered; rendered by desktop + mobile alike
```

**Guarantees**:
- Desktop and mobile navigation render from the same `navItems` array.
- Adding a destination = appending one `NavItem` (SC-004).
- `access` is inert; presence/absence changes nothing today.

## Component contracts

### `DashboardLayout` (route element)
- Props: none (reads route context).
- Renders: `<header>` (DashboardHeader), desktop `<nav>` (DashboardSidebar) at `md+`, `<main>` containing `<Outlet />`.
- Guarantee: renders only for authenticated users (nested under `ProtectedRoute`).

### `DashboardHeader`
- Renders brand (links to `/dashboard`), mobile nav trigger (`< md`), and `AccountMenu`.
- Mobile trigger is a labelled `Button` (`aria-label`, e.g. "Open navigation").

### `DashboardNav` (shared renderer)
- Props: `{ items: NavItem[]; onNavigate?: () => void }`.
- Renders each item as a react-router `NavLink`.
- Active item: token background/text change **plus** a non-color indicator **plus** `aria-current="page"` (not color-only — SC-003).
- `onNavigate` fires on item click (mobile uses it to close the drawer).

### `DashboardSidebar`
- Persistent desktop nav at `md+`; composes `DashboardNav` with `items={navItems}`.

### `DashboardMobileNav`
- Wraps `DashboardNav` in a `Sheet` (Radix Dialog).
- Dismissible by Escape and overlay click; closes on item selection (`onNavigate`).

### `AccountMenu`
- Trigger: avatar (initials fallback) — labelled for assistive tech.
- Content: `displayName`, `discordUsername`, and a Sign-out item invoking existing `useSignOut`.
- Exposes no tokens or auth internals.

### `PageHeader`
- Props: `{ title: string; description?: string; actions?: ReactNode }`.
- `title` rendered as the page heading; `description` muted; `actions` right-aligned.

## New generic primitives (in `components/ui/`, application-agnostic)

| Primitive | Backed by | Purpose |
|-----------|-----------|---------|
| `Sheet` | `@radix-ui/react-dialog` | mobile nav drawer |
| `DropdownMenu` | `@radix-ui/react-dropdown-menu` | account menu |
| `Separator` | `@radix-ui/react-separator` | visual dividers |
| `Badge` | pure CSS + `cva` | "placeholder / coming soon" labels |
| `Skeleton` | pure CSS | loading-feedback placement |

**Guarantee**: these contain no dashboard/feature-specific behaviour and use semantic tokens only.

## Accessibility contract

- Landmarks: one `<header>`, navigation in `<nav>`, page content in `<main>`.
- All nav links and account controls are keyboard-reachable with visible `ring-ring` focus states.
- Mobile drawer is dismissible by keyboard (Escape) and pointer (overlay/close).
- Active nav state conveyed by `aria-current` + non-color indicator (not color alone).
- Meaningful icons have accessible labels; purely decorative icons are `aria-hidden`.
- No horizontal scroll from 320 px to desktop.

## Theming contract

- Shell and feature components use semantic tokens only: `bg-background`, `text-foreground`, `bg-card`, `text-card-foreground`, `border-border`, `text-muted-foreground`, `bg-primary`, `text-primary-foreground`, `bg-secondary`, `text-secondary-foreground`, `bg-accent`, `text-accent-foreground`, `ring-ring`, and brand tokens (`text-brand`, …).
- **No raw hex** values in layout/feature components.
- Dark theme is the default (root `class="dark"`).
