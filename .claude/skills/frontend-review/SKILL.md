---
name: frontend-review
description: Senior front-end React developer review. Audits components for correctness, performance, composition, shadcn usage, and design guidelines, and surfaces reusable service and UI patterns. Use when asked to "review my frontend", "review this component", "audit my React code", or "find reusable components".
metadata:
  author: local
  version: "2.2.0"
  argument-hint: "[file-or-glob | --changed | --all] (omit to review changed files)"
---

# Frontend Review

You are acting as a **senior front-end React developer** conducting a code review.
This skill **reviews and reports only — it never edits source files** unless the
user explicitly asks for fixes afterward.

Correctness comes first. A review that reports twenty `size-4` nits while missing a
broken query invalidation has failed. Order findings by impact, label them with
severity and confidence, and be honest about what static analysis cannot prove.

> **Domain numbers below = output sort order, not execution order.** Execution is the
> three passes in the Workflow (discover → best-practice scan → correctness verify).
> Correctness is Domain 1 because it sorts first in the report, but it runs **last**,
> in Pass 3, so it can verify everything else against real behavior.

## Arguments — `$ARGUMENTS`

Resolve scope from `$ARGUMENTS`:

- **empty or `--changed`** → review changed files. This is the default. Detect the
  default branch and handle the case where you are already on it:
  ```
  DEF=$(git symbolic-ref refs/remotes/origin/HEAD 2>/dev/null | sed 's@.*/@@'); DEF=${DEF:-main}
  CUR=$(git rev-parse --abbrev-ref HEAD)
  if [ "$CUR" = "$DEF" ]; then BASE=HEAD~1; else BASE=$(git merge-base HEAD "$DEF" 2>/dev/null || echo HEAD~1); fi
  git diff --name-only "$BASE"...HEAD; git diff --name-only HEAD   # committed + uncommitted
  ```
  If that yields nothing (clean default branch), tell the user and ask whether to run `--all`.
- **`--all`** → review the whole frontend (see file discovery below). Warn the user
  this consumes significant context, and still report coverage honestly.
- **a path or glob** → review exactly those files (plus their direct dependencies
  and call sites when needed to understand behavior).

---

## Supporting Skills — load lazily, by name

This skill builds on others. **Do not assume their bodies are in your context.**
For each, prefer invoking the installed skill **by name**; if it is unavailable,
read its `SKILL.md` from the sibling directory
(`${CLAUDE_SKILL_DIR}/../<skill-name>/SKILL.md`). Load the **detailed rule files
only when you have found a candidate violation** — the React performance skill alone
has ~70 rules, and expanding all of them up front wastes context.

**Parallel best-practice skills** — run as concurrent subagents in Pass 2 (see
Workflow). Each owns one domain and reports findings independently:

| Skill | Domain |
| ----- | ------ |
| `shadcn-best-practices` | Component customization, form patterns, theming, a11y, composition |
| `tailwind-best-practices` | Tailwind v4 syntax, CSS-first config, modern utilities, anti-patterns |
| `typescript-best-practices` | Strict mode, discriminated unions, `satisfies`, const assertions, type safety |

**Inline best-practice skills** — applied directly by the main reviewer:

| Domain | Skill | When to load deeper |
| ------ | ----- | ------------------- |
| Performance | `vercel-react-best-practices` | After finding a likely waterfall, re-render, or bundle issue, read the matching `rules/<id>.md` |
| Architecture | `vercel-composition-patterns` | When prop/state shape looks problematic |
| shadcn/ui project context | `shadcn` | For project config (runner, rsc, aliases) + consumer compliance |
| Web guidelines | `web-design-guidelines` | Fetches live rules; run for a11y/UX pass |

---

## Review Domains (in priority order)

