# Phase 0 Research: Item Data Import

All decisions below are grounded in the existing ship-import implementation (feature 006) and the
project constitution. No open `NEEDS CLARIFICATION` items remain.

## R1 — Reuse the shared `IImportCoordinator` for one-at-a-time

**Decision**: Inject the existing singleton `IImportCoordinator` into both the
`RefreshCategoriesHandler` and `ImportItemsHandler`. Acquire at the start of the operation, release
in a `finally`. For all-category import, acquire **once** for the whole run (not per category).

**Rationale**: `ImportCoordinator` is registered as a singleton wrapping `SemaphoreSlim(1,1)` and is
already used by `ImportShipsHandler`. Reusing the same instance makes "only one import or refresh at
a time" span ships + categories + items with zero new code. Matches the spec's "matching the
existing ship import behavior".

**Alternatives considered**: A separate per-feature lock — rejected: would allow a category refresh
and an item import to run simultaneously, violating FR-018 and risking inconsistent reads of
categories mid-import.

## R2 — Two separate UEX typed clients

**Decision**: Add `IUexCategoryClient` (`FetchAllCategoriesAsync`) and `IUexItemClient`
(`FetchItemsByCategoryAsync(int categoryId)`), each registered with
`services.AddHttpClient<,>(...)` using the same base URL config key/default as the vehicle client.
Both parse the `{ "data": [...] }` envelope and throw `InvalidOperationException` when `data` is
missing or not an array (so a malformed category response becomes a per-category failure).

**Rationale**: Mirrors `UexVehicleClient` exactly (typed client, base URL, envelope handling). Two
clients keep the categories endpoint and items endpoint physically separate, structurally
guaranteeing item import never calls `/categories`.

**Alternatives considered**: One combined client with two methods — acceptable but a single
responsibility per client matches the existing one-client-per-feed precedent and reads cleaner in
DI. Either is fine; plan uses two for symmetry with the route separation requirement.

## R3 — Item identity, upsert, and `uuid = null` handling

**Decision**: `uuid` (string) is the stable identity and carries a **unique index**. The UEX `id`
is stored as `uex_id` (external/source route id) with a non-unique index for traceability but is
**not** the identity. During import, records with `uuid == null` are filtered out *before* upsert,
counted into `ItemsSkippedNoUuid`, and never cause failure. Remaining records upsert by `uuid`:
match → update (+ restore if soft-deleted), no match → insert.

**Rationale**: Directly encodes the spec business rules. Mirrors `ShipRepository.BulkUpsertAsync`
but swaps the match key from `UexId` to `Uuid` and adds the null-skip pre-filter.

**Alternatives considered**: Using `uex_id` as identity (rejected — spec explicitly says `id` is a
source route id, not stable); storing null-uuid rows with a synthetic key (rejected — spec says
skip, and they would pollute normal use).

## R4 — Category-scoped soft-delete + restore

**Decision**: `ItemRepository.BulkUpsertForCategoryAsync(int idCategory, IReadOnlyList<Item> incoming, ct)`
runs in one transaction: load existing items **where `IdCategory == idCategory`**, upsert incoming
by `uuid`, then soft-delete only those Active rows in **that same category** whose `uuid` is absent
from the incoming set. A matched row that was `SoftDeleted` is set back to `Active`
(`SoftDeletedAt = null`) and counted as restored, then updated with latest data.

**Rationale**: The ship soft-delete query (`status == Active && !incomingIds.Contains`) is global;
the items version adds the `IdCategory == idCategory` predicate so importing category A never
soft-deletes items of category B (FR-014). Restore-on-reappear (FR-015) is the ship "reactivated"
branch, unchanged in spirit.

**Edge case**: An item that moves from category A to B in the source. When B is imported it inserts
/updates the item under B; when A is next imported the item is absent from A's response and gets
soft-deleted *as the A-scoped row*. Because identity is `uuid`, an item is expected to belong to one
category at a time. If the same `uuid` legitimately appears under two categories in the source,
the second import updates the single row's `IdCategory` — documented as accepted v1 behavior; the
unique-on-`uuid` index means one physical row per item regardless of category churn.

