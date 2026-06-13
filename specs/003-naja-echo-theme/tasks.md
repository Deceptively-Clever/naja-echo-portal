# Tasks: Naja Echo Application Theme

**Input**: Design documents from `specs/003-naja-echo-theme/`

**Branch**: `003-naja-echo-theme` | **Plan**: [plan.md](./plan.md) | **Spec**: [spec.md](./spec.md)

**No API contract changes required.** Frontend-only feature.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no shared-file dependency)
- **[Story]**: User story this task belongs to (US1–US4)
- Exact file paths in every description

## Path Conventions

All paths are relative to the `frontend/` workspace root (i.e. `frontend/src/index.css`).

---

## Phase 1: Setup

**Purpose**: Confirm the Tailwind v4 build is healthy before any token changes; no structural changes needed.

- [x] T001 Confirm `npm run build` and `npm run test:run` pass clean from `frontend/` before any edits (establishes baseline)
- [x] T002 [P] Confirm `tailwind.config.ts` requires no changes for v4 CSS-first token approach (`theme.extend` is empty and the `@tailwindcss/vite` plugin handles content scanning automatically)

---

## Phase 2: Foundational — Define the Full Token System

**Purpose**: Define all semantic CSS tokens in `src/index.css`. This single file is the only place raw hex values live. Every subsequent phase depends on these tokens being present.

**⚠️ CRITICAL**: No user story can be visually validated until this phase is complete.

- [x] T003 Add `@custom-variant dark (&:is(.dark *));` to `frontend/src/index.css` directly after the `@import "tailwindcss";` line to enable class-based dark mode
- [x] T004 Add `:root` block to `frontend/src/index.css` defining all **light theme** token values as CSS custom properties: `--background: #F7F4EA`, `--foreground: #102A30`, `--card: #FFFFFF`, `--card-foreground: #102A30`, `--popover: #FFFFFF`, `--popover-foreground: #102A30`, `--primary: #204F59`, `--primary-foreground: #FFFFFF`, `--secondary: #ECE7D8`, `--secondary-foreground: #102A30`, `--accent: #265D73`, `--accent-foreground: #FFFFFF`, `--muted: #ECE7D8`, `--muted-foreground: #5C6870`, `--border: #D9D1B8`, `--input: #CFC6AC`, `--ring: #265D73`, `--destructive: #B42318`, `--destructive-foreground: #FFFFFF`, `--brand: #CCAC31`, `--brand-foreground: #102A30`, `--brand-muted: #8C7326`, `--brand-muted-foreground: #FFFFFF`
- [x] T005 Add `.dark` override block to `frontend/src/index.css` defining all **dark theme** token values: `--background: #0E2226`, `--foreground: #F4F0E3`, `--card: #132D33`, `--card-foreground: #F4F0E3`, `--popover: #132D33`, `--popover-foreground: #F4F0E3`, `--primary: #CCAC31`, `--primary-foreground: #0E2226`, `--secondary: #204F59`, `--secondary-foreground: #FFFFFF`, `--accent: #265D73`, `--accent-foreground: #FFFFFF`, `--muted: #193840`, `--muted-foreground: #A9B6B8`, `--border: #2B4A52`, `--input: #2B4A52`, `--ring: #CCAC31`, `--destructive: #F97066`, `--destructive-foreground: #0E2226`, `--brand: #CCAC31`, `--brand-foreground: #0E2226`, `--brand-muted: #8C7326`, `--brand-muted-foreground: #F4F0E3`
- [x] T006 Add `@theme inline` block to `frontend/src/index.css` registering all tokens as Tailwind color utilities: `--color-background: var(--background)`, `--color-foreground: var(--foreground)`, `--color-card: var(--card)`, `--color-card-foreground: var(--card-foreground)`, `--color-popover: var(--popover)`, `--color-popover-foreground: var(--popover-foreground)`, `--color-primary: var(--primary)`, `--color-primary-foreground: var(--primary-foreground)`, `--color-secondary: var(--secondary)`, `--color-secondary-foreground: var(--secondary-foreground)`, `--color-accent: var(--accent)`, `--color-accent-foreground: var(--accent-foreground)`, `--color-muted: var(--muted)`, `--color-muted-foreground: var(--muted-foreground)`, `--color-border: var(--border)`, `--color-input: var(--input)`, `--color-ring: var(--ring)`, `--color-destructive: var(--destructive)`, `--color-destructive-foreground: var(--destructive-foreground)`, `--color-brand: var(--brand)`, `--color-brand-foreground: var(--brand-foreground)`, `--color-brand-muted: var(--brand-muted)`, `--color-brand-muted-foreground: var(--brand-muted-foreground)`
- [x] T007 Add `@layer base` block to `frontend/src/index.css` applying `background-color: var(--background)` and `color: var(--foreground)` to `body`, `border-color: var(--border)` as `*` border default, and `outline-color: var(--ring)` as the default outline color
- [x] T008 Run `npm run build` from `frontend/` to confirm all new token utilities compile without TypeScript or Tailwind errors

