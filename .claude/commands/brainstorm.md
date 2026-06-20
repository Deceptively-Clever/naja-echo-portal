# /brainstorm

Turn a rough feature idea into a structured backlog entry.

## Input

`$ARGUMENTS` — a rough description of the idea (required)

---

## Instructions

You are a product-thinking collaborator for **NajaEchoPortal**, a Star Citizen org-management
portal for the Naja Echo organization. The stack is .NET 10 / ASP.NET Core backend (Clean
Architecture), React 19 + TypeScript frontend, PostgreSQL 16, Discord OAuth2 auth.

Your job is to take the user's raw idea and work it into a structured entry ready for
`specs/BACKLOG.md` — not a full spec, but enough to understand scope, value, dependencies, and
readiness.

---

### Step 0 — Check for existing session

Before doing anything else, check `.specify/memory/brainstorms/` for an existing memory file
whose `slug` or `idea` field closely matches `$ARGUMENTS`.

If a match is found:
- Tell the user: "Resuming brainstorm for `<slug>` (saved <date>, status: <status>)." and
  display the previously saved output.
- If `verdict` is "Ready to spec", show the `## Feature Description` section and remind the
  user they can run `/speckit-specify @.specify/memory/brainstorms/<slug>.md` directly.
- If `## Unresolved Questions` is non-empty, list them prominently.
- Ask: "Would you like to continue from here, answer the open questions, update any sections,
  or start fresh?"
- Do not re-run the steps below unless the user asks to start fresh or update specific sections.
- If the user answers any unresolved questions, update the memory file: add the Q&A pairs to
  `## Resolved Questions` and remove them from `## Unresolved Questions`. Update `verdict` in
  frontmatter to "Ready to spec" if all questions are now resolved. Do not rewrite
  `## Feature Description` unless the user signals they are done (see below).
- If the user says they are done, ready to spec, or similar, synthesize a final
  `## Feature Description`: read all structured sections (Problem Statement, Scope, Auth
  Surface, Resolved Questions, Draft P1 User Story) and write a cohesive 4-7 sentence
  description incorporating what the feature does, who benefits, the auth model, in-scope
  behaviour, out-of-scope exclusions, and answers to all resolved questions. Overwrite the
  existing `## Feature Description` in the file. Set `verdict` to "Ready to spec" and `status`
  to "ready-to-spec" in frontmatter. Then confirm: "Brainstorm finalized. Run:
  `/speckit-specify @.specify/memory/brainstorms/<slug>.md`"

If no match is found, proceed with the steps below.

---

### Step 1 — Determine the next feature number

Read the `specs/` directory to find the highest existing `###-*` folder number, then add 1.
For example, if `014-warehouse-materials` is the highest, the next number is `015`.
Include this as the suggested branch/folder name in your output: `###-<kebab-slug>`.

### Step 2 — Restate the idea

In 2–3 sentences, restate what you understood. Identify any ambiguity and ask one clarifying
question if needed. If the idea is clear enough to proceed, skip the question and continue.

### Step 3 — Problem statement

One paragraph: what problem does this solve for Naja Echo members or Quartermasters? Who benefits
and how?

### Step 4 — Proposed scope

Two lists:

**Likely in scope** — the core behaviour needed to deliver value. Keep it minimal.

**Likely out of scope (v1)** — things that are related but not essential for a first version.
This is where you prevent scope creep before it starts.

### Step 5 — Auth surface

State the access model explicitly — do not leave this as an open question:

- **Read access**: who can view this data? (Any authenticated member, or Quartermaster only?)
- **Write access**: who can add/edit/delete? (Any authenticated member, Quartermaster only, or
  no writes in v1?)

If this is genuinely ambiguous from the idea description, flag it here and ask before continuing.

### Step 6 — Key open questions

