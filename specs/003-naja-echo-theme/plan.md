# Implementation Plan: Naja Echo Application Theme

**Branch**: `003-naja-echo-theme` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-naja-echo-theme/spec.md`

## Summary

Turn the five Naja Echo brand colors into a complete semantic UI palette for both a flagship **dark** theme (default) and a polished **light** theme, exposed as shadcn/ui-compatible CSS variables plus brand-specific tokens. Feature components consume semantic Tailwind utilities (`bg-background`, `text-foreground`, `bg-primary`, `bg-card`, `border-border`, `ring-ring`, …) instead of raw hex values.

**Technical approach**: The frontend is **Tailwind CSS v4** (CSS-first config via `@tailwindcss/vite`), React 19, with hand-authored shadcn-style primitives under `src/components/ui/`. Currently `src/index.css` contains only `@import "tailwindcss";` — **no theme tokens are defined**, so existing components that already reference semantic tokens (`card.tsx`, `alert.tsx`, `avatar.tsx`) render unstyled, and `button.tsx` falls back to raw palette colors (`bg-indigo-600`, `bg-red-600`). The plan defines the full token set in `index.css` using Tailwind v4's `@theme inline` + a `dark` custom variant, makes dark the default via `<html class="dark">`, re-points the Button variants to semantic tokens, and migrates the handful of feature pages off non-semantic classes (`bg-gray-50`, `text-gray-500`).

**No API contract changes required.** This is a frontend-only design-system feature: no new or changed backend HTTP behaviour, no OpenAPI changes, no backend changes.

## Technical Context

**Language/Version**: TypeScript 5.8 (strict), React 19

**Primary Dependencies**: Tailwind CSS v4 (`tailwindcss` + `@tailwindcss/vite`), shadcn-style primitives on Radix UI (`@radix-ui/react-avatar`, `@radix-ui/react-alert-dialog`), `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react`

**Storage**: N/A (no persistence; theme is static CSS)

**Testing**: Vitest + React Testing Library + jsdom; MSW for API mocking. `npm run test:run`, typecheck via `tsc -b` (part of `npm run build`), lint via `npm run lint` (ESLint)

**Target Platform**: Modern evergreen browsers (SPA served by Vite)

**Project Type**: Web application — frontend only for this feature (`frontend/` workspace)

**Performance Goals**: No runtime cost beyond static CSS variables; no measurable bundle/runtime regression

**Constraints**: WCAG AA minimum contrast for body text and interactive elements; dark theme is the default experience; no new dependencies; no theme-switcher UI

**Scale/Scope**: ~19 semantic tokens × 2 themes + 4 brand tokens; 4 existing UI primitives (Button, Card, Alert, Avatar); ~5 feature pages/components to migrate off non-semantic classes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. API-Contract-First | ✅ PASS | **No API contract changes required** — recorded explicitly. UI-only feature introduces no new/changed backend HTTP behaviour. |
| II. Test-First / TDD | ✅ PASS | Existing frontend tests must continue to pass (SC-006). New lightweight token-render test added only if it follows the existing Vitest/RTL pattern; no new test infra. |
| III. Frontend/Backend Separation | ✅ PASS | Frontend-only; no backend or DB touch; no shared types changed. |
| IV. Simplicity / YAGNI | ✅ PASS | Tokens defined once in global CSS. No theme switcher, no new components beyond what already exists. Components named in the brief but absent from the repo (Input, Label, Dropdown/Menu, Sheet/Dialog, Badge) are **not** created — the token system makes them theme-ready when a real feature needs them. |
| V. Observability | ✅ N/A | No backend/log surface in this feature. |
| VI. Modular Monolith + Clean Architecture | ✅ PASS | Generic primitives stay in `components/ui/` and remain application-agnostic (only re-pointed to semantic tokens). No app-specific behaviour added to `ui/`. Route components stay thin. Raw hex centralized in global CSS only. |

**Frontend Conventions check**: shadcn/ui ownership respected (primitives are owned source, customized only for theme compatibility); no API client/type changes; no TanStack Query or form changes; no dashboard-shell or navigation redesign. ✅ PASS

**Gate result: PASS.** No violations; Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/003-naja-echo-theme/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output — the token model (dark + light palettes)
├── quickstart.md        # Phase 1 output — validation/run guide
├── contracts/
│   └── theme-tokens.md  # Phase 1 output — the semantic-token contract exposed to feature devs
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
frontend/
├── index.html                         # add class="dark" to <html> (dark is default)
├── tailwind.config.ts                 # vestigial under v4; left as-is unless a plugin is needed
├── src/
│   ├── index.css                      # PRIMARY CHANGE: define all tokens, @theme inline, dark variant
│   ├── App.css                        # leftover Vite starter CSS — out of scope (not imported by app shell)
│   ├── components/
│   │   ├── ui/                        # generic primitives — re-point to semantic tokens only
│   │   │   ├── button.tsx             # CHANGE: bg-indigo-600/bg-red-600 → bg-primary/bg-destructive; add secondary variant
│   │   │   ├── card.tsx               # already semantic — verify renders correctly with new tokens
│   │   │   ├── alert.tsx              # already semantic — verify destructive pairing
│   │   │   └── avatar.tsx             # already semantic (bg-muted) — verify
│   │   └── shared/                    # (none required by this feature)
│   └── features/
│       ├── auth/pages/LandingPage.tsx        # CHANGE: bg-gray-50 → bg-background, text-gray-500 → text-muted-foreground
│       ├── auth/pages/AuthErrorPage.tsx      # CHANGE: bg-gray-50 → bg-background
│       ├── auth/pages/AuthCallbackPage.tsx   # CHANGE: text-gray-500 → text-muted-foreground
│       └── dashboard/pages/DashboardPage.tsx # CHANGE: bg-gray-50 → bg-background, text-gray-500 → text-muted-foreground
└── docs/ or README.md                 # OPTIONAL: short theme usage note (token guidance)
```

**Structure Decision**: Frontend-only change confined to the `frontend/` workspace. The single source of truth for all raw hex values is `src/index.css`. Generic primitives in `components/ui/` are re-pointed to semantic tokens but gain no application-specific behaviour (Principle VI). No `components/shared/` additions are needed.

## Key Technical Decisions (Tailwind v4)

1. **Token definition (CSS-first)**: Define CSS custom properties in `:root` (light theme) and `.dark` (dark theme) inside `src/index.css`, then register them as Tailwind color utilities via `@theme inline { --color-background: var(--background); … }`. This is the Tailwind v4 idiom — the near-empty `tailwind.config.ts` `theme.extend` is **not** used for colors in v4.
2. **Dark mode mechanism**: Add `@custom-variant dark (&:is(.dark *));` and apply `class="dark"` on `<html>` in `index.html` so the **dark theme is the default**. Light tokens live in `:root` so the system is ready for a future class-based switcher without one being built now.
3. **Brand tokens**: Expose `brand`, `brand-foreground`, `brand-muted`, `brand-muted-foreground` as first-class Tailwind colors (`bg-brand`, `text-brand-muted`, …) for gold usage — especially muted gold on light surfaces.
4. **Button remediation**: Re-point `default` → `bg-primary text-primary-foreground`, `destructive` → `bg-destructive text-destructive-foreground`, and add a `secondary` variant (`bg-secondary text-secondary-foreground`). This is the only behavioural touch to a `ui/` primitive and is purely theme compatibility.
5. **Base layer defaults**: Add a small `@layer base` rule applying `bg-background text-foreground` to `body` and `border-border`/`ring` defaults so unstyled surfaces inherit the theme.

## Complexity Tracking

No constitution violations. Section intentionally empty.
