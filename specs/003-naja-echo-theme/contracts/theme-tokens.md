# Contract: Semantic Theme Tokens

This is the **UI contract** this feature exposes to frontend developers — the stable set of semantic Tailwind utilities available application-wide. It replaces ad-hoc hex usage. Consuming a token by these names is the supported interface; the underlying hex values are an implementation detail owned by `src/index.css`.

> **No API contract changes required.** This feature exposes no HTTP/OpenAPI surface. This document is a frontend design-token contract only.

## Available utility classes

For every token below, the standard Tailwind color utilities are available: `bg-<token>`, `text-<token>`, `border-<token>`, `ring-<token>`, plus opacity modifiers (`bg-<token>/50`).

### shadcn/ui-compatible semantic tokens

| Token | Utilities (examples) | Intended use |
|-------|----------------------|--------------|
| `background` | `bg-background` | App/page background |
| `foreground` | `text-foreground` | Primary body text |
| `card` / `card-foreground` | `bg-card`, `text-card-foreground` | Cards, panels |
| `popover` / `popover-foreground` | `bg-popover`, `text-popover-foreground` | Dropdowns, popovers |
| `primary` / `primary-foreground` | `bg-primary`, `text-primary-foreground` | Primary action buttons |
| `secondary` / `secondary-foreground` | `bg-secondary`, `text-secondary-foreground` | Secondary actions, section headers |
| `accent` / `accent-foreground` | `bg-accent`, `text-accent-foreground` | Links, selected/active states |
| `muted` / `muted-foreground` | `bg-muted`, `text-muted-foreground` | Subdued surfaces, secondary text |
| `destructive` / `destructive-foreground` | `bg-destructive`, `text-destructive-foreground` | Error/destructive states |
| `border` | `border-border` | Borders/dividers |
| `input` | `border-input` | Input borders/surfaces |
| `ring` | `ring-ring` | Focus rings |

### Brand-specific tokens

| Token | Utilities | Intended use |
|-------|-----------|--------------|
| `brand` / `brand-foreground` | `bg-brand`, `text-brand-foreground` | Filled gold elements (logo, badges, ornament) with dark text |
| `brand-muted` / `brand-muted-foreground` | `text-brand-muted`, `bg-brand-muted` | Gold *text*/accents — **preferred for gold on light backgrounds** |

## Behavioural guarantees

1. Every token resolves correctly under both the default dark theme (`.dark` on `<html>`) and the light theme (`:root`).
2. Dark `primary` is gold `#CCAC31` with dark foreground; light `primary` is teal `#204F59` with light foreground. The same `bg-primary`/`text-primary-foreground` pair is correct in both themes.
3. Focus rings (`ring-ring`) are visible on both surfaces (gold on dark, teal on light).
4. `destructive` is accessible on both surfaces.
5. No feature component contains raw hex; the only hex literals live in `src/index.css`.

## Consumer obligations

- Use these utilities instead of raw palette classes (`bg-indigo-600`, `bg-gray-50`) or arbitrary values (`bg-[#0E2226]`).
- Do not use teal tokens (`accent`/`secondary`) as body text on the dark `background`.
- For gold text/accents on light backgrounds use `text-brand-muted`, never `text-primary`/`text-brand`.
- Provide a non-color cue (icon, label) alongside `destructive` state — never color alone.

## Verification

See `quickstart.md` for the runnable checks that confirm this contract holds (build, typecheck, lint, test, and visual token inspection).