2–4 questions that must be answered before a spec can be written. Focus on:
- Data source (does this need new SC catalog data, existing tables, or user-provided data?)
- UI surface (new page, new section on an existing page, a modal?)
- Integration points (does this depend on a feature that isn't built yet?)

Do not re-ask about auth — that was covered in Step 5.

**Known SC catalog tables** (for reference when asking data-source questions):
- `sc.commodities` — crafting/trade commodities (name, code, soft-delete)
- `sc.items` — game items (ships, components, gear); related: `sc.item_categories`, `sc.item_attributes`
- `sc.ships` — ship definitions
- `sc.ship_component_attributes` — ship component attribute view (read-only)

### Step 7 — Feature area

Tag the idea with one of the project's established feature areas (or "New Area" if it doesn't fit):

- **Auth** — sign-in, session, roles, Discord OAuth
- **UI/Theme** — layout, theming, navigation shell, dashboard chrome
- **Catalog Import** — importing or refreshing SC game data into the `sc.*` tables
- **Hangar** — member ship ownership and org fleet views
- **Warehouse** — org inventory tracking (items, components, materials)

### Step 8 — Closest precedent

Name the single closest existing spec folder and explain in one sentence what this idea reuses
from it and what is genuinely different. Read the `specs/` directory to identify the best match.

Example: "`012-warehouse-ship-components` — reuses the owner-scoped inventory table pattern and
Quartermaster write gate; differs in catalog source (`sc.commodities` vs `sc.items`) and decimal
quantity."

If there is no close precedent, say so explicitly.

### Step 9 — Dependencies

List any features from `specs/` that this idea depends on or closely relates to (beyond the
closest precedent named in Step 8). Note whether each is blocking or just related.

### Step 10 — Complexity estimate

One of: **Small** / **Medium** / **Large**

- Small: UI-only, no new endpoints, no new tables, no new catalog data
- Medium: 1–2 new endpoints, possibly one new table, straightforward frontend
- Large: New catalog data pipeline, multi-table schema, complex business rules, or significant
  new UI surface

### Step 11 — Constitution pre-check

Flag which of the project's six constitution principles have immediate implications for this idea.
One sentence per relevant principle. Skip principles with no specific implication.

- **I. API-Contract-First** — Does this add or change backend HTTP behaviour? If yes, an OpenAPI
  contract is required before implementation. If UI-only, explicitly note "No API contract changes
  required."
- **II. Test-First / TDD** — What layers need tests? (Domain/Application unit tests,
  Testcontainers integration tests, frontend component/hook tests)
- **III. Frontend/Backend Separation** — Does anything risk leaking DB logic to the frontend or
  bypassing the API contract?
- **IV. Simplicity / YAGNI** — Is there an existing pattern or table this should reuse rather
  than creating new abstractions? (Cross-reference Step 8.)
- **V. Observability** — Any structured-logging or sensitive-data concerns?
- **VI. Modular Monolith + Clean Architecture** — Which of the four backend layers are touched?
  Any cross-layer dependency risk?

### Step 12 — Draft P1 user story

Write one user story for the highest-priority user action this feature enables. Use the spec
template format exactly:

```
### User Story 1 — <Brief Title> (Priority: P1)

<One paragraph describing the user journey in plain language.>

**Why this priority**: <One sentence on why this is the most critical capability.>

**Independent Test**: <One sentence on how this story can be verified in isolation — e.g.,
"Can be fully tested by navigating to X and confirming Y.">

**Acceptance Scenarios**:

1. **Given** <initial state>, **When** <action>, **Then** <expected outcome>.
2. **Given** <initial state>, **When** <action>, **Then** <expected outcome>.
```

Include 2–4 acceptance scenarios. Keep them concrete and testable. This story is a starting point
for `/speckit-specify`, not a final spec.

### Step 13 — Readiness verdict

One of:

- **Ready to spec** — open questions are minor; enough is known to write a spec now. Next step
  (after Step 14 saves the memory file):
  ```
  /speckit-specify @.specify/memory/brainstorms/<slug>.md
  ```
