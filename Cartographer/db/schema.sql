-- Cartographer schema (PostgreSQL)
create table if not exists cells (
  grid_id         text        not null,
  cell_x          bigint      not null,
  cell_y          bigint      not null,
  class           text        not null,
  secondary_class text        null,
  confidence      real        not null,
  sampled_at      timestamptz not null,
  expires_at      timestamptz not null,
  primary key (grid_id, cell_x, cell_y)
);

create index if not exists cells_expiry on cells (grid_id, expires_at);

create table if not exists render_jobs (
  id              bigserial   primary key,
  batch_key       text        not null unique,
  grid_id         text        not null,
  origin_x        bigint      not null,
  origin_y        bigint      not null,
  width           int         not null,
  height          int         not null,
  status          text        not null default 'pending',
  attempts        int         not null default 0,
  last_error      text        null,
  created_at      timestamptz not null default now(),
  updated_at      timestamptz not null default now(),
  locked_at       timestamptz null,
  locked_by       text        null
);

create index if not exists render_jobs_poll
  on render_jobs (status, created_at)
  where status in ('pending', 'failed');
