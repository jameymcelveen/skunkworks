# Sentinel style visual check

Manual gate for milestone 2. Point `render.html` at a self-hosted Protomaps
extract and confirm flat sentinel colors with correct layer order.

## Setup

1. Place a regional `.pmtiles` extract under `data/pmtiles/` (gitignored).
2. Serve the repo root (or at least `styles/` and `render/`) over HTTP.
3. Open a URL like:

```
/render/render.html?style=/styles/sentinel-style.json&pmtiles=http://localhost:8080/data/pmtiles/region.pmtiles&minLng=...&minLat=...&maxLng=...&maxLat=...
```

## Suggested checkpoints

| Scene | What to confirm |
|---|---|
| Lake / river | Solid `#0000FF` water above land fills |
| Forest block | Solid `#007700` trees |
| Interstate / primary | Magenta `#FF00FF` paved roads above fills |
| Track / path | Brown `#AA5500` dirt roads |
| Building cluster | Red `#FF0000` structures on top |
| Unmapped land | Background dirt `#885533` (not black, not unknown) |

No labels. No photographic basemap. No Mapbox/Google/Bing tiles.
