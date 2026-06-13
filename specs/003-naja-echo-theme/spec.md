# Feature Specification: Naja Echo Application Theme

**Feature Branch**: `003-naja-echo-theme`

**Created**: 2026-06-13

**Status**: Draft

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Dark Theme Is the Primary Application Experience (Priority: P1)

A user opens the Naja Echo portal in their browser. The entire application surface — background, panels, text, buttons, inputs, navigation, and interactive elements — renders in a dark, sci-fi-inspired colour scheme anchored by deep teal-black backgrounds and gold primary actions. The experience feels polished, readable, and on-brand. No elements appear using placeholder or browser-default colours.

**Why this priority**: The dark theme is the flagship application experience. Until a full branded dark theme is in place, every subsequent UI feature ships against an off-brand surface, degrading first impressions and developer confidence.

**Independent Test**: Load the application in a browser without toggling any settings. Inspect the page background, a primary action button, a card or panel, body text, and a form input — all must show themed colours rather than browser or framework defaults.

**Acceptance Scenarios**:

1. **Given** the app loads in a browser, **When** the page renders, **Then** the main application background uses the deep teal-black brand colour and all primary text is warm and readable against it.
2. **Given** a primary action button renders on a dark surface, **When** the user views it, **Then** the button displays the gold brand colour with dark, readable foreground text.
3. **Given** a card or panel renders on the dark surface, **When** the user views it, **Then** the card surface is slightly raised from the background, uses themed border and foreground colours, and is visually distinct from the background without using raw hex values.
4. **Given** a form input renders on the dark surface, **When** the user interacts with it, **Then** the input uses a subtle teal border and a visible gold focus ring.
5. **Given** an error or destructive state is displayed on a dark surface, **When** the user sees it, **Then** the destructive colour is accessible and readable against the dark background.

---

### User Story 2 — Light Surfaces Are Readable and Professionally Styled (Priority: P2)

A user encounters a light-themed surface within the application (for example, a modal, a sheet, or a page section rendered with light styling). The surface uses a warm off-white background, deep teal-black text, and teal primary actions — not an inverted copy of the dark theme. Gold brand accents on light surfaces use a muted gold rather than the bright logo gold to preserve readability.

**Why this priority**: Light surfaces must be usable when they appear, but they are secondary to the dark experience. An unthemed or garish light surface undermines the polished feel of the product.

**Independent Test**: Render a shadcn/ui Card, Button (primary variant), and a form Input in a light-mode context. Verify the background is off-white, the primary button uses teal (not gold), and any gold accent is the muted gold variant.

**Acceptance Scenarios**:

1. **Given** a light-theme surface renders, **When** the user views the background and primary text, **Then** the background is a warm off-white and text is deep teal-black.
2. **Given** a primary action button renders on a light surface, **When** the user views it, **Then** it uses the secondary teal colour with light, readable foreground text — not the bright gold.
3. **Given** gold text or a gold accent is used on a light background, **When** the user views it, **Then** the muted gold variant is used rather than the bright primary gold.
4. **Given** a card renders on a light surface, **When** the user views it, **Then** it uses a near-white card background, themed border, and deep teal-black foreground text.

---

### User Story 3 — Keyboard Users Can See Focus States (Priority: P2)

A keyboard or assistive-technology user navigates through interactive controls (buttons, inputs, links, dropdowns) in both dark and light contexts. A clearly visible focus ring appears on every focused element and does not disappear or blend into the background.

**Why this priority**: Accessibility is a non-negotiable product quality requirement. Focus visibility is required for WCAG conformance and for users who cannot rely on a mouse.

**Independent Test**: Open the application and tab through at least three interactive controls on both dark and light surfaces. Confirm a visible focus ring appears on each control.

**Acceptance Scenarios**:

