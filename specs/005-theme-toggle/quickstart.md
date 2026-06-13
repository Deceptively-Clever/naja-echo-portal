# Quickstart & Validation: Dark/Light Theme Toggle

A run/validation guide proving the theme toggle works end-to-end. Implementation details live in `tasks.md`; component shapes live in [contracts/theme-toggle.md](./contracts/theme-toggle.md).

## Prerequisites

- Node + the `frontend/` workspace dependencies installed (`npm install` in `frontend/`).
- One new dependency is added for this feature: `@radix-ui/react-toggle` (run `npm install` after pulling the branch so it is present).
- No backend is needed to validate theming (the toggle is client-only). Sign-in is only needed to reach the dashboard header where the toggle lives.

## Run the app

```bash
cd frontend
npm run dev
```

Open the served URL. Sign in to reach the dashboard (the toggle is in the dashboard header, beside the account menu).

## Manual validation scenarios

### US1 — Switching themes (P1)

1. With the app in dark theme, click the theme icon (Sun) in the header.
   - **Expect**: the entire UI switches to light theme instantly (<100 ms); the icon becomes a Moon.
2. Click again.
   - **Expect**: the UI returns to dark theme instantly; the icon becomes a Sun.
3. Navigate between dashboard pages (Home / Profile / Settings) after switching.
   - **Expect**: every page renders in the selected theme consistently.

### US2 — Persistence (P1)

1. Switch to light theme, then reload the page (F5).
   - **Expect**: the app loads directly in light theme with **no flash** of dark first (SC-005).
2. Close the tab and reopen the app URL.
   - **Expect**: light theme is still applied.
3. In DevTools → Application → Local Storage, confirm key `theme` = `light` (or `dark`).
4. Clear the `theme` key and reload with the OS set to light mode.
   - **Expect**: app respects the system preference and loads light. With OS dark (or no preference), it loads dark (flagship default).
5. Set the `theme` key to a garbage value (e.g. `purple`) and reload.
   - **Expect**: app ignores it and resolves via system/default — no crash (corrupted-preference edge case).

### US3 — Accessibility (P2)

1. Using only the keyboard, Tab to the theme toggle.
   - **Expect**: it receives focus with a visible focus ring.
2. Press Enter (and separately, Space) while focused.
   - **Expect**: the theme switches on each activation.
3. Inspect the button's `aria-label`.
   - **Expect**: it reflects the action and current state — `"Switch to light theme"` in dark mode, `"Switch to dark theme"` in light mode.
4. (Optional) With a screen reader, navigate to the control.
   - **Expect**: announced as a button with the action label above.

## Automated checks

```bash
cd frontend
npm run test:run     # unit/component tests (theme storage, provider, toggle) + existing suites stay green
npm run lint         # eslint
npm run build        # tsc -b typecheck + vite build
```

**Expected**: all new theme tests pass; all pre-existing dashboard-shell and auth tests remain green; typecheck and lint clean.

## Done-when

- Toggle switches the whole UI between dark and light on click and via keyboard.
- Choice persists across reloads and new sessions via localStorage (`theme` key).
- No flash of incorrect theme on load.
- Both themes remain WCAG AA legible (inherited from the existing palettes).
- New tests pass and existing tests stay green; lint and build clean.
