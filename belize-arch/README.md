# Belize Hub Architecture

Two diagrams of the Christ Medical field architecture for Belize mission trips.
Field Clinical design system (warm paper, campus brick, college ink, bowman sage).

## Files

- **`index.html`** — click-through demo. Pressable buttons; each Advance walks the
  data one step from the home system to the field and back. For presenting live.
  Space bar / arrow keys to drive it.
- **`architecture.html`** — the full architecture on one static slide. Everything
  visible at once, no narration needed. This is the reference diagram.
- **`styles.css`** — Field Clinical tokens + the pressable-button styling.
- **`demo.js`** — the `<press-button>` web component and the reveal sequence.

## The model (one paragraph)

Belize routes all internet through official government channels, so there is no
legitimate live cloud link at the clinic. A local **hub** (API + Postgres on a box
at the site) becomes the master record for the duration of a trip. Field tablets
connect to it over a **local travel-router network** with no internet, which gives
live cross-device visibility (the doctor sees the nurse's vitals immediately). Once
a day, at the hotel, the hub does a **one-way bulk push** to the cloud over the legal
connection. On **check-in** at trip end, clinical records reconcile home (checked-out
data wins) and shared dictionary tables (formulary, diagnoses) **merge additively** so
no trip clobbers another's additions.

Architecturally this is the **cruise-ship model**: a local hub behind a controlled
boundary, serving clients over a LAN, reconciling to shore on a schedule. Same shape,
arrived at from a regulatory constraint instead of a physical one.

## Click order (edit in `demo.js` → `STEPS`)

1. ChristMed — home/cloud master
2. Reference tables prepared (additive dictionary merge noted)
3. Cornerstone Church prepped
4. First Baptist prepped (multi-church, multi-trip)
5. Checkout — hub becomes trip master
6. Hub travels to the clinic site
7. Tablets connect over local network (the cross-device-visibility justification)
8. Nightly one-way push to cloud from the hotel
9. Check-in — Belize hold releases, records reconcile, dictionaries merge

To change wording or order, edit the `STEPS` array in `demo.js`. To restyle, the
tokens are all at the top of `styles.css`.

## Run

```sh
make run     # click-through demo at http://127.0.0.1:8080/index.html
make arch    # static reference diagram
make stop    # free the port if a server got left behind
make         # list targets
```

The Makefile is a convenience only — it just runs python3's stdlib static server and
opens a tab. Still no build, no dependencies: `open index.html` works exactly the same.
Overrides: `make run PORT=9000`, or `make serve HOST=0.0.0.0` to pull it up on a tablet
over the same wifi. Fonts load from Google Fonts and degrade to system fonts offline.