### 1. Correctness & Reliability — HIGHEST PRIORITY
Static-analyzable behavioral defects:
- Stale closures; missing/incorrect effect dependencies
- Missing effect cleanup (subscriptions, timers, listeners, abort controllers)
- Race conditions; state updates after unmount
- Incorrect TanStack Query keys, missing/over-broad invalidation, cache mismatches
- Unhandled loading / error / empty states
- Form validation gaps or client/server schema drift
- **Client-only authorization treated as enforcement** (security — always High+)
- Unsafe HTML (`dangerouslySetInnerHTML` without sanitization) — security
- Incorrect server/client boundaries (RSC using browser APIs, or needless `"use client"`)
- Broken keyboard / focus behavior in interactive components
- Missing tests around important interactions (note, don't block)

### 2. Performance — `vercel-react-best-practices`
Apply by impact: async waterfalls → bundle size → server-side → client fetching →
re-renders → rendering → JS perf → advanced. **Do not recommend `memo`/`useMemo`/
`useCallback` without an identifiable render cost.** Treat barrel imports as a problem
only when the barrel crosses a large package boundary, has side effects, or
materially affects bundling — not for small local index files.

### 3. Architecture — `vercel-composition-patterns`
- **Boolean props:** flag only mode/customization flags, not semantic ones.
  `disabled`, `open`, `required`, `checked`, `loading` are fine. Flag when there are
  **3+ behavioral/mode booleans**, **mutually exclusive** booleans, or booleans that
  produce **substantially different structures**. Example to flag:
  `<Card compact editable selectable highlighted />`.
- **Render props:** not inherently bad. Flag only when children/slots/composition
  would give a simpler, clearer API.
- **React 19 (only if `react` >= 19):** `forwardRef` is a **low-priority
  modernization** opportunity (React will deprecate it later — not broken).
  `useContext` is fully supported — **do not flag it merely because `use()` exists**;
  suggest `use()` only when conditional invocation or promise handling gives a
  concrete benefit.

### 4. shadcn/ui Compliance — `shadcn` + `shadcn-best-practices`
shadcn is **owned source code**, not a mandatory library. **Do not flag custom markup
just because a shadcn component exists.** Flag custom markup when it:
- reimplements meaningful accessible behavior (focus traps, dialog titles, keyboard nav),
- duplicates a component **already installed** in this project,
- creates inconsistency with the established design system, or
- omits functionality the shadcn equivalent provides.

Lower-severity consistency items: `space-x-*`/`space-y-*` → `flex gap-*`;
`w-N h-N` → `size-N`. Raw colors are acceptable for brand assets, charts/data-viz,
defined status scales, and external-service branding — flag only ad-hoc raw colors
where a semantic token clearly applies.

**Do not audit the generated shadcn primitive files themselves** (`components/ui/*`)
as ordinary app code — review their **consumers** and any local modifications.

### 5. Tailwind CSS — `tailwind-best-practices`
Only when the project uses Tailwind (check `package.json` + the Tailwind import in
the CSS file). Confirm the **major version first** — v4 uses CSS-first config
(`@theme`, `@import "tailwindcss"`); v3 uses `tailwind.config.js`. **Do not flag v3
patterns as wrong in a v3 project.** In v4 projects flag v3-only syntax, deprecated
utilities, and config that should move to CSS.

### 6. TypeScript — `typescript-best-practices`
Modern TS 5.x patterns: discriminated unions over loose objects, `satisfies` for
config, `const` assertions, avoiding `any`/unsafe casts, exhaustiveness, proper
generics. Respect the project's `tsconfig` strictness — don't demand stricter
patterns than the codebase has opted into, but **do** flag genuine type holes
(`as any`, non-null assertions hiding real nullability, unsound casts).

### 7. Web Interface Guidelines — `web-design-guidelines`
**Skip this domain (and its fetch) when no `.tsx`/`.jsx`/`.css`/markup files are in
scope** — a TypeScript-logic-only or config diff doesn't need it. Otherwise fetch
`https://raw.githubusercontent.com/vercel-labs/web-interface-guidelines/main/command.md`
**once per run** and apply. Contrast, motion, focus order, responsive overflow, and
interactive behavior usually **cannot be proven from source** — label these
**Needs Runtime Verification**, not Confirmed.

---

## Reuse Identification

The highest-value part of the review. Be **conservative** — don't invent abstractions.

Recommend extraction only when:
- a **simple** pattern appears **3+ times**, or
- **complex/high-risk** logic (auth, cache keys, money, dates) appears **2+ times**, or
- centralizing fixes a **consistency, security, or cache-correctness** problem.

Before proposing anything new:
1. **Search for an existing** component/hook/query-factory/util that already does it.
2. Find the **nearest common ownership boundary**; prefer **feature-local** reuse.
   Promote to shared code only when multiple features genuinely need it.
3. State the **tradeoff/cost** of the extraction. Avoid generic dumping grounds like
   `components/common/SharedComponent.tsx` with no domain concept.

Scan both **service/logic** (repeated `fetch`/`useQuery`/`useMutation`, auth guards,
validation schemas, query-key factories, polling/debounce effects) and **UI/UX**
(duplicated card/list/empty/loading/error markup, variant-shaped prop patterns).

---

## Workflow — three passes (parallel scan → aggregate → verify)

### Project context (once)
1. Detect the **package runner** from the lockfile: `pnpm-lock.yaml` → `pnpm dlx`,
   `bun.lock`/`bun.lockb` → `bunx --bun`, else `npx`.
2. If `components.json` exists, run `<runner> shadcn@latest info --json`
   (read-only; first run may download the package — allow a generous timeout). Parse
   whatever fields are present — typically `rsc`, `aliases`, `iconLibrary`,
   `tailwind.version`, `resolvedPaths`. **Field names vary; don't assume a fixed
   schema.** If the command fails or stalls, **read `components.json` directly** and
   move on — don't retry. **Never run `init`, `add`, or any modifying command.**
3. From `package.json` note the **React version** (gates the React 19 rules),
   **TypeScript version**, **Tailwind version** (gates which Tailwind agent runs and
   v3-vs-v4 rules), and the framework (Next App Router vs Vite vs React Router). This
   context is passed to each parallel agent in Pass 2.

### Pass 1 — inventory & candidate discovery (cheap, ripgrep)
Discover files (exclude build dirs; don't traverse them):
```
rg --files -g '*.ts' -g '*.tsx' -g '*.js' -g '*.jsx' -g '*.css' \
  -g '!node_modules/**' -g '!.next/**' -g '!dist/**' -g '!build/**' -g '!coverage/**'
```
Then find candidate signals in **one batched call** (not ten separate greps), and
exclude the generated primitives so `forwardRef`/`useContext` hits don't flood from
`components/ui/*`:
```
rg -n --no-heading -g '!**/components/ui/**' -g '!node_modules/**' -g '!.next/**' \
  'useEffect|use(Query|Mutation)|fetch\(|axios|"use client"|forwardRef|useContext|dangerouslySetInnerHTML|space-[xy]-|#[0-9a-fA-F]{3,6}' \
  <scope>
```
Include `.ts`/`.css` — query hooks, schemas, API clients, auth utils, query-key
factories, and tokens live outside `.tsx`. Eyeball the output for repeated
loading/error/empty markup and near-identical query keys/schemas (reuse candidates).

### Pass 2 — best-practice scan (inline by default; parallel only for latency)
The three best-practice domains are `shadcn-best-practices`,
`tailwind-best-practices` (skip if no Tailwind), and `typescript-best-practices`.

**"Files in scope" = the resolved file list from Pass 1** (not the count of grep
hits). Use that number for the threshold below.

**Inline is the default and is the token-cheaper path.** Parallel subagents each
*cold-read the same files* and load their own skill bodies, so parallel costs **more
tokens** — it only buys lower wall-clock latency. Choose it only when latency matters.

- **Inline** — scope is `--changed`, an explicit path/glob, or **≤ 15 files in
  scope**. Process **one domain at a time**: invoke a single best-practice skill,
  emit that domain's findings, then move to the next. **Do not load all three skill
  bodies at once** — stacking them (plus the Vercel skills and the fetched guidelines)
  bloats context and degrades the review, especially on Sonnet. Reuse files already
  read in this pass; don't re-read.
- **Parallel** — scope is `--all` **or > 15 files** *and* you want lower latency:
  dispatch **three `general-purpose` subagents concurrently** (one tool block, three
  `Agent` calls), one skill each. **Do not use `Explore`** — it's a read-only locator
  that stops early and is unsuited to rule analysis.

**Subagent dispatch must be self-contained** — the agent does not have this SKILL.md.
Each prompt must include: (1) the resolved file list; (2) the project-context values
inline (package runner, React/TS/Tailwind versions, `rsc`, aliases, Tailwind major
version); (3) "invoke skill `<name>`, **report only — never edit**, skip generated
`components/ui/*`"; and (4) the literal Output Format block copied from this file so
all three return identically shaped findings.

**Aggregate** the three result sets together with the main reviewer's own
`vercel-react-best-practices`, `vercel-composition-patterns`, and
`web-design-guidelines` passes. Where the same line is flagged by more than one
agent/domain (e.g. a custom loading indicator hitting both shadcn and a rendering
rule), **merge into one finding** with a single primary category and multiple rule
tags. Reconcile conflicting severities to the highest.

### Pass 3 — correctness & reliability verification (main reviewer, not parallel)
This pass is **last and runs in the main thread** — it needs cross-file
understanding the per-skill agents don't have. **Reuse files already read in earlier
passes (the harness tracks file state — don't re-read them);** only read *new*
direct dependencies, shared abstractions, related CSS/token files, and the **call
sites** needed to judge actual behavior. Apply Domain 1 (Correctness &
Reliability) and the Reuse scan, and **verify the aggregated findings**: drop any
that don't hold against real behavior, upgrade severity where a "style" finding
actually causes a bug (e.g. a bad query key is a cache defect, not a nit), and
confirm each `file:line` reference resolves. Correctness findings always sort above
the aggregated best-practice findings.

---

## Output Format

Each finding carries **severity**, **confidence**, a **rule tag**, and **impact**:

- **Severity:** Critical | High | Medium | Low
- **Confidence:** Confirmed | Likely | Needs Runtime Verification
- Security and client-only-auth findings are **High or above**.

**Noise budget:** cap **Low**-severity findings at **5 per category**; collapse the
rest into one summary line (e.g. "+7 more `space-y-*`→`gap-*` across 4 files"). Never
truncate Critical/High/Medium findings.

```
## Correctness & Reliability
- [HIGH][Confirmed] src/features/profile/ProfileDialog.tsx:42
  [a11y-dialog-title][shadcn-dialog] DialogContent has no accessible title; screen
  readers can't identify the dialog.
  Fix: add DialogTitle (sr-only if no visible title).

## Performance
- [MEDIUM][Likely] src/components/CharacterList.tsx:42
  [async-parallel] Independent fetches awaited serially.
  Fix: Promise.all([fetchUser(), fetchRoles()]).

## Architecture
- [LOW][Confirmed] src/components/Card.tsx:8
  [architecture-avoid-boolean-props] 4 mode booleans (compact/editable/selectable/
  highlighted) → consider explicit variants or composition.

## shadcn/ui Compliance
- [LOW][Confirmed] src/components/Form.tsx:14  [shadcn-spacing] space-y-4 → flex flex-col gap-4.

## Tailwind CSS
- [MEDIUM][Confirmed] src/styles/app.css:1  [tw-v4-import] Uses v3 @tailwind directives in a v4 project → @import "tailwindcss".

## TypeScript
- [HIGH][Confirmed] src/api/client.ts:88  [ts-no-any] `res as any` discards the response type; downstream access is unchecked.

## Web Interface Guidelines
- [MEDIUM][Needs Runtime Verification] src/components/Header.tsx:31
  [contrast] Muted-on-muted text may fail WCAG AA; verify in browser.

## Reuse Opportunities
- [Service] fetchCharacter(id) duplicated in CharacterPage/CharacterCard/EditModal
  (3×) → extract feature-local useCharacter(id). Tradeoff: one more hook file;
  centralizes the query key so invalidation stays correct.
```

**Keep each finding to 1–2 lines.** Include an **API sketch only for the top 3
reuse opportunities**, as a short code block under the finding:
```ts
// src/features/character/useCharacter.ts
function useCharacter(id: string): UseQueryResult<Character>
```

### Coverage & Limitations (always include)
- Scope requested / resolution (`--changed`, `--all`, glob)
- Files discovered vs. inspected vs. skipped (and why)
- Framework + React/TypeScript/Tailwind versions; whether RSC rules applied
- How the best-practice scan ran — **inline** or **parallel** (with file count and
  threshold) — and which of shadcn / tailwind / typescript ran or were skipped, and
  why (e.g. Tailwind skipped — project doesn't use it)
- Whether shadcn context loaded; whether guidelines were fetched
- Whether lint/typecheck/tests were run (this skill does **not** run them by default)
- Areas needing **runtime verification** that were not confirmed

### Summary & Rating
One paragraph with the **top 3 highest-impact changes** (each with effort S/M/L) and
an overall rating by this rubric:
- **Excellent** — no Critical/High; few Mediums; strong reuse.
- **Good** — no Critical; isolated Highs; mostly Medium/Low.
- **Needs Work** — multiple Highs or one Critical; notable duplication.
- **Poor** — multiple Criticals, or systemic correctness/security gaps.

State if the rating is provisional because key areas need runtime verification.