**Checkpoint**: All semantic Tailwind utilities (`bg-primary`, `text-foreground`, `bg-card`, `border-border`, `ring-ring`, `bg-brand`, `text-brand-muted`, …) are now available for use in components.

---

## Phase 3: User Story 1 — Dark Theme as the Primary Application Experience (P1) 🎯 MVP

**Goal**: Every page surface renders in the flagship dark theme — deep teal-black background, gold primary actions, readable warm text — using only semantic tokens.

**Independent Test**: Load the app (default dark), inspect: background is `#0E2226`, "Sign in with Discord" button is gold, card surface is raised dark teal, body text is warm and readable, no `bg-gray-*` or `bg-indigo-*` remains in any rendered element.

- [x] T009 [US1] Apply `class="dark"` to the `<html>` element in `frontend/index.html` so dark is the default application experience
- [x] T010 [US1] Update the `default` variant in `frontend/src/components/ui/button.tsx` from `bg-indigo-600 text-white hover:bg-indigo-700` to `bg-primary text-primary-foreground hover:bg-primary/90`
- [x] T011 [US1] Update the `destructive` variant in `frontend/src/components/ui/button.tsx` from `bg-red-600 text-white hover:bg-red-700` to `bg-destructive text-destructive-foreground hover:bg-destructive/90`
- [x] T012 [P] [US1] Add `secondary` variant to `frontend/src/components/ui/button.tsx`: `bg-secondary text-secondary-foreground hover:bg-secondary/80`
- [x] T013 [P] [US1] Migrate `frontend/src/features/auth/pages/LandingPage.tsx`: replace `bg-gray-50` → `bg-background` on the `<main>` element and `text-gray-500` → `text-muted-foreground` on the subtitle paragraph
- [x] T014 [P] [US1] Migrate `frontend/src/features/auth/pages/AuthErrorPage.tsx`: replace `bg-gray-50` → `bg-background` on the `<main>` element
- [x] T015 [P] [US1] Migrate `frontend/src/features/auth/pages/AuthCallbackPage.tsx`: replace `text-gray-500` → `text-muted-foreground` on the loading paragraph
- [x] T016 [P] [US1] Migrate `frontend/src/features/dashboard/pages/DashboardPage.tsx`: replace `bg-gray-50` → `bg-background` on the `<main>` element and `text-gray-500` → `text-muted-foreground` on the welcome paragraph
- [x] T017 [US1] Visually verify in the browser (`npm run dev`): dark background renders, "Sign in with Discord" is gold, Card has a distinct dark-teal surface, Alert destructive variant is readable — confirms card.tsx/alert.tsx/avatar.tsx inherit tokens correctly with no component edits
- [x] T018 [US1] Run `npm run test:run` from `frontend/` and confirm all existing tests pass (SC-006 / FR-014)

**Checkpoint**: US1 fully functional — dark theme is the default, all pages use semantic tokens, all tests green.

---

## Phase 4: User Story 2 — Light Surfaces Are Readable and Professionally Styled (P2)

