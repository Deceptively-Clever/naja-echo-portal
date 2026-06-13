# Naja Echo Portal — Frontend

React + TypeScript SPA for the Naja Echo org-management portal. Built with Vite, Tailwind CSS v4, and shadcn/ui-style primitives.

## Theme

The application uses a semantic token system defined in `src/index.css`. All raw hex values live there. Feature components use semantic Tailwind utilities only — never raw hex or palette classes.

**Dark theme** is the default (flagship) experience, activated by `class="dark"` on `<html>`.
**Light theme** activates when the `.dark` class is absent.

### Brand colors and their semantic tokens

| Brand color | Hex | Use |
|-------------|-----|-----|
| Primary Gold | `#CCAC31` | Dark theme primary actions (`bg-primary`), logo, badges, focus ring (`ring-ring`) |
| Primary Gold Muted | `#8C7326` | Gold text/accents on light backgrounds (`text-brand-muted`) |
| Accent Teal | `#265D73` | Links, selected states, accents (`bg-accent`), light focus ring (`ring-ring`) |
| Secondary Teal | `#204F59` | Light theme primary actions (`bg-primary`), section headers (`bg-secondary` dark) |
| Deep Background | `#0E2226` | Dark app background (`bg-background`) |

### Available semantic utilities

Use these in all feature components:

```
bg-background        text-foreground
bg-card              text-card-foreground
bg-popover           text-popover-foreground
bg-primary           text-primary-foreground
bg-secondary         text-secondary-foreground
bg-accent            text-accent-foreground
bg-muted             text-muted-foreground
bg-destructive       text-destructive-foreground
border-border        border-input
ring-ring
bg-brand             text-brand-foreground
bg-brand-muted       text-brand-muted
```

### Color usage rules

- **Dark theme primary actions** → `bg-primary` (gold `#CCAC31`) with `text-primary-foreground` (dark)
- **Light theme primary actions** → `bg-primary` (teal `#204F59`) with `text-primary-foreground` (white)
- **Gold text on light backgrounds** → `text-brand-muted` (muted gold `#8C7326`), never `text-primary`
- **Teal colors** are surfaces, borders, accents — not body text on the dark background
- **Destructive states** → `bg-destructive` + a non-color cue (icon or label)

Full token reference: [`specs/003-naja-echo-theme/contracts/theme-tokens.md`](../specs/003-naja-echo-theme/contracts/theme-tokens.md)

---

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend updating the configuration to enable type-aware lint rules:

```js
export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...

      // Remove tseslint.configs.recommended and replace with this
      tseslint.configs.recommendedTypeChecked,
      // Alternatively, use this for stricter rules
      tseslint.configs.strictTypeChecked,
      // Optionally, add this for stylistic rules
      tseslint.configs.stylisticTypeChecked,

      // Other configs...
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```

You can also install [eslint-plugin-react-x](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-x) and [eslint-plugin-react-dom](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-dom) for React-specific lint rules:

```js
// eslint.config.js
import reactX from 'eslint-plugin-react-x'
import reactDom from 'eslint-plugin-react-dom'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...
      // Enable lint rules for React
      reactX.configs['recommended-typescript'],
      // Enable lint rules for React DOM
      reactDom.configs.recommended,
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```
