# ADR 0001: Check-in history, tenant isolation, MapProvider

Status: Accepted (milestone 1 decisions; MapProvider section reserved for milestone 5)

## CheckInHistory write path

**Decision:** Insert into `check_in_history` only on checkout, in the same transaction that deletes the `active_check_ins` row. The history row carries both `CheckedInAt` (copied from the active row) and `CheckedOutAt` (now). History rows are never updated.

**Why:** True append-only log. No nullable `CheckedOutAt`, no update race on close. Live occupancy stays exclusively in `ActiveCheckIn`; history is for audit and activity views.

**Rejected alternative:** Insert history on check-in with `CheckedOutAt = null`, then update on checkout. That is a two-write path and weakens append-only semantics.

## Tenant isolation (RLS + query filters)

**Decision:** Dual enforcement on every club-scoped table.

1. **EF Core `HasQueryFilter`** on `ClubId` (or via `Stand.ClubId` for check-in tables). Filters use `ITenantContext.ClubIds` hydrated per request.
2. **Postgres Row Level Security** with `FORCE ROW LEVEL SECURITY`. Session GUC `app.current_profile_id` is set by `TenantConnectionInterceptor` on connection open. Policies key off `app_user_club_ids()`, a `SECURITY DEFINER` helper that reads `club_members` without RLS recursion.

**Why:** Query filters catch app bugs early. RLS is the database backstop if a filter is forgotten or bypassed (`IgnoreQueryFilters`). Both, always.

## MapProvider contract

**Decision:** All map code lives under `src/map/` (Next.js frontend). Screens import only `ClubMap`. Providers implement `MapProviderProps` from `contract.ts`. Swap procedure: add `src/map/providers/{name}/`, set `NEXT_PUBLIC_MAP_PROVIDER`, keep the provider contract test suite green. No mapping-library imports outside `src/map/providers/**` (ESLint enforced in milestone 5).

v1 provider: `maplibre` with self-hosted Protomaps `.pmtiles`. Future `tiles` provider is documented only; do not scaffold it.
