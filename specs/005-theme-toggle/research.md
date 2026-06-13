# Phase 0 Research: Dark/Light Theme Toggle

This feature adds a control and state layer over the theming mechanism already shipped in spec 003. Research focused on integrating cleanly with that mechanism, persisting per the user's localStorage direction, and eliminating the flash-of-incorrect-theme. No NEEDS CLARIFICATION markers remained from the spec; the user's plan input resolved persistence and control shape.

---

## Decision 1: Reuse the existing `.dark`-class token mechanism

**Decision**: Switch themes by adding/removing the `dark` class on `document.documentElement`. Light is the `:root` default; dark is the `.dark` override. No new CSS tokens or color work.

**Rationale**: `frontend/src/index.css` already defines both palettes and registers them as Tailwind color utilities via `@theme inline`, with `@custom-variant dark (&:is(.dark *))`. The whole component library already consumes semantic tokens (`bg-background`, `text-foreground`, etc.), so a single class toggle re-themes every component instantly. This is the standard Tailwind v4 dark-mode pattern and requires zero changes to existing components. It also makes SC-001 (<100 ms switch) trivial — it's one `classList` operation.

**Alternatives considered**:
- *Per-component theme props / context-driven inline styles* — rejected: enormous surface, fights the existing token system, no benefit.
- *Separate stylesheets swapped at runtime* — rejected: causes FOUC, defeats Tailwind's build-time utility generation, and the token system already handles theming with one class.

---

## Decision 2: Persist in localStorage under key `theme`

**Decision**: Store the selected theme in `localStorage` under key `theme`, value `"dark"` or `"light"`. No backend/user-profile persistence.

**Rationale**: The user explicitly directed localStorage. localStorage is synchronous, durable across sessions and tabs, and readable from the pre-React inline boot script (Decision 3) — which `sessionStorage` (cleared per tab session) and cookies (sent to the server, unnecessary here) are not as suited for. Principle IV (YAGNI) discourages adding server persistence that wasn't requested; this supersedes the spec's tentative "or user profile" assumption.

**Alternatives considered**:
- *User-profile persistence via the backend* — rejected: not requested, would require an API/contract change (violating the UI-only scope), and adds round-trips and auth coupling for a client-only preference.
- *Cookie* — rejected: would be sent on every request for no server-side benefit.

**Validation/fallback**: The stored value is validated against the allowed set on read. A missing or corrupted value falls back to the resolution chain (Decision 4), satisfying the spec's "corrupted preference" edge case.

---

## Decision 3: No-FOUC blocking inline script in `index.html`

**Decision**: Add a small synchronous `<script>` in `index.html`'s `<head>` (before the module script) that resolves the theme and sets/clears the `dark` class on `<html>` before first paint. Keep `class="dark"` on `<html>` as the pre-script default.

**Rationale**: SC-005 requires no flash of incorrect theme. React mounts asynchronously after the document parses; if the class were only set in React, a stored *light* preference would briefly show the default dark theme (a visible flash). A synchronous head script runs before the browser paints, so the correct theme is applied to the very first frame. This is the well-established pattern used by Tailwind, Next.js theme libraries, etc. Keeping `class="dark"` as the static default means even with JS disabled the flagship dark theme renders.

**Alternatives considered**:
- *Set the class only inside React (`useEffect`/provider)* — rejected: runs after first paint → visible flash, fails SC-005.
- *Server-side rendering of the class* — rejected: this is a Vite SPA with no SSR; not available.

**Duplication note**: The inline script and `themeStorage.ts` share the same resolution logic (read key, validate, consult `prefers-color-scheme`, fall back to dark). The inline script is intentionally a tiny standalone duplicate (it must run before any module loads and cannot import). The logic is small and stable; `themeStorage.ts` is the single source of truth for the React layer, and the inline script mirrors it. This is the accepted, minimal trade-off for the no-FOUC guarantee.

---

## Decision 4: Theme resolution order (first load)

**Decision**: Resolve the initial theme as: (1) explicit stored value in localStorage → (2) OS/browser `prefers-color-scheme` → (3) flagship **dark** default.

**Rationale**: Explicit user choice must win (spec US2, edge case on system changes). When there's no stored choice, honoring the OS `prefers-color-scheme` is the expected modern behaviour (spec US2 scenario 3). When neither yields a definitive light signal, defaulting to dark keeps consistency with spec 004's stated "dark theme is the default" and the shipped `class="dark"`. This refines the spec's literal "otherwise defaults to light" wording to respect the established flagship-dark default; the spec's *intent* — explicit/system signals take precedence — is preserved. Documented as an intentional reconciliation.

**Alternatives considered**:
- *Always default to light when unset* — rejected: contradicts the flagship dark default the app already ships and would itself cause a flash from the static `class="dark"`.
- *Ignore system preference entirely* — rejected: spec US2 scenario 3 explicitly wants system preference respected on first load.

---

## Decision 5: Do not subscribe to live system-preference changes

