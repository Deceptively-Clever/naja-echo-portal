# /prepare-commit

Run all pre-commit and CI checks locally, auto-fixing where possible, to verify the branch is
ready to commit and push.

---

## Instructions

### Step 1 — Determine what changed

Run `git diff --name-only HEAD` and `git status --short` to see all modified and untracked files.
Determine:

- **Frontend changed**: any file path starts with `frontend/`
- **Backend changed**: any file path starts with `backend/`

If nothing has changed from HEAD, report that and stop.

---

### Step 2 — Launch subagents in parallel

Spawn the backend and frontend check agents **at the same time in a single message** (one Agent
tool call per changed side). Use a fresh agent (no subagent_type needed). Do not wait for one to
finish before launching the other.

Only launch the agents for sides that have changes (from Step 1). If only backend changed, launch
only the backend agent. If only frontend changed, launch only the frontend agent.

---

#### Backend subagent prompt

Use this prompt verbatim when spawning the backend check agent. It is fully self-contained.

---

```
You are running CI checks for the NajaEchoPortal backend (.NET 10 / ASP.NET Core), with
auto-fix enabled for fixable issues.

Working directory for all commands: /home/rdurham/source/NajaEchoPortal/backend

Run the following steps in order. Stop at the first unrecoverable failure (one that could not be
fixed automatically). Report each step's result as you go. Show full output only on failure.

---

## Step 1 — Restore
Command: dotnet restore NajaEcho.slnx
Expected: exits 0.
On failure: report immediately and stop. Restore failures are not auto-fixable.

---

## Step 2 — Build (Release)
Command: dotnet build NajaEcho.slnx --no-restore -c Release
Expected: exits 0 with no errors.
On failure: report immediately and stop. Compilation errors require manual fixes.

---

## Step 3 — Code analysis (with auto-fix)
Command: dotnet build NajaEcho.slnx --no-restore -c Release /p:EnforceCodeStyleInBuild=true /p:EnableNETAnalyzers=true
Expected: exits 0. This mirrors the pre-commit hook and CI.

On failure — attempt auto-fix:
  1. Run: dotnet format NajaEcho.slnx --no-restore --severity error
     This rewrites source files in place to fix code style and formatting violations.
  2. Report which files were modified (run `git diff --name-only` after to list them).
  3. Re-run the code analysis command above.
  4. If it now passes: mark as FIXED and continue.
  5. If it still fails: mark as FAIL (partial fix) and stop. Include the remaining errors.

---

## Step 4 — Tests
Command: dotnet test NajaEcho.slnx --no-build -c Release --results-directory TestResults --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage" --settings:coverlet.runsettings
Expected: exits 0, all tests pass.
On failure: report immediately and stop. Test failures are not auto-fixable.

---

## Return format

Return a structured summary at the end:

BACKEND RESULTS
- Restore:        PASS / FAIL
- Build:          PASS / FAIL / SKIPPED
- Code analysis:  PASS / FIXED (list changed files) / FAIL (partial fix — list remaining errors) / SKIPPED
- Tests:          PASS / FAIL / SKIPPED

OVERALL: PASS or FAIL

If any step failed or was partially fixed, include the relevant error output below the table.
If files were auto-fixed, list them clearly so the caller can inform the user.
```

---

#### Frontend subagent prompt

Use this prompt verbatim when spawning the frontend check agent. It is fully self-contained.

---

```
You are running CI checks for the NajaEchoPortal frontend (React 19 / TypeScript / Vite), with
auto-fix enabled for fixable issues.

Working directory for all commands: /home/rdurham/source/NajaEchoPortal/frontend

Run the following steps in order. Stop at the first unrecoverable failure (one that could not be
fixed automatically). Report each step's result as you go. Show full output only on failure.

---

## Step 1 — Install
Command: npm ci
Expected: exits 0, node_modules installed cleanly from package-lock.json. Do not use npm install.
On failure: report immediately and stop. Install failures are not auto-fixable.

---

## Step 2 — Lint (with auto-fix)
Command: npm run lint
  (This runs: eslint .)
Expected: exits 0 with no lint errors. This mirrors the pre-commit hook and CI.

On failure — attempt auto-fix:
  1. Run: npx eslint . --fix
     This rewrites source files in place for all auto-fixable rules (formatting, simple style).
     Note: logic errors, type errors, and some rules cannot be auto-fixed.
  2. Report which files were modified (run `git diff --name-only` after to list them).
  3. Re-run: npm run lint
  4. If it now passes: mark as FIXED and continue.
  5. If it still fails: mark as FAIL (partial fix — remaining errors need manual attention)
     and stop. Include the remaining lint errors.

---

## Step 3 — Tests with coverage
Command: npm run test:run -- --coverage
Expected: exits 0, all tests pass.
On failure: report immediately and stop. Test failures are not auto-fixable.

---

## Step 4 — Build
Command: npm run build
  (This runs: tsc -b && vite build)
Expected: exits 0, dist/ produced with no type or build errors.
On failure: report immediately and stop. TypeScript and build errors require manual fixes.

---

## Return format

Return a structured summary at the end:

FRONTEND RESULTS
- Install:  PASS / FAIL
- Lint:     PASS / FIXED (list changed files) / FAIL (partial fix — list remaining errors) / SKIPPED
- Tests:    PASS / FAIL / SKIPPED
- Build:    PASS / FAIL / SKIPPED

OVERALL: PASS or FAIL

If any step failed or was partially fixed, include the relevant error output below the table.
If files were auto-fixed, list them clearly so the caller can inform the user.
```

---

### Step 3 — Collate results and report

Wait for both subagents to finish. Then print a unified summary table:

| Check | Result |
|-------|--------|
| Backend: restore | ✓ / ✗ / skipped |
| Backend: build | ✓ / ✗ / skipped |
| Backend: code analysis | ✓ auto-fixed / ✗ partial fix / ✗ failed / skipped |
| Backend: tests | ✓ / ✗ / skipped |
| Frontend: install | ✓ / ✗ / skipped |
| Frontend: lint | ✓ auto-fixed / ✗ partial fix / ✗ failed / skipped |
| Frontend: tests | ✓ / ✗ / skipped |
| Frontend: build | ✓ / ✗ / skipped |

If any files were auto-fixed, list them under an **"Auto-fixed files"** section so the user knows
to review and stage them before committing.

If all applicable steps passed (including after auto-fix): **"Branch is ready to commit and push."**

If any step failed or had a partial fix: **"Branch is NOT ready — fix the remaining issues above
before committing."** Show the error output from the failing subagent(s).

Do not suggest force-pushing or skipping hooks.