1. **Given** the user tabs to a button in the dark theme, **When** focus lands on the button, **Then** a clearly visible gold focus ring surrounds it.
2. **Given** the user tabs to a form input in the dark theme, **When** focus lands on the input, **Then** a visible focus ring appears and does not blend into the dark background.
3. **Given** the user tabs to a control in a light-theme surface, **When** focus lands on the control, **Then** a visible teal focus ring appears and does not blend into the light background.

---

### User Story 4 — Developers Can Build New Features Without Hardcoding Colours (Priority: P3)

A developer building a new feature component reaches for semantic Tailwind and shadcn/ui utility classes (`bg-background`, `text-foreground`, `bg-primary`, `text-primary-foreground`, `bg-card`, `border-border`, `ring-ring`, `text-muted-foreground`, `bg-brand`, `text-brand-muted`, etc.) and finds that they work correctly in both dark and light contexts without needing to know any raw hex values or add custom styles.

**Why this priority**: A semantic token system is what makes the theme sustainable. Without it, future feature work degrades the brand palette through scattered hardcoded values.

**Independent Test**: Create a minimal test component using only semantic Tailwind token classes — no raw hex values — and verify it renders correctly in both dark and light contexts with the correct brand colours.

**Acceptance Scenarios**:

1. **Given** a developer uses `bg-primary` and `text-primary-foreground` in a component, **When** the component renders in the dark theme, **Then** it displays the gold background with dark foreground text without any raw hex in the component file.
2. **Given** a developer uses `bg-primary` and `text-primary-foreground` in a component, **When** the component renders in a light context, **Then** it displays the secondary teal background with light foreground text.
3. **Given** a developer uses `text-brand-muted` in a component, **When** the component renders on a light background, **Then** it displays the muted gold colour.
4. **Given** a developer uses `bg-card`, `border-border`, and `text-foreground` in a component, **When** the component renders, **Then** it correctly inherits theme-appropriate card, border, and text colours in both modes.

---

### Edge Cases

- What happens when a shadcn/ui component that was not explicitly reviewed (e.g., a Tooltip, Popover, or Combobox) renders? It should inherit the CSS variable-based theme and not show browser or shadcn default blue/purple colours.
- What happens when both dark and light tokens are defined but a component is rendered outside a themed root element? The component should degrade gracefully to visible defaults rather than becoming illegible.
- What happens when a developer accidentally uses a teal colour class (`bg-accent`, `text-accent`) as body text on the deep dark background? The muted-foreground and foreground tokens are designed to remain readable; teal tokens are surface/highlight only and should not be used as body text.
- What happens when the destructive colour is shown on the dark background versus the light background? Both modes must use colours with sufficient contrast to be readable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST define a complete dark theme semantic colour palette covering: background, foreground, card, card-foreground, primary, primary-foreground, secondary, secondary-foreground, accent, accent-foreground, muted, muted-foreground, popover, popover-foreground, border, input, ring, destructive, and destructive-foreground.
- **FR-002**: The application MUST define a complete light theme semantic colour palette covering all tokens listed in FR-001, plus brand and brand-muted tokens for gold usage on light surfaces.
- **FR-003**: The dark theme background MUST use the deep teal-black brand colour (`#0E2226`) as the main application background.
- **FR-004**: The dark theme primary token MUST map to the primary gold brand colour (`#CCAC31`) with a dark, readable primary-foreground.
- **FR-005**: The light theme primary token MUST map to the secondary teal brand colour (`#204F59`) with a light, readable primary-foreground.
- **FR-006**: The light theme MUST provide a brand token for the primary gold (`#CCAC31`) and a brand-muted token for the muted gold (`#8C7326`) to support logo, ornament, and accent usage on light surfaces without using gold as body text.
- **FR-007**: All raw brand hex values MUST be centralized in global theme configuration files only. Feature components MUST NOT contain hardcoded hex colour values.
- **FR-008**: shadcn/ui components (Button, Card, Input, Dropdown, Sheet, Badge, Alert, and navigation-adjacent components) MUST inherit the Naja Echo theme through CSS variables and display in the correct themed colours in both dark and light contexts.
- **FR-009**: Tailwind MUST expose all semantic colour tokens (including brand and brand-muted) so developers can use utility classes without writing custom CSS or raw hex values in component files.
- **FR-010**: Focus rings MUST be visible on all interactive controls in both dark and light contexts. The dark theme focus ring MUST use the primary gold or an equivalently visible colour; the light theme focus ring MUST use the accent teal or an equivalently visible colour.
- **FR-011**: The destructive token MUST be accessible (sufficient contrast) against both dark and light backgrounds. Text on destructive backgrounds (destructive-foreground) MUST be readable in both themes.
- **FR-012**: Teal colours MUST NOT be used as low-contrast body text on the deep dark background.
- **FR-013**: Bright primary gold MUST NOT be used as normal body text on light backgrounds.
- **FR-014**: Existing layout, routing, navigation behaviour, and all currently passing frontend tests MUST continue to function correctly after the theme is applied.