**Alternatives considered**: Global soft-delete (rejected — would delete other categories' items on
a single-category import); hard delete (rejected — spec requires preservation for references/
troubleshooting).

## R5 — All-category import: per-category failure isolation

**Decision**: `ImportItemsHandler` with a null `CategoryId` loads all eligible local categories
(`type == "item"`), then loops: for each, fetch + upsert inside a try/catch. A failure (HTTP error,
malformed `data`, etc.) is caught, recorded as a `CategoryImportError`, the category counted as
failed, and the loop continues. Item-level totals accumulate across succeeded categories. The lock
is held for the whole run. The result's overall status is derived: all succeeded ⇒ success, some
failed ⇒ "completed with errors", all failed ⇒ failure.

**Rationale**: Encodes FR-017 and the all-category behaviour/acceptance scenarios. A single-category
import (`CategoryId` set) uses the same per-category routine but surfaces the failure directly
(the endpoint returns a non-2xx / the result carries the single error).

**Alternatives considered**: Fail-fast on first error (rejected — violates FR-017); parallel
category fetches (rejected — YAGNI at this scale and complicates the shared lock + summary; also
risks hammering UEX).

## R6 — Eligibility = `type == "item"`

**Decision**: The repository exposes `GetEligibleAsync()` returning local categories where
`Type == "item"`. The all-category import and the selector's import affordances use this set.
`GetCategoriesHandler` returns the full local set (with all selector context fields) so the UI can
display non-item categories greyed/ineligible if desired, but only `type == "item"` rows expose
import actions.

**Rationale**: FR-008. Keeping eligibility a repository concern keeps the rule in one place.

## R7 — "Last refreshed" timestamp

**Decision**: Track per-category `ImportedAt`/`UpdatedAt` (as ships do) and derive the page-level
"categories last refreshed" as the max `UpdatedAt` across `item_categories` (or the refresh
operation's end time). The `GetCategories` response includes this aggregate (nullable — omitted/
unknown when no categories exist, satisfying the spec edge case).

**Rationale**: Avoids a separate settings/metadata table (YAGNI). The aggregate is cheap and
truthful. FR-004.

**Alternatives considered**: A dedicated `last_refreshed_at` metadata row (rejected — extra table
for one timestamp; the max-of-column derivation is sufficient for v1).

## R8 — Summaries are transient DTOs (no persistence)

**Decision**: `RefreshCategoriesResult` and `ImportItemsResult` are returned in the HTTP response
and rendered for the current session only. No history table (explicit v1 out-of-scope). Both carry
`StartedAt`, `CompletedAt`, and a computed `Duration`.

**Rationale**: Matches `ImportShipsResult` (transient) and the spec's "no import history
persistence" exclusion.

## R9 — Stored item fields & excluded fields

**Decision**: Promote the spec's listed item fields to typed columns where they drive querying or
display identity (`uex_id`, `uuid`, `id_category`, `name`, `section`, `category`, `slug`,
`game_version`, the `is_*` flags, etc.); retain the full source record as a `jsonb raw_data` column
(as ships do) **with `attributes` and `screenshot` stripped before storage**. Categories similarly
get typed columns for all listed fields plus the selector-driving flags.

**Rationale**: The `jsonb` + promoted-columns hybrid is the established `sc.ships` pattern and keeps
the selector/filter queries indexable while preserving source fidelity. Stripping `attributes`
(deprecated) and `screenshot` (v1-excluded) at map time honours the data-handling requirements
(FR-021, FR-022). See [data-model.md](./data-model.md) for the exact column list.

**Alternatives considered**: Storing only `jsonb` (rejected — filtering/sorting the selector and
guaranteeing the `uuid` unique constraint need real columns); storing every field as a column with
no `jsonb` (rejected — loses source fidelity and is brittle to UEX additions).
