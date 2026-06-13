# Quickstart: Validate the Naja Echo Theme

Validation/run guide proving the theme works end-to-end. Implementation details live in `tasks.md`; this file is how you confirm the result.

## Prerequisites

- Node + npm installed
- From `frontend/`: `npm install` (already present in this repo)

## Commands

```bash
cd frontend

# 1. Typecheck + production build (also runs tsc -b)
npm run build

# 2. Lint
npm run lint

# 3. Unit/component tests (must stay green — SC-006)
npm run test:run

# 4. Run the app for visual inspection
npm run dev   # http://localhost:5173
```

## Expected outcomes

| Check | Expected result | Maps to |
|-------|-----------------|---------|
| `npm run build` | Succeeds; no TypeScript errors | SC-006 |
| `npm run lint` | No new errors introduced by theme changes | — |
| `npm run test:run` | All existing tests pass | SC-006, FR-014 |
| App loads | `<html>` has `class="dark"`; page background is deep teal-black `#0E2226`; body text warm/readable | SC-001, FR-003 |

## Visual validation scenarios

Run `npm run dev` and verify on the **dark** theme (default):

1. **Background & text** — Landing page background is deep teal-black; heading/body text is readable (warm light). *(FR-003, SC-001)*
2. **Primary action** — "Sign in with Discord" button is **gold** with **dark** text. *(FR-004, SC-002)*
3. **Card** — The card surface is slightly lifted from the background, with a subtle teal border and themed foreground text. *(FR-001)*
4. **Focus ring** — Tab to the sign-in button and any input: a visible **gold** focus ring appears and does not blend in. *(FR-010, SC-003)*
5. **Destructive** — Visit `/auth/error` (e.g. `?reason=oauth_error`): the Alert uses an accessible destructive color readable on dark. *(FR-011, SC-007)*

To validate the **light** theme, temporarily remove `class="dark"` from `<html>` (or add `.dark` removal in devtools) and confirm:

6. **Light background** — Warm off-white background, deep teal-black text. *(FR-002, light Story 2)*
7. **Light primary action** — Primary button is **teal** `#204F59` with light text — **not gold**. *(FR-005, SC-002)*
8. **Muted gold** — Any gold text/accent uses muted gold `#8C7326`, not bright gold. *(FR-006, FR-013)*
9. **Light focus ring** — Tab through controls: visible **teal** focus ring. *(FR-010, SC-003)*

## Developer-experience validation

10. **Semantic-only component** — Confirm no feature component contains raw hex or palette classes:

```bash
cd frontend
# Should return NOTHING (no raw hex utilities, no stray palette colors in feature/ui code)
grep -rnE "bg-\[#|text-\[#|border-\[#|bg-(indigo|gray|red|blue)-[0-9]|text-gray-[0-9]" src/components src/features
```

A nil result satisfies FR-007 and SC-004 (developers build with semantic tokens; raw hex lives only in `src/index.css`).

## Token reference

See `contracts/theme-tokens.md` for the available utilities and `data-model.md` for the full dark/light value tables.
