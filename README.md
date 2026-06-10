# NajaEchoPortal

Org-management utilities for the [Naja Echo](https://robertsspaceindustries.com/) organisation in Star Citizen.

## Stack

| Layer | Technology |
|-------|------------|
| Backend API | ASP.NET Core Web API (.NET 10 LTS) |
| Frontend | React 18 + TypeScript + Vite |
| Database | PostgreSQL 16 |
| Auth | Discord OAuth2 (authorization-code flow) |
| Session | HTTP-only server-managed cookies |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) and npm 10+
- [Docker](https://docs.docker.com/get-docker/) (for the local PostgreSQL container)
- A [Discord application](https://discord.com/developers/applications) with a redirect URI configured (see below)

---

## One-time setup

### 1. Discord application

1. Go to the [Discord Developer Portal](https://discord.com/developers/applications) and create a new application.
2. In **OAuth2 → General**, add this redirect URI:
   ```
   http://localhost:5080/api/auth/discord/callback
   ```
3. Copy the **Client ID** and **Client Secret** — you'll need them in the next step.

### 2. Backend secrets

The backend requires Discord credentials and a database connection string. These are stored using
`dotnet user-secrets` so they are never committed to the repository.

```bash
cd backend/src/NajaEcho.Api

dotnet user-secrets set "Discord:ClientId" "<your-client-id>"
dotnet user-secrets set "Discord:ClientSecret" "<your-client-secret>"
dotnet user-secrets set "ConnectionStrings:Default" \
  "Host=localhost;Database=najaecho;Username=najaecho;Password=najaecho"
```

Secrets are stored in your OS user profile outside the repository and automatically loaded at
runtime when `ASPNETCORE_ENVIRONMENT` is `Development` (the default).

### 3. Install frontend dependencies

```bash
cd frontend
npm install
```

---

## Running the application

All commands assume you are in the repository root unless noted otherwise.

### Step 1 — Start PostgreSQL

```bash
docker compose up -d postgres
```

Wait for the health check to pass:

```bash
docker compose ps   # Status should show "healthy"
```

### Step 2 — Apply database migrations

```bash
cd backend
export PATH="$PATH:$HOME/.dotnet/tools"   # ensure dotnet-ef is on PATH
dotnet ef database update \
  --project src/NajaEcho.Infrastructure/NajaEcho.Infrastructure.csproj \
  --startup-project src/NajaEcho.Api/NajaEcho.Api.csproj
```

### Step 3 — Start the backend

In one terminal:

```bash
cd backend/src/NajaEcho.Api
dotnet run
```

The API listens on `http://localhost:5080`. Verify it is healthy:

```bash
curl http://localhost:5080/api/health
# {"status":"ok"}
```

### Step 4 — Start the frontend

In a second terminal:

```bash
cd frontend
npm run dev
```

The React app is available at `http://localhost:5173`. The Vite dev server proxies all `/api`
requests to the backend automatically — no CORS setup needed for local development.

---

## Running tests

### Backend

```bash
cd backend

# Unit and integration tests (no Docker required):
dotnet test NajaEcho.slnx \
  --filter "FullyQualifiedName!~Infrastructure"

# All tests including Testcontainers PostgreSQL (requires Docker):
dotnet test NajaEcho.slnx
```

### Frontend

```bash
cd frontend
npm test          # watch mode
npm run test:run  # single run (CI)
```

---

## Project structure

```text
NajaEchoPortal/
├── backend/
│   ├── src/
│   │   ├── NajaEcho.Domain/            # Entities and domain rules
│   │   ├── NajaEcho.Application/       # Use cases, abstractions
│   │   ├── NajaEcho.Infrastructure/    # EF Core, PostgreSQL, Discord client
│   │   └── NajaEcho.Api/               # Endpoints, auth config, Program.cs
│   └── tests/
│       ├── NajaEcho.Domain.Tests/
│       ├── NajaEcho.Application.Tests/
│       ├── NajaEcho.Infrastructure.Tests/
│       └── NajaEcho.Api.Tests/
├── frontend/
│   └── src/
│       ├── features/
│       │   ├── auth/                   # Sign-in, sign-out, protected route
│       │   └── dashboard/              # Authenticated dashboard
│       ├── components/ui/              # shadcn/ui primitives
│       └── lib/                        # API client, query client, utils
├── specs/                              # Spec Kit design artifacts
├── docker-compose.yml
└── README.md
```

---

## Environment variables reference

The backend reads configuration from `appsettings.json` and `dotnet user-secrets` (dev) or
environment variables (prod). All sensitive values **must** come from secrets — never from
`appsettings.json`.

| Key | Description | Example |
|-----|-------------|---------|
| `Discord:ClientId` | Discord OAuth2 application client ID | `1234567890` |
| `Discord:ClientSecret` | Discord OAuth2 application client secret | `abc123...` |
| `ConnectionStrings:Default` | PostgreSQL connection string | `Host=localhost;Database=najaecho;...` |
| `Frontend:Origin` | CORS-allowed frontend origin | `http://localhost:5173` |

### Viewing and managing secrets

```bash
cd backend/src/NajaEcho.Api

# List all stored secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "Discord:ClientSecret"

# Clear all secrets
dotnet user-secrets clear
```

---

## Stopping the stack

```bash
docker compose down          # stop containers, keep data volume
docker compose down -v       # stop containers and delete the database volume
```
