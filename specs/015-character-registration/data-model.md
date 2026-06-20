# Phase 1 Data Model: Character Registration & RSI Verification

Two new tables, one EF Core migration. PostgreSQL, snake_case columns (project convention). Owner is
`AspNetUsers` (ASP.NET Core Identity). Maps the spec's two Key Entities.

## Entity: Character

Durable record of a verified Star Citizen identity linked to a member. Table: `characters`.

| Field | Column | Type | Constraints | Notes |
|-------|--------|------|-------------|-------|
| Id | `id` | `uuid` | PK | App-generated GUID. |
| OwnerUserId | `owner_user_id` | `uuid` | NOT NULL, FK → `AspNetUsers(Id)` (`OnDelete Cascade`) | The member who owns this character. |
| Name | `name` | `varchar(100)` | NOT NULL | RSI Community Moniker (display name) scraped from the citizen page during verification, e.g. `G8trdone`. Falls back to `Handle` if the moniker can't be parsed (research R1/R4). |
| Handle | `handle` | `varchar(100)` | NOT NULL | RSI username, stored exactly as the member typed it. |
| CreatedAt | `created_at` | `timestamptz` | NOT NULL | Registration timestamp. |

**Indexes / constraints**
- `ux_characters_handle_lower` — **UNIQUE on `lower(handle)`** — global, case-insensitive handle
  uniqueness across all members (FR-005, SC-002, edge case). Functional index (raw SQL in the migration).
- `ix_characters_owner_user_id` — non-unique, for the per-member list query (US3, FR-008).
- FK `fk_characters_owner_user_id` → `AspNetUsers(Id)`.

**Relationships**: a member (`AspNetUsers`) has many `Character`s; each `Character` has exactly one owner.

**Validation (Application layer, before insert)**
- `handle`: required, trimmed, non-empty, ≤ 100 chars (the only member-supplied value).
- `name`: not supplied by the client; set from the scraped RSI moniker, trimmed and truncated to ≤ 100
  chars; if the moniker is missing/blank, set to `handle`.
- Handle must not already exist case-insensitively (pre-check + DB unique index race guard).

## Entity: PendingCharacterRegistration

Ephemeral in-progress verification attempt. Table: `pending_character_registrations`. Exists only during
the verification window; deleted on success, overwritten when a member restarts after expiry.

| Field | Column | Type | Constraints | Notes |
|-------|--------|------|-------------|-------|
| Id | `id` | `uuid` | PK | App-generated GUID. |
| OwnerUserId | `owner_user_id` | `uuid` | NOT NULL, FK → `AspNetUsers(Id)` (`OnDelete Cascade`) | The member registering. |
| Token | `token` | `varchar(64)` | NOT NULL | High-entropy verification token (research R2). Sensitive — never logged. |
| ExpiresAt | `expires_at` | `timestamptz` | NOT NULL | `CreatedAt + 30 min`. |
| CreatedAt | `created_at` | `timestamptz` | NOT NULL | When this token was issued. |

**Indexes / constraints**
- `ux_pending_character_registrations_owner_user_id` — **UNIQUE on `owner_user_id`** — at most one
  pending registration per member, enabling token reuse (FR-010, research R3).
- FK `fk_pending_character_registrations_owner_user_id` → `AspNetUsers(Id)`.

**Relationships**: a member has zero-or-one pending registration.

**Domain behavior** (`PendingCharacterRegistration`)
- `static Create(Guid ownerUserId, DateTimeOffset now)` — generates a fresh high-entropy token and sets
  `ExpiresAt = now + ValidityWindow`. `ValidityWindow` is a single constant = 30 minutes (FR-002, FR-006).
- `bool IsExpired(DateTimeOffset now)` → `now >= ExpiresAt`.

## Lifecycle / State

```
StartRegistration(owner):
  row = pending.get(owner)
  if row != null and not row.IsExpired(now):   return row.Token            # reuse (FR-010, US1.2)
  else:                                          pending.upsert(Create(owner, now))  # fresh (US1.3)

VerifyCharacter(owner, handle):
  pending = pending.get(owner)
  if pending == null or pending.IsExpired(now):  throw TokenExpired         # FR-006, US2.3
  if characters.handleExists(handle):            throw HandleAlreadyClaimed # FR-005, US2.4 (before RSI fetch)
  page = rsi.fetch(handle)                        # 404 → RsiProfileNotFound (US2.5); error → RsiUnreachable (edge)
  if not page.Content.contains(pending.Token):   throw TokenNotFound        # US2.2
  name = page.DisplayName ?? handle               # scraped Community Moniker (research R1/R4)
  characters.add(Character{owner, name, handle, now})  # unique-violation → HandleAlreadyClaimed
  pending.remove(owner)                           # discard token on success
  return character
```

Character records are immutable in v1 (no edit, no delete — out of scope). Pending rows never accumulate:
one-per-member by unique index, deleted on success, overwritten on restart-after-expiry.

## Out of scope (no schema)
- Character deletion / deregistration, re-verification, audit/history of attempts.
- Any scraped RSI data beyond the token match and the display-name moniker (avatar, org, account age).
