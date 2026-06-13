# Phase 1 Data Model: Theme Token Model

This feature has no runtime data entities. The "data model" is the **design-token model**: the set of semantic CSS variables and their per-theme values. These are the source of truth that `contracts/theme-tokens.md` exposes to feature developers.

## Entities

### Theme

A named set of token values activated by a root selector.

| Theme | Activation selector | Role |
|-------|--------------------|------|
| Dark (default) | `.dark` on `<html>` | Flagship application experience |
| Light | `:root` (no `.dark`) | Polished, readable secondary surface set |

### Semantic Token

A named CSS variable (`--<token>`) registered as a Tailwind color (`--color-<token>`) so it produces `bg-/text-/border-/ring-` utilities. Each token has exactly one value per theme.

### Token usage rules (validation rules)

- Feature components MUST reference tokens via utilities (`bg-primary`, `text-muted-foreground`, …), never raw hex.
- Raw hex literals MUST appear only in `src/index.css`.
- Teal tokens (`accent`, `secondary`) are surface/border/active-state colors — MUST NOT be used as body text on the dark `background`.
- Bright gold (`primary`/`brand` in dark; `brand` in light) MUST NOT be used as body text on light backgrounds; use `brand-muted` instead.
- Every `*-foreground` token MUST maintain readable contrast against its paired surface token.

## Dark theme token values (default)

| Token | Value | Usage |
|-------|-------|-------|
| `background` | `#0E2226` | Main app background (deep teal-black) |
| `foreground` | `#F4F0E3` | Primary body text (warm light) |
| `card` | `#132D33` | Raised card/panel surface |
| `card-foreground` | `#F4F0E3` | Text on cards |
| `popover` | `#132D33` | Popover/dropdown surface |
| `popover-foreground` | `#F4F0E3` | Text on popovers |
| `primary` | `#CCAC31` | Primary actions, brand emphasis (gold) |
| `primary-foreground` | `#0E2226` | Dark text on gold |
| `secondary` | `#204F59` | Secondary actions, section headers (teal) |
| `secondary-foreground` | `#FFFFFF` | Text on secondary |
| `accent` | `#265D73` | Links, selected/active states, accent surfaces |
| `accent-foreground` | `#FFFFFF` | Text on accent |
| `muted` | `#193840` | Subdued surface |
| `muted-foreground` | `#A9B6B8` | Lower-emphasis text |
| `border` | `#2B4A52` | Subtle teal/blue border |
| `input` | `#2B4A52` | Input border/surface |
| `ring` | `#CCAC31` | Focus ring (gold, visible) |
| `destructive` | `#F97066` | Error/destructive (readable on dark) |
| `destructive-foreground` | `#0E2226` | Dark text on destructive |
| `brand` | `#CCAC31` | Filled brand elements / logo gold |
| `brand-foreground` | `#0E2226` | Dark text on brand gold |
| `brand-muted` | `#8C7326` | Muted gold accent |
| `brand-muted-foreground` | `#F4F0E3` | Text paired with muted gold |

## Light theme token values

| Token | Value | Usage |
|-------|-------|-------|
| `background` | `#F7F4EA` | Warm off-white background |
| `foreground` | `#102A30` | Deep teal-black body text |
| `card` | `#FFFFFF` | Card/panel surface |
| `card-foreground` | `#102A30` | Text on cards |
| `popover` | `#FFFFFF` | Popover/dropdown surface |
| `popover-foreground` | `#102A30` | Text on popovers |
| `primary` | `#204F59` | Primary actions (secondary teal — NOT gold) |
| `primary-foreground` | `#FFFFFF` | Light text on teal |
| `secondary` | `#ECE7D8` | Warm muted secondary surface |
| `secondary-foreground` | `#102A30` | Text on secondary |
| `accent` | `#265D73` | Links, selected states, headers |
| `accent-foreground` | `#FFFFFF` | Text on accent |
| `muted` | `#ECE7D8` | Warm muted surface |
| `muted-foreground` | `#5C6870` | Lower-emphasis text |
| `border` | `#D9D1B8` | Warm subtle border |
| `input` | `#CFC6AC` | Warm subtle input border |
| `ring` | `#265D73` | Focus ring (accent teal, visible) |
| `destructive` | `#B42318` | Error/destructive (readable on light) |
| `destructive-foreground` | `#FFFFFF` | Light text on destructive |
| `brand` | `#CCAC31` | Logo/ornament/badge gold (filled, with dark text) |
| `brand-foreground` | `#102A30` | Dark text on brand gold |
| `brand-muted` | `#8C7326` | Muted gold for gold *text*/accents on light |
| `brand-muted-foreground` | `#FFFFFF` | Text paired with muted gold |

## State / transitions

The only "transition" is theme activation: presence/absence of the `.dark` class on `<html>`. Dark is applied at load (default). No runtime state machine; a future switcher would toggle the class but is out of scope.
