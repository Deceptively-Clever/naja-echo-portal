---
name: frontend-components
description: >
  Enforces consistent UI component usage in the React/Tailwind/shadcn frontend.
  USE FOR: any frontend work that adds or modifies form controls, selects,
  dropdowns, buttons, or other interactive elements.
  DO NOT USE FOR: backend C# code, EF Core, API endpoints, or test-only changes.
license: MIT
---

# Frontend Component Standards

This project uses **React + Tailwind CSS v4 + shadcn/ui (Radix UI)**.
All interactive UI elements must come from `src/components/ui/` — never use
raw HTML form elements or hand-rolled Tailwind class strings for things the
component library already covers.

## Core rule

> If a shadcn/ui component exists for the element you need, use it.
> Never reach for a raw `<select>`, `<input type="checkbox">`, `<button>`, etc.
> when a `<Select>`, `<Checkbox>`, or `<Button>` is available.

This ensures:
- Styling is defined once and inherited from CSS variables (`--background`,
  `--foreground`, `--border`, etc.) — it adapts to theme changes automatically.
- You cannot accidentally forget `bg-background text-foreground` and ship a
  control with a white background on a dark theme.
- Keyboard navigation, ARIA roles, and focus management are handled by Radix UI.

## Available components

Check `src/components/ui/` for what's installed. As of the initial setup:

| Component | File | Use for |
|-----------|------|---------|
| `Button` | `button.tsx` | All clickable actions |
| `Select` | `select.tsx` | Dropdowns / option pickers |
| `DropdownMenu` | `dropdown-menu.tsx` | Contextual action menus |
| `Badge` | `badge.tsx` | Status / count labels |
| `Card` | `card.tsx` | Content containers |
| `Sheet` | `sheet.tsx` | Side panels / drawers |
| `Table` | `table.tsx` | Tabular data |
| `Tabs` | `tabs.tsx` | Tab navigation |
| `Skeleton` | `skeleton.tsx` | Loading placeholders |

## Adding a new shadcn component

When you need a component that isn't in `src/components/ui/` yet:

1. Check if `@radix-ui/react-<name>` is already in `package.json`.
2. If not, install it: `npm install @radix-ui/react-<name>` from the `frontend/` directory.
3. Copy the standard shadcn/ui implementation into `src/components/ui/<name>.tsx`.
   - Use `cn()` from `@/lib/utils` for class merging.
   - Apply theme tokens (`bg-background`, `text-foreground`, `border-input`,
     `ring-ring`, etc.) — never hardcode colors.
   - Follow the same `React.forwardRef` + `displayName` pattern as existing components.
4. Export all sub-components from the new file.

Do **not** install shadcn's CLI or auto-generate components — add them manually
following the patterns in existing files like `select.tsx` or `button.tsx`.

## Select / dropdown pattern

```tsx
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

<Select value={value} onValueChange={onChange}>
  <SelectTrigger aria-label="Label" className="w-40">
    <SelectValue placeholder="All items" />
  </SelectTrigger>
  <SelectContent>
    <SelectItem value="__all__">All items</SelectItem>
    {items.map((item) => (
      <SelectItem key={item.id} value={item.id}>{item.name}</SelectItem>
    ))}
  </SelectContent>
</Select>
```

**Important:** Radix Select does not allow an empty string `""` as a value.
Use a sentinel string like `"__all__"` for the "show all" option and convert
it back to `""` or `undefined` in the `onValueChange` handler:

```ts
onValueChange={(v) => setFilter(v === '__all__' ? '' : v)}
```

## Plain text inputs

Raw `<input type="text">` is fine — there's no shadcn component for basic text
inputs in this project. Apply these classes consistently:

```
h-9 rounded-md border border-input bg-background px-3 text-sm text-foreground
```

## What NOT to do

```tsx
// Bad — raw select, no theme tokens, inconsistent height
<select className="h-8 rounded border px-2 text-sm">

// Bad — native select with partial tokens (still not a shadcn component)
<select className="h-7 rounded-md border border-input bg-background px-2 text-xs text-foreground">

// Good
<Select ...><SelectTrigger className="h-8 w-36 text-xs">...</SelectTrigger></Select>
```

## Validation checklist

Before completing any frontend task:

- [ ] No raw `<select>` elements (use `<Select>` from shadcn)
- [ ] No raw `<button>` elements where `<Button>` applies
- [ ] All text inputs use the standard class string above
- [ ] New shadcn components follow the `forwardRef` + `cn()` + theme-token pattern
- [ ] `npx tsc --noEmit` passes with zero errors
