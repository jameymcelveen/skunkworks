# CLAUDE.md — belize-arch

Agent brief for a Claude Code session finishing or maintaining this folder.
Lives at `skunkworks/belize-arch/`. Read this before touching anything.

## What this is

Two diagrams of the Christ Medical **Belize hub field architecture**, for a mission
clinic EMR. One is an animated click-through for presenting live; the other is a
static one-slide reference for the repo.

- `index.html` — click-through demo. Pressable `<press-button>` keys; each Advance
  presses the outgoing key and reveals the next node + connector. Space / arrow keys
  drive it. This is the **story** diagram, for presenting.
- `architecture.html` — the full architecture on one static slide, everything visible,
  no narration needed. This is the **reference** diagram.
- `styles.css` — Field Clinical design tokens + pressable-button styling.
- `demo.js` — the `<press-button>` web component and the `STEPS` reveal sequence.
- `README.md` — human-facing description and click order.

No build, no dependencies, no server. Open the HTML directly. Fonts load from Google
Fonts and degrade to system fonts offline.

## The architecture it depicts (ground truth — do not drift from this)

Belize routes all internet through official government channels, so there is **no
legitimate live cloud link at the clinic**. The design:

- A local **hub** (API + Postgres on a box on-site) becomes the **master record for
  the duration of a trip** (checkout). Checked-out clinical data wins on return.
- Field **tablets connect to the hub over a local travel-router network**, no internet.
  This gives live cross-device visibility (registration → nurse → doctor → pharmacy on
  different devices). **That cross-device need is the real reason for the hub**, not
  just the legal-internet problem.
- **Nightly**, at the hotel, the hub does a **one-way bulk push** to the cloud over the
  legal link. One writer, one direction, once a day, at rest. NOT bidirectional
  per-device sync. Roughly a fifth of that complexity.
- On **check-in** (trip end), the Belize hold releases: clinical records reconcile home,
  shared dictionary tables (formulary, diagnoses) **merge additively by row** so no trip
  clobbers another's or the home office's additions.
- Architecturally this is the **cruise-ship model**: local hub behind a controlled
  boundary, clients over a LAN, scheduled reconcile. Same shape, regulatory constraint
  instead of physical. (Keep this parallel in the diagrams as a one-line bridge only;
  the full comparison is delivered verbally, not drawn.)

Open design question (from the field, not yet answered): can the formulary be **frozen
before departure**? If yes, the hub can run a read-only reference snapshot and the
additive-merge complexity drops. If field teams add drugs/diagnoses mid-trip, the
additive merge is required. Do not hard-code an assumption either way.

## Design system — Field Clinical (already established, do not invent new colors)

Tokens live at the top of `styles.css`. Match exactly:

- `--paper #FAF7F0` warm paper (page)
- `--paper-2 #F1ECE0` recessed panel
- `--brick #B94700` campus brick — the accent, used with restraint
- `--ink #2E2A26` college avenue ink (text)
- `--sage #546223` bowman field sage — local-network / field elements
- `--hair #D9D3CA` hairline · `--ghost #C9C2B6` not-yet-reached
- Display face: Bricolage Grotesque · Body: Public Sans · Data/IDs: mono

Convention: brick = cloud/checkout path, sage = local field network, dashed brick =
nightly reconcile, ghost/dimmed = not yet reached in the sequence.

## How to change things

- **Click order or captions:** edit the `STEPS` array in `demo.js`. Each entry is
  `{ caption, on: [ids to activate], press: 'id to depress' }`. Order of the array is
  the click order. This is the single source of truth for the sequence.
- **A new node:** add a `<press-button id="..." label="..." sub="...">` in `index.html`,
  give it `state="ghost"` implicitly (default), reference its id in the relevant `STEPS`
  entry. For the static diagram, add the matching `<rect>`/`<text>` in `architecture.html`.
- **Restyle:** change tokens at the top of `styles.css` only. Do not scatter hex values.
- **Reduced motion** is already respected (`prefers-reduced-motion`). Keep it that way.

## Quality gates before commit

- Both HTML files open standalone with no console errors.
- Demo: Advance disables at the last step; Back re-enables and steps down correctly;
  Reset returns to the ghost state. Space / → advance, ← steps back.
- Reduced-motion: animations off, all content still reachable.
- No new dependencies, no build step introduced, no external calls beyond Google Fonts.
- Keyboard focus visible; degrades to system fonts offline.

## Do not

- Do not add the Carnival diagram as a drawn second panel. The parallel stays a one-line
  verbal/caption bridge. Two systems drawn at once gets busy.
- Do not introduce a framework, bundler, or npm install. This is intentionally
  zero-build static HTML/CSS/JS.
- Do not change the architecture facts above to simplify the diagram. If the diagram is
  hard to draw, fix the drawing, not the truth.