**Decision**: Consult `prefers-color-scheme` only as the first-load default when no stored choice exists. Do not add a `matchMedia` change listener that flips the theme while the app is open.

**Rationale**: The spec edge case states the app should "respect the user's explicit in-app choice over system changes." Once the user toggles, that choice is authoritative. Not subscribing keeps behaviour predictable and avoids surprising mid-session theme flips. Principle IV — no listener machinery that isn't needed.

**Alternatives considered**:
- *Live-follow the system until the user overrides* — rejected: adds a tri-state ("system/dark/light") model and listener lifecycle the user didn't ask for and the simple two-state toggle doesn't need.

---

## Decision 6: Client UI state via React context — not TanStack Query

**Decision**: Manage theme through a small `ThemeProvider` context + `useTheme` hook. Do not use TanStack Query.

**Rationale**: The Frontend Conventions mandate TanStack Query for *server* state. Theme is purely client UI state with a localStorage side effect — there is no server resource to fetch, cache, or invalidate. A minimal context provider is the correct, conventional tool and keeps the toggle and any future consumers decoupled from storage details.

**Alternatives considered**:
- *TanStack Query* — rejected: no server state; would be a misuse of the tool.
- *Prop drilling / module-level singleton* — rejected: context is cleaner for app-wide consumption and testability; a bare module singleton wouldn't re-render consumers on change.

---

## Decision 7: Single-icon shadcn `Toggle` primitive + feature-specific `ThemeToggle`

**Decision**: Add the standard shadcn `Toggle` as a new generic primitive `components/ui/toggle.tsx` (wrapping `@radix-ui/react-toggle`), then build the feature-specific `ThemeToggle` on top of it. `ThemeToggle` is bound to theme state (`pressed={theme === 'dark'}` / `onPressedChange`), shows a `lucide-react` Moon icon in light theme and Sun icon in dark theme, and carries a state-aware `aria-label`.

**Rationale**: The user explicitly asked for a shadcn **toggle** component. shadcn's `Toggle` is a pressed/unpressed button that maps cleanly onto a two-state theme choice and, via `@radix-ui/react-toggle`, manages `aria-pressed`/`data-state` automatically — which is exactly the `aria-pressed` semantics the spec's US3 calls for, given for free. It is keyboard-operable with a visible focus ring out of the box (FR-007/SC-004). The icon swap remains the active-theme visual feedback (FR-005); the `aria-label` ("Switch to light/dark theme") conveys the action to assistive tech. Splitting generic `Toggle` (in `components/ui/`) from application-specific `ThemeToggle` (in `features/theme/`) keeps Principle VI intact: the primitive stays application-agnostic, the theme-aware behaviour lives in the feature folder.

**Cost accepted**: one new dependency, `@radix-ui/react-toggle` — MIT-licensed and in the same Radix family already vendored (`react-dialog`, `react-dropdown-menu`, `react-separator`), so no new licence/security surface of note. Justified by a concrete requirement (the requested control + correct two-state a11y).

**Alternatives considered**:
- *Compose the existing `Button` primitive (no new dependency)* — viable and lighter, but the user specifically requested a shadcn toggle, and `Toggle` provides correct `aria-pressed` two-state semantics that a plain button would otherwise need to emulate by hand.
- *A two-position switch / segmented control* — rejected: the user asked for a single icon toggle; a switch adds markup and a second label without benefit at two states.
- *Putting theme behaviour directly in a `components/ui/` component* — rejected: a theme toggle is application-specific (it knows about `useTheme`), so per Principle VI only the generic, theme-unaware `Toggle` belongs in the primitives layer; `ThemeToggle` belongs in the feature folder.

---

## Decision 8: jsdom `matchMedia` mock in test setup

**Decision**: Add a minimal `window.matchMedia` mock (default `matches: false`) to `src/tests/setup.ts`.

**Rationale**: jsdom does not implement `matchMedia`; the resolution helper calls it, so without a mock provider/storage tests would throw. Mocking once in shared setup avoids per-test boilerplate; individual tests override `matches` when they need to assert system-preference behaviour.

**Alternatives considered**:
- *Mock per test file* — rejected: repetitive; a shared default is cleaner and tests can still override locally.

---

## Summary of resolved unknowns

| Question | Resolution |
|----------|------------|
| How to switch themes? | Toggle `.dark` class on `<html>` (existing spec-003 mechanism) |
| Where to persist? | localStorage key `theme` = `"dark"`/`"light"` (user direction) |
| How to avoid flash? | Blocking inline script in `index.html` head, before React |
| First-load default? | stored → `prefers-color-scheme` → flagship dark |
| Follow live system changes? | No — explicit/stored choice is authoritative |
| State management tool? | React context (`ThemeProvider`/`useTheme`), not TanStack Query |
| Control shape? | Single-icon shadcn `Toggle` (Sun/Moon) in `DashboardHeader` |
| New dependencies? | One — `@radix-ui/react-toggle` (backs the shadcn `Toggle` primitive) |
