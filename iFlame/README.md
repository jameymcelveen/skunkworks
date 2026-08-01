# iFlame

Pitch-black ambient focus workspace: a quiet fire, three looping stems (fire, rain, noise), and controls that fade when you go idle.

Static site. No build step. No runtime dependencies.

## Quick start

Open `index.html` in a browser, or serve the folder:

```bash
npx --yes serve .
```

Click **Light the fire**. Adjust Fire / Rain / Noise. Move the mouse (or touch / press a key) to reveal the tray; it fades after 4 seconds idle.

## Layout

| Path | Role |
|---|---|
| `index.html` | Markup |
| `styles.css` | Black stage, fire SVG, tray, idle fade, reduced motion |
| `app.js` | Web Audio mixer, idle tray, cookie persistence |
| `assets/` | Ogg + MP3 stems (each under 2 MB) |
| `CREDITS.md` | Stem licenses and loop notes |

## Audio

One `AudioContext`. Three media-element sources, each through its own `GainNode`, into a master gain then the destination. Persistence is `loadState` / `saveState` (cookie today; swap the pair for query-string or in-memory if needed).

## Deploy

Point Vercel (or any static host) at this directory. `vercel.json` sets SPA-free static headers.
