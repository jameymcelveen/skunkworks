# Cartographer

Pipeline that translates real-world geography into a grid of game-terrain cells.

A headless browser renders a purpose-built MapLibre sentinel style over a
self-hosted Protomaps `.pmtiles` extract. A worker samples pixels per cell,
classifies each cell into a closed terrain enum, and upserts into Postgres.
Clients (DeerStand `tiles` MapProvider, later) read the class grid and choose
sprites. Cartographer produces data, not pictures.

## Attribution (ODbL)

Map data © [OpenStreetMap](https://www.openstreetmap.org/copyright) contributors,
available under the Open Database License (ODbL). Cartographer's cell database is
a derived database: keep this attribution on any public surface that displays
derived terrain.

Protomaps packaging of OSM data is used for local/self-hosted rendering only.

## Legal constraint

Do **not** point the renderer at Mapbox, Google, Bing, or any commercial tile
service. Their terms prohibit deriving data from rendered tiles. The only
allowed tile origin is our self-hosted Protomaps extract. The worker asserts
this at startup.

## Projects

| Project | Role |
|---|---|
| `Cartographer.Core` | EPSG:3857 grid math, terrain enum, classifier, storage |
| `Cartographer.Api` | Discovery API, TTL expiry sweep |
| `Cartographer.Worker` | Playwright render + ImageSharp classify + upsert |

## Quickstart (local)

```bash
docker compose up -d postgres
# Place a regional extract at data/pmtiles/region.pmtiles
# Serve pmtiles (example): python3 -m http.server 8080
dotnet run --project src/Cartographer.Api
dotnet run --project src/Cartographer.Worker
```

Discovery:

```
GET /grids/{gridId}/cells?bbox=minLng,minLat,maxLng,maxLat
```

`gridId` is `SHA-256(cellSize|datasetVersion)` for the configured grid
(default cell size 10 m, dataset `v1`). See `/health` for the active id.

Debug class viewer (colored divs, not the sprite renderer):

```
open debug/class-grid.html
```

## Tests

```bash
dotnet test Cartographer.sln
```

## Deploy (Railway)

See `railway.toml` and `Dockerfile`. Provision Postgres, set
`ConnectionStrings__Cartographer`, `Render__PmtilesUrl` to the private pmtiles
host, and deploy API + Worker services.

## Docs

- [Tile selection contract](docs/TILE_SELECTION.md) (for the future DeerStand renderer)
- [ADR: sentinel classification](docs/adr/0001-sentinel-style-classification.md)
- [Sentinel visual check](styles/VISUAL_CHECK.md)
