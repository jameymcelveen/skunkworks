# ADR 0001: Sentinel-style classification vs satellite averaging

## Status

Accepted (Cartographer v1)

## Context

We need a stable terrain class per ~10 m Web Mercator cell for a game grid.
Two approaches were considered:

1. Average colors from photographic/satellite or styled cartography tiles and
   threshold into classes.
2. Render a purpose-built MapLibre style where each OSM-derived land cover /
   highway / building class paints as one flat sentinel color, then majority-
   vote sample the screenshot.

Commercial tile providers (Mapbox, Google, Bing) prohibit deriving databases
from rendered tiles. Even with legal imagery, photographic averaging is
ambiguous (shadows, season, roof materials) and unstable across zoom and
vendor style changes.

## Decision

Use sentinel-style classification over self-hosted Protomaps (OSM-derived)
`.pmtiles` only.

- Each terrain class maps to one RGB sentinel (see `styles/sentinel-style.json`).
- Background is the dirt sentinel so unmapped land is dirt, not unknown.
- `unknown` is reserved for sampling failures outside snap tolerance.
- Roads and structures draw above area fills; water above land.
- Sampling: snap each pixel to the nearest sentinel (exact match preferred,
  small RGB tolerance for edge AA), majority vote wins; record runner-up and
  confidence.

Derived cell data is an ODbL-derived database. Attribution to OpenStreetMap
contributors must appear in the README and any public surface that displays
derived terrain.

## Consequences

- Classification is deterministic for a given dataset version and style.
- Changing cell size or dataset version creates a new `grid_id`; old grids are
  never mutated in place.
- We must host and update our own pmtiles extracts.
- Visual richness is irrelevant at render time; beauty belongs in the client
  sprite layer (`docs/TILE_SELECTION.md`).