### Key Entities

- **Dark Theme Palette**: The set of CSS variable values active when the dark theme class is applied. Anchored to the deep teal-black background. Primary gold for actions.
- **Light Theme Palette**: The set of CSS variable values active when the light theme class is applied. Warm off-white background. Secondary teal for primary actions. Muted gold for gold accents.
- **Semantic Token**: A named CSS variable (e.g., `--background`, `--primary`, `--ring`) that maps to a colour value for the active theme. Feature components consume tokens, not raw colours.
- **Brand Token**: An additional semantic token (`brand`, `brand-muted`) used specifically for gold brand presence on light surfaces where the standard primary is teal.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every page and surface visible to a user on the dark theme displays the deep teal-black background — no browser-default white, grey, or shadcn placeholder colour appears on any themed surface.
- **SC-002**: A primary action button rendered in the dark theme displays the gold brand colour with readable foreground text; the same semantic component rendered in a light context displays the secondary teal with readable foreground text.
- **SC-003**: A keyboard user navigating through the application can identify the focused interactive element on every control without relying on position alone — focus rings are visible against both dark and light surfaces.
- **SC-004**: A developer can implement a complete feature component — including background, text, button, card, input, and badge usage — using only semantic Tailwind utility classes, with zero raw hex values in the component source.
- **SC-005**: All shadcn/ui components used in the application display in theme-consistent colours after the theme is applied, with no component reverting to unrelated framework defaults.
- **SC-006**: All existing frontend tests pass without modification after the theme is applied.
- **SC-007**: Destructive and error states are visually distinguishable from normal states on both dark and light surfaces, without relying on colour as the sole differentiator.

## Assumptions

- The application already has Tailwind CSS and shadcn/ui installed and configured. This feature modifies the theme configuration rather than installing new tooling.
- The dark theme is applied via a class or attribute on a root element (e.g., `<html class="dark">`), consistent with shadcn/ui's standard theming approach. A light mode class or the absence of the dark class activates the light theme.
- The dark theme is the default application experience; the app loads in dark mode by default without requiring user action.
- No user-facing theme switcher is required as part of this feature; the class-based dark/light mechanism provides a foundation for a switcher to be added later.
- There is no backend API involved. This is a frontend-only configuration and styling change.
- "No API contract changes required" — this feature introduces no new or changed backend HTTP behaviour (per constitution Principle I).
- All colour contrast decisions assume WCAG AA as the minimum target for body text and interactive elements.
- The `brand` and `brand-muted` tokens are additional semantic tokens beyond the standard shadcn/ui variable set; they are exposed through Tailwind configuration for developer use.
- Popover and dropdown surfaces on the dark theme are assumed to use a slightly lighter dark teal surface (the popover token) to visually lift them above the base card level.
- Mobile layout and responsive behaviour are within scope for theme consistency but not for layout changes; the theme applies uniformly regardless of viewport.
