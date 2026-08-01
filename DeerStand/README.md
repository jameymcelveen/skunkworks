# DeerStand

Privacy-first multi-tenant SaaS for hunting clubs: stand placements on a private map, real-time check-ins (one hunter per stand), and activity logs.

Lab path: `~/Developer/skunkworks/DeerStand`

## Stack

- ASP.NET Core 9 API, EF Core, PostgreSQL 16
- Zitadel OIDC (milestone 2+)
- SignalR club groups (milestone 4+)
- Next.js 15 frontend (milestone 5+)

## Local setup (API + Postgres)

1. Install .NET 9 SDK and Docker.
2. `docker compose up -d` (Postgres on host port **5433**).
3. `dotnet restore`
4. `dotnet ef database update --project src/DeerStand.Infrastructure --startup-project src/DeerStand.Api` (or just run the API; it migrates on startup).
5. `dotnet run --project src/DeerStand.Api`
6. Health check: `curl http://localhost:5187/healthz`
7. Tests: `dotnet test`
8. Connection string and secrets: env only. Default local string is in `appsettings.json` (dev password, not for production).
9. Read `docs/adr/0001-checkin-rls-map.md` for check-in history and RLS decisions.

Frontend, Zitadel, SignalR, and map steps land in later milestones.

## Layout

```
src/DeerStand.Core/            domain constants
src/DeerStand.Infrastructure/  EF entities, migrations, RLS, tenant interceptor
src/DeerStand.Api/             ASP.NET Core host
tests/                         xUnit + Shouldly
docs/adr/                      architecture decisions
docker-compose.yml             local Postgres only
```

## Milestone status

1. **Done:** solution scaffold, EF model, migrations, RLS, compose, constraint tests
2. Auth + club CRUD + join
3. Stand CRUD + check-in/checkout
4. SignalR hub
5. Map layer (MapLibre + pmtiles)
6. Frontend screens 1-5
7. Deploy (Railway + Vercel)