- **Needs discussion** — one or more Step 6 questions are blocking; resolve them first, then
  re-run `/brainstorm` to update the memory file before moving to `/speckit-specify`.
- **Too small — just do it** — no spec needed; implement directly on a `###-<slug>` branch and
  open a PR referencing this brainstorm output.

### Step 14 — Save memory file

After presenting the full output, save a memory file to `.specify/memory/brainstorms/<slug>.md`
(where `<slug>` is the suggested `###-kebab-slug`). Create the directory if it does not exist.

This file is designed to be passed directly to `/speckit-specify` via the `@file` syntax:
`/speckit-specify @.specify/memory/brainstorms/<slug>.md`

Structure the file so speckit-useful content comes first and brainstorm-only metadata is
separated at the bottom. speckit-specify will read the whole file as its input — the top
sections should give it everything needed to write a complete spec with no NEEDS CLARIFICATION
markers.

The file format:

```markdown
---
slug: <###-kebab-slug>
title: <Human-readable feature title, 3-6 words>
idea: "<original $ARGUMENTS text verbatim>"
area: <Feature area from Step 7>
complexity: <Small | Medium | Large>
verdict: <Ready to spec | Needs discussion | Too small — just do it>
date: <YYYY-MM-DD>
status: brainstormed
---

# Brainstorm: <title>

> Use with: `/speckit-specify @.specify/memory/brainstorms/<slug>.md`

## Feature Description

<The enriched feature description: the Step 2 restatement expanded to incorporate the auth
model, in-scope behaviour, out-of-scope boundaries, and the answers to any resolved questions.
This is the primary input for speckit-specify — 3-6 sentences. A reader with no prior context
should be able to understand exactly what to build and what to exclude.>

## Auth Surface

- **Read**: <Step 5 read access>
- **Write**: <Step 5 write access>

## Scope

### In Scope
<Step 4 in-scope list>

### Out of Scope (v1)
<Step 4 out-of-scope list>

## Resolved Questions

<Q&A pairs resolved during this session. Format: "Q: <question> → A: <answer>"
If none, write "None yet.">

## Draft P1 User Story

<Step 12 output verbatim>

## Problem Statement

<Step 3 output>

---
*Sections below are for brainstorm resumption only — not primary inputs for speckit-specify.*

## Unresolved Questions

<Remaining open questions from Step 6 that still need answers before the spec can be written.
If all resolved, write "None.">

## Closest Precedent

<Step 8 output>

## Constitution Notes

<Step 11 output>
```

After saving, tell the user:

"Session saved to `.specify/memory/brainstorms/<slug>.md`."

If the verdict is **Ready to spec**, add:

"Run `/speckit-specify @.specify/memory/brainstorms/<slug>.md` to start the spec with full
context pre-loaded."

If the verdict is **Needs discussion**, add:

"Resolve the open questions above, then re-run `/brainstorm <idea>` to update this file before
moving to `/speckit-specify`."

### Step 15 — Backlog entry

Output the markdown row to add to `specs/BACKLOG.md`:

```markdown
| — | <###-suggested-slug> | <Area> | <Title> | <one-line description> | <Complexity> | <Readiness verdict> |
```

Then ask: **"Should I add this to `specs/BACKLOG.md`?"**

If yes:
1. Check whether `specs/BACKLOG.md` exists. If it does not, create it with this structure:

```markdown
# NajaEchoPortal Backlog

## In Progress

| # | Branch | Area | Title | Description | Complexity | Status |
|---|--------|------|-------|-------------|------------|--------|

## Planned

| # | Branch | Area | Title | Description | Complexity | Status |
|---|--------|------|-------|-------------|------------|--------|

## Ideas

| # | Branch | Area | Title | Description | Complexity | Readiness |
|---|--------|------|-------|-------------|------------|-----------|
```

2. Insert the new row under **Ideas** and write the file.
3. Update the `status` field in the memory file's frontmatter from `brainstormed` to `added-to-backlog`.
