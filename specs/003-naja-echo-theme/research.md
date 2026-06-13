# Phase 0 Research: Naja Echo Application Theme

All open questions are resolved by inspecting the existing frontend. No NEEDS CLARIFICATION remain.

## Decision 1: Tailwind version and token mechanism

**Decision**: Use Tailwind CSS **v4** CSS-first configuration. Define tokens as CSS custom properties in `src/index.css` and register them as utility colors via `@theme inline`.

**Rationale**: `package.json` pins `tailwindcss: ^4.3.0` and `@tailwindcss/vite: ^4.3.0`; `vite.config.ts` uses the `@tailwindcss/vite` plugin; `src/index.css` is `@import "tailwindcss";`. In v4, colors are registered through `@theme`, not `tailwind.config.ts` `theme.extend`. The existing `tailwind.config.ts` is effectively vestigial (empty `extend`, content auto-detected in v4).

**Alternatives considered**:
- *Tailwind v3 `tailwind.config.ts` `theme.extend.colors` with `hsl(var(--x))`*: Rejected — wrong major version; would not take effect under the v4 plugin.
- *Inline arbitrary values (`bg-[#0E2226]`) in components*: Rejected — violates the "no scattered raw hex" requirement and Principle VI.

## Decision 2: Current theme state

**Decision**: Treat this as a **greenfield token definition**, not a token migration.

**Rationale**: `src/index.css` defines **no** tokens. `card.tsx`, `alert.tsx`, and `avatar.tsx` already reference semantic classes (`bg-card`, `text-card-foreground`, `bg-background`, `text-foreground`, `bg-muted`, `text-destructive`, `ring-ring`, `border-input`) that currently resolve to nothing. Once tokens exist, those components style correctly with no edits. `button.tsx` is the exception — it uses raw palette colors (`bg-indigo-600`, `bg-red-600`, `text-white`) and must be re-pointed.

**Alternatives considered**: None — state is unambiguous from inspection.

## Decision 3: Dark/light mechanism and default

**Decision**: Class-based dark mode via `@custom-variant dark (&:is(.dark *));`. Light tokens in `:root`, dark overrides in `.dark`. Apply `class="dark"` on `<html>` in `index.html` so **dark is the default**.

**Rationale**: The project has no existing dark-mode mechanism (no `dark:` usage, no theme provider, no `class`/`media` config). The spec mandates dark as the flagship default while keeping light fully defined for future use. Class-based + `:root` light fallback satisfies both and leaves the door open for a future switcher without building one now (YAGNI).

**Alternatives considered**:
- *`prefers-color-scheme` media strategy*: Rejected — would make the default follow OS preference rather than guaranteeing the flagship dark experience.
- *Dark values directly in `:root` with no `.dark` class*: Rejected — would not leave a clean path to a future switcher and would make light a second-class afterthought.

## Decision 4: Color format

**Decision**: Store the brand/neutral palette as hex literals in the CSS variables (matching the suggested mappings in the spec/plan input), e.g. `--background: #0E2226;`.

**Rationale**: The brand colors are specified as hex; Tailwind v4 supports any valid CSS color in `@theme`. Hex keeps the single source of truth legible and matches the brand guide exactly. Alpha-modified utilities (e.g. `border-destructive/50` already used in `alert.tsx`) work in v4 via `color-mix`, which accepts hex.

**Alternatives considered**:
- *HSL channel triples (`--background: 188 44% 10%`)*: This is the classic shadcn v3 pattern enabling `/opacity`. Rejected as the default because v4's `@theme inline` + `color-mix` already gives opacity support on hex, and hex is more readable against the brand guide. (Existing `border-destructive/50` confirms opacity modifiers still work.)

## Decision 5: Scope of components

**Decision**: Apply and verify the theme on the components that **exist**: Button, Card, Alert, Avatar — plus migrate the feature pages off `bg-gray-*`/`text-gray-*`. Do **not** create Input, Label, Dropdown/Menu, Sheet/Dialog, or Badge.

**Rationale**: The brief lists those for "coherence verification," but they are absent from the repo. Creating them now would be speculative (Principle IV / YAGNI). The token system makes them theme-ready the moment a real feature introduces them.

**Alternatives considered**:
- *Generate the full shadcn component set now*: Rejected — speculative scope, no current consumer.

## Decision 6: Regression / snapshot risk

**Decision**: Low risk; no snapshot baselines to update.

**Rationale**: Grep of `*.test.tsx`/`*.test.ts` shows no assertions on color classes and no `toMatchSnapshot` usage. Tests query by role/text/aria-label. Theme changes should not break behavioural tests. Any failure caused by the theme will be inspected, not blindly re-baselined.

## Accessibility validation approach

Contrast pairings to verify (WCAG AA target) against the suggested mappings:

- Dark `foreground` `#F4F0E3` on `background` `#0E2226` — high contrast ✔ expected.
- Dark `primary-foreground` `#0E2226` on `primary` gold `#CCAC31` — dark-on-gold ✔ expected pass.
- Dark `muted-foreground` `#A9B6B8` on `background`/`muted` — verify ≥ 4.5:1 for body, ≥ 3:1 acceptable for secondary text.
- Light `primary-foreground` `#FFFFFF` on `primary` teal `#204F59` — ✔ expected pass.
- Light `foreground` `#102A30` on `background` `#F7F4EA` — ✔ expected pass.
- Focus rings: dark `ring` gold `#CCAC31` on dark surfaces; light `ring` accent teal `#265D73` on light surfaces — both visible.
- Avoid: teal as body text on `#0E2226` (use foreground/muted-foreground); bright gold body text on light (use `brand-muted` `#8C7326`).

Verification is manual/visual plus the rendering checks in `quickstart.md`. Contrast is confirmed during implementation; any pairing under AA is adjusted at the token level only.