**Goal**: With `.dark` class removed from `<html>`, all surfaces use the warm off-white light palette; primary actions use secondary teal, not gold; gold accents use muted gold.

**Independent Test**: Temporarily remove `class="dark"` from `<html>` (devtools or `index.html` edit); confirm: background is warm off-white, primary button is teal, Card background is near-white with dark text, no gold body text.

- [x] T019 [US2] Visually verify light theme in browser: remove `class="dark"` from `frontend/index.html`, load app, confirm `background: #F7F4EA` (warm off-white), `foreground: #102A30` (deep teal-black), primary button is teal `#204F59` with white text — not gold
- [x] T020 [P] [US2] Visually verify light Card renders: white card surface, dark teal-black text, warm border — no dark backgrounds bleed through
- [x] T021 [P] [US2] Visually verify `bg-brand` and `text-brand-muted` utilities: render a test element with each class and confirm `bg-brand` is gold `#CCAC31` with dark foreground, `text-brand-muted` is muted gold `#8C7326` — ready for badge/logo use
- [x] T022 [US2] Restore `class="dark"` in `frontend/index.html` after light-theme validation

**Checkpoint**: US2 validated — light palette is polished and distinct from dark; muted gold available for accents.

---

## Phase 5: User Story 3 — Keyboard Focus States Are Visible (P2)

**Goal**: Visible focus rings appear on all interactive controls in both themes; they use the `ring` token (gold on dark, teal on light) and do not blend into either background.

**Independent Test**: Tab through the app (dark and light); confirm every focused Button, Link, and interactive element shows a distinct focus ring without CSS inspector needed.

- [x] T023 [US3] Verify the button base class in `frontend/src/components/ui/button.tsx` includes `focus-visible:ring-2 focus-visible:ring-ring` (already present per inspection); confirm no edits needed; if the class is missing, add it
- [x] T024 [US3] Manually tab through the landing page (dark theme): confirm the "Sign in with Discord" button shows a visible **gold** focus ring (`ring-ring` = `#CCAC31`) when focused
- [x] T025 [US3] Manually tab through the landing page (light theme, `class="dark"` temporarily removed): confirm the same button shows a visible **teal** focus ring (`ring-ring` = `#265D73`) when focused; restore `class="dark"` after

**Checkpoint**: US3 validated — focus rings are visible on both dark and light surfaces; no changes to component logic required (handled entirely by the `ring` token defined in Phase 2).

---

## Phase 6: User Story 4 — Developer Semantic Token System (P3)

**Goal**: Feature components contain zero raw hex values; all semantic and brand Tailwind utilities work; optional usage documentation is in place.

**Independent Test**: Run the grep check from `quickstart.md` — zero results for raw hex utilities or palette classes in `src/components` and `src/features`; build and tests stay green.

- [x] T026 [US4] Run the semantic-completeness grep check from `frontend/`: `grep -rnE "bg-\[#|text-\[#|border-\[#|bg-(indigo|gray|red|blue)-[0-9]|text-gray-[0-9]" src/components src/features` — confirm zero results after all Phase 3 migrations
- [x] T027 [P] [US4] Add a `# Theme` section to `frontend/README.md` (or create `frontend/docs/theme.md` if a docs directory exists) documenting: the five brand colors and their intended use, the full semantic token list with dark/light values, the `brand`/`brand-muted` tokens, and the rule to always use semantic utilities over raw hex — reference `specs/003-naja-echo-theme/contracts/theme-tokens.md`
- [x] T028 [US4] Run `npm run build` and `npm run test:run` as final developer-story confirmation that the full token surface compiles and all tests pass

**Checkpoint**: US4 validated — no raw hex in components, token system is documented, build is green.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Full quickstart.md validation pass, final hygiene.

