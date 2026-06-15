# Quickstart & Validation: Add item quality

Validation guide for `001-add-item-quality`. See [data-model.md](./data-model.md),
[research.md](./research.md), and [contracts/openapi.yaml](./contracts/openapi.yaml).

## Prerequisites

- PostgreSQL is running and reachable by backend connection string.
- Backend and frontend dependencies are installed.
- At least one authenticated user with Quartermaster (or Admin) role exists.

## Setup

```bash
# Apply backend migration
cd backend
dotnet ef database update --project src/NajaEcho.Infrastructure --startup-project src/NajaEcho.Api

# Regenerate frontend API types if script points at this feature contract
cd ../frontend
npm run gen:api:warehouse
```

Run apps:

```bash
# backend
cd backend/src/NajaEcho.Api
dotnet run

# frontend (separate terminal)
cd frontend
npm run dev
```

## Automated tests

```bash
cd backend
dotnet test NajaEcho.slnx

cd ../frontend
npm run test:run
```

## Manual validation scenarios

1. **Add inventory with explicit quality**
   - As Quartermaster, add an item with `quality=750`.
   - Expected: request succeeds; returned row has `quality=750`; table shows 750.

2. **Add inventory without quality**
   - Omit quality in add flow.
   - Expected: request succeeds with `quality=500`.

3. **Boundary acceptance**
   - Add with `quality=1`, then with `quality=1000`.
   - Expected: both succeed.

4. **Out-of-range rejection**
   - Add with `quality=0` and `quality=1001`.
   - Expected: validation errors; no row persisted/updated.

5. **Existing-row increment behavior**
   - Add same `item+owner+location` twice with different quality values.
   - Expected: one row remains; quantity increments; quality reflects latest submitted value.

6. **Ship-components add parity**
   - Add through ship-components endpoint/UI with explicit and omitted quality.
   - Expected: same validation/default behavior as standard warehouse add.

7. **Read/list propagation**
   - Load both `/api/warehouse/items` and `/api/warehouse/ship-components`.
   - Expected: each row includes `quality` and UI renders it.

## Expected outcomes

- Quartermasters can explicitly set quality during item add.
- Default quality is always `500` when omitted.
- Invalid quality values are rejected consistently.
- Quality persists and is returned by warehouse list/read APIs.

## Validation run

- Backend solution build succeeds.
- Focused backend tests for add/inventory/ship-component quality scenarios pass.
- Frontend tests and build pass with quality field updates in forms/tables.
