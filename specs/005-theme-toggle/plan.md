# Implementation Plan: Dark/Light Theme Toggle

**Branch**: `005-theme-toggle` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-theme-toggle/spec.md`

## Summary

Add a **single-icon theme toggle** that lets the user switch the application between its existing dark and light themes, persisting the choice in the browser's **localStorage** so it survives reloads and new sessions. The themes themselves already exist (spec 003): light is the `:root` default and dark is activated by a `.dark` class on the `<html>` element, with a Tailwind v4 `@custom-variant dark`. This feature adds the *control* and the *state management* around that existing token system — it does not redesign or add theme colors.

The toggle is a single-icon **shadcn `Toggle`** (sun/moon) rendered in the dashboard shell header (spec 004) next to the account menu. A small `ThemeProvider` holds the current theme, writes it to localStorage, and toggles the `.dark` class on `document.documentElement`. A tiny **blocking inline script** in `index.html` applies the stored (or system-preferred) theme *before* React renders, eliminating the flash-of-incorrect-theme called out in SC-005.

**No API contract changes required.** This is a frontend-only feature: no new or changed backend HTTP behaviour, no OpenAPI changes, no backend changes. Persistence is client-side localStorage only (per the user's explicit direction), so no user-profile/backend persistence is in scope.

**Technical approach**: React 19 + TypeScript (strict) on Vite, Tailwind CSS v4 semantic tokens. A new generic `Toggle` primitive is added under `components/ui/toggle.tsx` — the standard shadcn Toggle (a pressed/unpressed button) wrapping `@radix-ui/react-toggle`, authored in the repo's existing `cva` + `cn` + `forwardRef` house style and kept application-agnostic. A new `features/theme/` folder owns the cross-cutting theme concern: `ThemeProvider` (context), `useTheme` (hook), `themeStorage` (localStorage read/write/validation helper mirroring the inline script), and `ThemeToggle` (the application-specific control that composes the generic `Toggle` primitive with `lucide-react` Sun/Moon icons and `useTheme`). `ThemeProvider` wraps the app in `App.tsx`; `ThemeToggle` is placed in the existing `DashboardHeader`. One new dependency: `@radix-ui/react-toggle` (MIT, same Radix family already in use).

## Technical Context

**Language/Version**: TypeScript ~5.8 (strict), React 19.2

**Primary Dependencies**: `react-router-dom` v7, Tailwind CSS v4 (`@tailwindcss/vite`), `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react` (Sun/Moon icons — already installed), `@tanstack/react-query` v5. **One new dependency**: `@radix-ui/react-toggle` (MIT-licensed, same Radix family as the already-installed `@radix-ui/react-dialog`/`-dropdown-menu`/`-separator`), backing the new shadcn `Toggle` primitive.

**Storage**: Browser `localStorage` (key `theme`, value `"dark"` | `"light"`). No backend/server persistence.

**Testing**: Vitest + React Testing Library + jsdom; `@testing-library/user-event` for interaction. `npm run test:run`, typecheck via `tsc -b` (part of `npm run build`), lint via `npm run lint`. A `window.matchMedia` mock is added to `src/tests/setup.ts` (jsdom does not implement it).

**Target Platform**: Modern evergreen browsers (SPA served by Vite)

**Project Type**: Web application — frontend only for this feature (`frontend/` workspace)

**Performance Goals**: Theme switch applies in well under 100 ms (SC-001) — a single class toggle on `<html>`. No flash of incorrect theme on first paint (SC-005), guaranteed by the blocking inline script.

**Constraints**: WCAG AA contrast in both themes (SC-003) — inherited from the existing spec-003 token palettes, verified, not redesigned here. Toggle fully keyboard-operable and screen-reader labelled (SC-004). Semantic tokens only; no raw hex in new components. Preserve existing auth/session and dashboard-shell behaviour and tests. No backend/OpenAPI changes.

**Scale/Scope**: 1 new generic `Toggle` primitive (`components/ui/toggle.tsx`) + 1 new dependency, 1 theme context/provider, 1 hook, 1 storage helper, 1 `ThemeToggle` component, 1 inline boot script in `index.html`, 1 wiring change in `App.tsx`, 1 placement change in `DashboardHeader.tsx`, 1 test-setup addition (`matchMedia` mock). Frontend tests for: storage read/write/validation, provider toggle behaviour + class/localStorage side effects + initial resolution, toggle render/click/keyboard/aria.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. API-Contract-First | ✅ PASS | **No API contract changes required** — recorded explicitly. UI-only feature; introduces no new/changed backend HTTP behaviour, no OpenAPI edits. Persistence is client-side localStorage only. |
| II. Test-First / TDD | ✅ PASS | Frontend tests added for all new user-facing behaviour: storage helper, provider toggle/persist/initial-resolution, toggle component render/click/keyboard/aria. Existing dashboard-shell and auth tests must stay green. Uses existing Vitest/RTL stack; adds only a `matchMedia` mock to test setup. |
| III. Frontend/Backend Separation | ✅ PASS | Frontend-only; no backend, DB, or shared-type changes. No API types touched. |
| IV. Simplicity / YAGNI | ✅ PASS | Two states (dark/light) only — no theme registry, no "system" tri-state setting UI, no per-user backend persistence. One new dependency (`@radix-ui/react-toggle`), justified by a concrete requirement: it backs the shadcn `Toggle` the user asked for and gives correct `aria-pressed` two-state semantics for free (FR-005/FR-007). The provider is the minimal context needed to share theme state with the toggle. |
| V. Observability | ✅ N/A | No backend/log surface in this feature. |
| VI. Modular Monolith + Clean Architecture | ✅ PASS | The new `Toggle` is a generic, application-agnostic primitive in `components/ui/` (no theme behaviour embedded). `ThemeToggle` is the application-specific composition (it knows about `useTheme`) → lives in the `features/theme/` folder, **not** `components/ui/`, and composes the generic `Toggle`. The theme concern is genuinely cross-cutting (consumed app-wide), justifying a dedicated feature folder. No raw hex; semantic tokens only. `DashboardHeader` stays thin — it just renders `<ThemeToggle />`. |

**Frontend Conventions check**:
- shadcn/ui ownership respected — the new `Toggle` primitive in `components/ui/` is generic (no theme behaviour embedded); the feature-specific `ThemeToggle` composes it from `features/theme/`. ✅
- API client/type generation — no API types touched. ✅
- TanStack Query — not applicable; theme is client UI state, not server state, so it correctly does **not** use TanStack Query. ✅
- Forms — none in this feature. ✅
- Dashboard shell & navigation — the toggle renders into the shell header region the shell already owns; it does not re-implement shell regions. ✅

**Gate result: PASS.** No violations. Complexity Tracking not required.

**Post-Design re-check (after Phase 1)**: ✅ PASS — design artifacts (data-model, UI contract, quickstart) introduce no new violations. The single new dependency, `@radix-ui/react-toggle`, is MIT-licensed, in the already-approved Radix family, and justified by the concrete requirement for a shadcn `Toggle` control with correct two-state `aria-pressed` semantics. The blocking inline script in `index.html` is the standard, framework-agnostic no-FOUC pattern and touches no backend or contract surface.

## Project Structure

### Documentation (this feature)

```text
specs/005-theme-toggle/
├── plan.md              # This file
├── research.md          # Phase 0 output — decisions & rationale
├── data-model.md        # Phase 1 output — Theme state model & resolution rules
├── quickstart.md        # Phase 1 output — validation/run guide
├── contracts/
│   └── theme-toggle.md  # Phase 1 output — UI contract (provider API, hook, toggle props, storage)
├── checklists/
│   └── requirements.md  # Spec quality checklist (created by /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
frontend/
├── index.html                              # CHANGE — add blocking no-FOUC inline script in <head>;
│                                           #          keep class="dark" as the pre-resolution default
├── src/
│   ├── App.tsx                             # CHANGE — wrap <AppRouter/> with <ThemeProvider>
│   ├── components/
│   │   └── ui/
│   │       ├── button.tsx                  # EXISTS — unchanged
│   │       └── toggle.tsx                  # NEW — generic shadcn Toggle primitive (wraps @radix-ui/react-toggle)
│   ├── features/
│   │   ├── theme/                          # NEW — cross-cutting theme concern
│   │   │   ├── ThemeProvider.tsx           # NEW — context provider: holds theme, toggles .dark, persists
│   │   │   ├── ThemeProvider.test.tsx      # NEW — toggle/persist/initial-resolution tests
│   │   │   ├── useTheme.ts                  # NEW — hook consuming ThemeContext
│   │   │   ├── themeStorage.ts             # NEW — localStorage read/write/validate + resolve helper
│   │   │   ├── themeStorage.test.ts        # NEW — storage read/write/validation/fallback tests
│   │   │   ├── ThemeToggle.tsx             # NEW — single-icon shadcn Toggle (Sun/Moon) bound to useTheme
│   │   │   └── ThemeToggle.test.tsx        # NEW — render/click/keyboard/aria tests
│   │   └── dashboard/
│   │       └── components/
│   │           └── DashboardHeader.tsx     # CHANGE — render <ThemeToggle /> beside <AccountMenu />
│   └── tests/
│       └── setup.ts                        # CHANGE — add window.matchMedia mock for jsdom
└── (no backend / OpenAPI / contract changes)
```

**Structure Decision**: Frontend-only change confined to the `frontend/` workspace. The generic shadcn `Toggle` primitive lives in `components/ui/` and is kept application-agnostic. The theme concern is cross-cutting (it themes the whole app, authenticated or not), so it gets its own `features/theme/` folder owning the provider, hook, storage helper, and the `ThemeToggle` control. `ThemeToggle` is application-specific composition and therefore lives in the feature folder — **not** in `components/ui/` (Principle VI) — and composes the generic `Toggle` primitive. The existing `DashboardHeader` is the toggle's placement (the shell already owns the header region); the change there is a single rendered element, keeping the route/shell components thin.

## Key Technical Decisions

1. **Reuse the existing `.dark`-class token system (don't reinvent themes)**: Spec 003 already defines light as the `:root` default and dark via `.dark` on `<html>`, with `@custom-variant dark (&:is(.dark *))` in `index.css`. This feature only adds the *control* and *state* around that mechanism. Switching theme = adding/removing the `.dark` class on `document.documentElement`. No CSS color work beyond what already exists; SC-003 (WCAG AA) is satisfied by the existing palettes and verified, not re-authored.

2. **localStorage persistence (per user direction)**: The selected theme is stored under the key `theme` with value `"dark"` or `"light"`. No backend/user-profile persistence — the user explicitly chose localStorage, and Principle IV (YAGNI) discourages adding server persistence not asked for. This supersedes the spec's earlier "or user profile" assumption.

3. **No-FOUC blocking inline script (SC-005)**: A tiny synchronous `<script>` in `index.html`'s `<head>` runs before stylesheet/React paint. It resolves the theme (stored value → system `prefers-color-scheme` → flagship default) and sets/clears `.dark` on `<html>` immediately, so the first paint is already in the correct theme — no flash. `index.html` keeps `class="dark"` as the pre-script default so even with JS disabled the flagship dark theme shows.

4. **Theme resolution order**: (1) explicit stored choice in localStorage; (2) if none, the OS/browser `prefers-color-scheme` media query; (3) if neither resolves to light, the **flagship dark default** (consistent with spec 004's "dark theme is the default" and the shipped `class="dark"`). This refines the spec's "fall back to light" wording to honor the established flagship-dark default; explicit user and system signals still take precedence as the spec intends. Documented in research.md.

5. **Explicit choice wins over later system changes (edge case)**: Once the user toggles, the choice is written to localStorage and is authoritative on subsequent loads. The app does not subscribe to live `prefers-color-scheme` changes — the system preference is only consulted as the first-load default when no stored choice exists. This keeps behaviour predictable and matches the spec edge case ("respect the user's explicit in-app choice over system changes").

6. **`ThemeProvider` + `useTheme` for shared state**: A React context provider holds `theme` and exposes `toggleTheme()` / `setTheme()`. On mount it initializes from the already-resolved DOM/localStorage state (no second flash). Whenever theme changes it (a) toggles the `.dark` class and (b) writes localStorage. Theme is **client UI state, not server state**, so it deliberately does not use TanStack Query. The provider wraps `<AppRouter/>` in `App.tsx` so the whole app (including unauthenticated pages) is themed consistently.

7. **Single-icon shadcn `Toggle` (per user direction)**: The control is the standard shadcn `Toggle` — a new generic primitive `components/ui/toggle.tsx` wrapping `@radix-ui/react-toggle`, authored in the repo's `cva` + `cn` + `forwardRef` house style with `variant`/`size` (incl. an `icon` size) using semantic tokens only. The feature-specific `ThemeToggle` composes it: `pressed={theme === 'dark'}` with `onPressedChange` calling `setTheme`, so "pressed" means dark mode is on. It shows a Moon icon in light theme (action: switch to dark) and a Sun icon in dark theme (action: switch to light) — the icon change is the visual feedback for the active theme (FR-005). Accessibility (FR-007, SC-004): Radix Toggle renders a native `<button>` that is keyboard-operable (Enter/Space), focusable with a visible focus ring, and **automatically manages `aria-pressed`** to reflect the two-state choice (which the spec's US3 explicitly calls for); an `aria-label` describing the action (e.g. "Switch to light theme") is added and updates with state.

8. **Toggle placement**: The toggle lives in `DashboardHeader`, beside `AccountMenu`, matching the spec assumption (header/account area) and the shell's ownership of the header region. The `ThemeProvider` is app-global, so theme still applies to unauthenticated pages even though the toggle control itself is in the authenticated shell. Adding a second toggle to the landing/auth pages is out of scope (YAGNI) and can be added later by rendering the same `ThemeToggle`.

9. **jsdom `matchMedia` mock**: jsdom does not implement `window.matchMedia`, which the resolution helper calls. A minimal mock is added once in `src/tests/setup.ts` (defaulting `matches: false`), so existing and new tests run without per-test boilerplate. Tests that need a specific system preference override it locally.

## Complexity Tracking

No constitution violations. Section intentionally empty.