- [x] T029 [P] Run the full quickstart validation from `specs/003-naja-echo-theme/quickstart.md`: build, lint, test, visual dark-theme checks (scenarios 1–5), visual light-theme checks (scenarios 6–9), developer grep check (scenario 10)
- [x] T030 [P] Run `npm run lint` from `frontend/` and fix any lint errors introduced by theme changes (unused imports, etc.)
- [x] T031 Restore `class="dark"` in `frontend/index.html` if any phase temporarily removed it for light-theme validation — confirm the shipped default is dark

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — **BLOCKS all user story phases**
- **Phase 3 (US1)**: Depends on Phase 2 — delivers MVP, must complete before Phase 4/5/6
- **Phase 4 (US2)**: Depends on Phase 2; light theme tokens defined in Phase 2, visual validation here
- **Phase 5 (US3)**: Depends on Phase 2 (ring token) and Phase 3 (button focus class confirmed)
- **Phase 6 (US4)**: Depends on Phases 3, 4, 5 all being complete (grep check covers all pages)
- **Phase 7 (Polish)**: Depends on Phase 6

### User Story Dependencies

- **US1 (P1)**: Depends only on Phase 2 (Foundational) — no story dependencies
- **US2 (P2)**: Depends only on Phase 2 — independent of US1 implementation (uses same token definitions)
- **US3 (P2)**: Depends on Phase 2 + T023 (button focus class confirmation from US1 phase)
- **US4 (P3)**: Depends on US1 completion (grep check covers pages migrated in US1)

### Parallel Opportunities Within Each Phase

**Phase 2**: T003–T007 are sequential (all edit `src/index.css` in order). T008 follows last.

**Phase 3**: T009 (index.html), T010–T012 (button.tsx), T013–T016 (four separate page files) — T013–T016 are all marked `[P]` and can run in parallel since they are different files. T010–T012 share `button.tsx` — run sequentially. T009 and T013–T016 can parallel after T010–T012 are done.

**Phases 4/5**: T019–T021 and T023–T025 are visual verification — can run in parallel with each other once Phase 3 is complete, if two reviewers are available.

**Phase 6**: T027 (docs) can run in parallel with T026 (grep) since they touch different files.

---

## Parallel Example: Phase 3 Feature-Page Migration

```bash
# After T010–T012 (button.tsx) are done, these four page migrations can run simultaneously:
Task T013: "Migrate frontend/src/features/auth/pages/LandingPage.tsx off bg-gray-50, text-gray-500"
Task T014: "Migrate frontend/src/features/auth/pages/AuthErrorPage.tsx off bg-gray-50"
Task T015: "Migrate frontend/src/features/auth/pages/AuthCallbackPage.tsx off text-gray-500"
Task T016: "Migrate frontend/src/features/dashboard/pages/DashboardPage.tsx off bg-gray-50, text-gray-500"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (baseline confirmation)
2. Complete Phase 2: Foundational — define all tokens in `src/index.css` (**CRITICAL BLOCK**)
3. Complete Phase 3: US1 — button.tsx + feature page migrations + dark theme visual + tests
4. **STOP and VALIDATE**: App loads in dark with gold primary, all pages semantic, tests green
5. This is a shippable themed application — US2/3/4 add polish and developer ergonomics

### Incremental Delivery

1. Phase 1 + Phase 2 → token system live (nothing visible yet since no components used semantic tokens in dark before)
2. + Phase 3 → dark flagship theme fully rendered, tests green (MVP)
3. + Phase 4 → light theme verified and polished
4. + Phase 5 → keyboard accessibility confirmed
5. + Phase 6 → developer documentation and clean grep check
6. + Phase 7 → full validation pass, ready to merge

---

## Notes

- `[P]` tasks touch different files — safe to run in parallel with no merge conflicts
- Phases 4/5 are primarily visual validation — the implementation is complete once Phase 2 defines the tokens; these phases confirm correctness
- All raw hex values live exclusively in `src/index.css` after Phase 2; feature components use only semantic Tailwind utilities
- Do not create new shadcn/ui primitives (Input, Label, Dropdown, Sheet, Badge) — out of scope per plan; the token system makes them theme-ready when a real feature needs them
- Restore `class="dark"` in `index.html` before merging; temporary light-theme validation in Phases 4/5 must not ship as the default
