# SlotBook

Multi-tenant appointment booking platform: an Angular frontend and a .NET backend in one repo (see `docs/adr/0006-monorepo-layout-and-test-stack.md`).

## Prerequisites

- .NET 10 SDK (`global.json` pins `10.0.100`, rolling forward to later 10.0 feature bands)
- Node 24.15 or later (Angular 22 requires at least 24.15.0) and npm

## Run locally

Start the API first, then the frontend, in two terminals.

```bash
# Terminal 1: API on http://localhost:5080
dotnet run --project src/backend/SlotBook.Api
```

```bash
# Terminal 2: frontend on http://localhost:4200
cd src/frontend
npm ci
npm start
```

Open <http://localhost:4200>. The page says "API is healthy" while the API runs, and "API is unavailable" when it is stopped.

The browser only talks to the dev server. The dev-server proxy (`src/frontend/proxy.conf.json`) forwards `/health` to the API, so the API needs no CORS configuration. If port 5080 is taken, change it in both `src/backend/SlotBook.Api/Properties/launchSettings.json` and `src/frontend/proxy.conf.json`.

Check the API alone: `curl -i http://localhost:5080/health` returns `200` with `{"status":"Healthy"}`.

## Test

```bash
dotnet test                                  # from the repo root
cd src/frontend && npm ci && npm test        # runs once and exits
```

## Layout

| Path | Contents |
|---|---|
| `src/backend/` | .NET backend projects |
| `src/backend/tests/` | Backend tests (xUnit v3, Shouldly) |
| `src/frontend/` | Angular 22 workspace (Vitest, specs next to the code) |
| `docs/` | Process, ADRs, session logs |
