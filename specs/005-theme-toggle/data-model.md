# Phase 1 Data Model: Dark/Light Theme Toggle

This feature has no backend entities and no database. The only "data" is a single client-side preference value. This document defines that value, where it lives, and the rules that govern reading/resolving/writing it.

## Entity: Theme Preference

The user's selected color scheme. Pure client UI state.

| Aspect | Definition |
|--------|------------|
| **Type** | `Theme = "dark" \| "light"` (TypeScript string-literal union) |
| **Storage location** | Browser `localStorage`, key `theme` |
| **Stored values** | The string `"dark"` or `"light"` |
| **Absent state** | Key not present → no explicit choice yet → resolved from system/default |
| **Applied representation** | Presence/absence of the `dark` class on `document.documentElement` (`<html>`) |
| **Lifecycle** | Created/updated when the user activates the toggle; read on every app load (inline script + provider init) |
| **Ownership** | `features/theme/` — `themeStorage.ts` owns read/write/validate; `ThemeProvider` owns in-memory state and DOM/storage side effects |

### Constants

| Name | Value | Where |
|------|-------|-------|
| `STORAGE_KEY` | `"theme"` | `themeStorage.ts` (and mirrored literally in the `index.html` inline script) |
| `DARK_CLASS` | `"dark"` | applied to `document.documentElement` |
| Default theme | `"dark"` (flagship) | fallback in resolution chain |

## Validation Rules

- **VR-1**: A value read from localStorage is accepted only if it is exactly `"dark"` or `"light"`. Any other value (missing, empty, corrupted, legacy) is treated as *absent* and triggers resolution (see below). Satisfies the spec "corrupted preference" edge case.
- **VR-2**: Only `"dark"` or `"light"` are ever written to localStorage. The provider never persists a transient/unknown value.
- **VR-3**: The DOM is always consistent with the in-memory theme: theme `"dark"` ⇔ `dark` class present on `<html>`; theme `"light"` ⇔ class absent.

## Theme Resolution (first load, no explicit choice)

Resolution order when there is **no valid stored value** (see research Decision 4):

1. **Stored value** — if localStorage holds a valid `"dark"`/`"light"`, use it (this branch is the explicit-choice case and short-circuits resolution).
2. **System preference** — else if `window.matchMedia('(prefers-color-scheme: light)').matches`, resolve to `"light"`.
3. **Flagship default** — else resolve to `"dark"`.

```text
storedTheme valid?
 ├─ yes → use storedTheme
 └─ no  → prefers-color-scheme: light?
           ├─ yes → "light"
           └─ no  → "dark"   (flagship default)
```

This same logic runs in two places, kept in sync intentionally:
- The **blocking inline script** in `index.html` (runs before React; prevents FOUC).
- `themeStorage.resolveInitialTheme()` (used by `ThemeProvider` to initialize in-memory state to match what the inline script already applied).

## State Transitions

| From | Event | To | Side effects |
|------|-------|----|--------------|
| `dark` | user activates toggle | `light` | remove `dark` class from `<html>`; write `"light"` to localStorage |
| `light` | user activates toggle | `dark` | add `dark` class to `<html>`; write `"dark"` to localStorage |
| (page load) | app boot | resolved theme | inline script applies class pre-paint; provider initializes matching in-memory state (no class flip, no flash) |

There are exactly two states; the toggle flips between them. No intermediate/loading state exists (the value is read synchronously).

## Relationships

None. This is an isolated client preference with no relationship to backend entities, the session, or other features. It is intentionally **not** part of the user profile or any API payload (research Decision 2).
