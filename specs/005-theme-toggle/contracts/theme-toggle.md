# UI Contract: Dark/Light Theme Toggle

This feature exposes no HTTP/API contract (**No API contract changes required**). The contract below is the **frontend UI/module contract** — the public shapes the new `features/theme/` modules expose and the behavioural guarantees the toggle provides. These are what tests assert against.

---

## Module: `themeStorage.ts`

Single source of truth for the localStorage preference (React layer).

```ts
export type Theme = 'dark' | 'light'

export const STORAGE_KEY = 'theme'

/** Read + validate the stored preference. Returns null if absent or invalid (VR-1). */
export function getStoredTheme(): Theme | null

/** Persist the preference (VR-2). Only 'dark' | 'light' are accepted by the type. */
export function setStoredTheme(theme: Theme): void

/** Resolve the initial theme: stored → prefers-color-scheme → 'dark' (flagship). */
export function resolveInitialTheme(): Theme

/** Apply a theme to the DOM: toggles the `dark` class on document.documentElement (VR-3). */
export function applyTheme(theme: Theme): void
```

**Guarantees**:
- `getStoredTheme()` returns `null` for any value that is not exactly `'dark'` or `'light'` (corrupted/missing).
- `resolveInitialTheme()` never throws if `localStorage` or `matchMedia` are unavailable — it falls back to `'dark'`.
- `applyTheme('dark')` adds the `dark` class; `applyTheme('light')` removes it.

---

## Module: `ThemeProvider.tsx` + `useTheme.ts`

App-wide theme state via React context.

```ts
interface ThemeContextValue {
  theme: Theme            // current resolved theme
  toggleTheme: () => void // flip dark <-> light
  setTheme: (t: Theme) => void
}

export function ThemeProvider(props: { children: React.ReactNode }): JSX.Element

/** Consume theme state. Throws if used outside <ThemeProvider>. */
export function useTheme(): ThemeContextValue
```

**Behavioural guarantees**:
- On mount, `theme` initializes from `resolveInitialTheme()` so in-memory state matches what the inline boot script already applied to the DOM (no flash, no class flip on hydrate).
- Whenever `theme` changes, the provider (a) calls `applyTheme(theme)` and (b) calls `setStoredTheme(theme)`.
- `toggleTheme()` switches `'dark' ↔ 'light'`.
- `useTheme()` outside a provider throws a descriptive error (developer guardrail).

---

## Component: `components/ui/toggle.tsx` (generic primitive)

The standard shadcn `Toggle`, wrapping `@radix-ui/react-toggle`. Application-agnostic — no theme knowledge.

```ts
import * as TogglePrimitive from '@radix-ui/react-toggle'
import { type VariantProps } from 'class-variance-authority'

export const toggleVariants // cva: variant (default | outline) + size (default | sm | lg | icon)

export interface ToggleProps
  extends React.ComponentPropsWithoutRef<typeof TogglePrimitive.Root>,
    VariantProps<typeof toggleVariants> {}

export const Toggle: React.ForwardRefExoticComponent<ToggleProps & React.RefAttributes<HTMLButtonElement>>
```

**Guarantees**:
- Renders a native `<button>` with Radix-managed `data-state` (`on`/`off`) and `aria-pressed`.
- Controlled via `pressed` + `onPressedChange`; uncontrolled via `defaultPressed`.
- Styled with semantic tokens only; matches the repo `cva`/`cn`/`forwardRef` house style.

---

## Component: `ThemeToggle.tsx` (feature-specific)

Single-icon control that toggles the theme, composing the generic `Toggle`.

```ts
export function ThemeToggle(props?: { className?: string }): JSX.Element
```

**Rendering contract**:
- Renders the generic `Toggle` primitive (`size="icon"`), bound to theme state: `pressed={theme === 'dark'}`, `onPressedChange={(on) => setTheme(on ? 'dark' : 'light')}`.
- Shows a **Moon** icon when current theme is `light` (action = switch to dark); shows a **Sun** icon when current theme is `dark` (action = switch to light). Icons from `lucide-react`. (FR-005 — active-theme visual feedback.)
- Has an `aria-label` describing the action and reflecting state: `"Switch to dark theme"` when light, `"Switch to light theme"` when dark. (FR-007.)
- Relies on Radix Toggle's automatic `aria-pressed`/`data-state` for the two-state semantics in US3.
- The decorative icon carries `aria-hidden`.

**Behavioural contract**:
- Clicking (or pressing Enter/Space when focused) flips the theme → `.dark` class updates, localStorage updates, icon + `aria-label` + `aria-pressed` update. (FR-001, FR-002, US1, US3.)
- Keyboard-focusable with a visible focus ring (inherited from the `Toggle` primitive). (SC-004.)

---

## Boot contract: `index.html` inline script

```html
<html lang="en" class="dark">
  <head>
    <!-- runs synchronously before the module script and first paint -->
    <script>
      // resolve: stored 'theme' → prefers-color-scheme → 'dark'
      // toggle the `dark` class on document.documentElement accordingly
    </script>
    ...
```

**Guarantee**: The correct theme class is present on `<html>` before the first paint, so there is no flash of incorrect theme (SC-005). Mirrors `resolveInitialTheme()` logic (research Decision 3). `class="dark"` remains the static default for the no-JS case.

---

## App wiring contract: `App.tsx`

`<ThemeProvider>` wraps `<AppRouter/>` so theme state is available app-wide (authenticated and unauthenticated routes alike). `<QueryClientProvider>` remains the outer provider; ordering relative to `ThemeProvider` is not significant (theme does not depend on query state).

---

## Placement contract: `DashboardHeader.tsx`

`<ThemeToggle />` is rendered in the header's right-hand control group, beside `<AccountMenu />`. The header remains thin — it adds one element and imports `ThemeToggle` from `features/theme/`; no theme logic lives in the header.

---

## Test surface (what the contract guarantees, asserted by tests)

| Test file | Asserts |
|-----------|---------|
| `themeStorage.test.ts` | valid read; invalid/missing → `null`; write persists; `resolveInitialTheme` order (stored > system > dark); `applyTheme` toggles class |
| `ThemeProvider.test.tsx` | initial theme from resolution; `toggleTheme` flips state; side effects (class + localStorage) on change; `useTheme` outside provider throws |
| `ThemeToggle.test.tsx` | renders one toggle button with correct `aria-label` + `aria-pressed` per theme; correct icon per theme; click toggles (class + storage + label/pressed update); keyboard activation works |
