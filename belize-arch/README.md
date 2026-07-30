# Belize Hub Architecture

Diagrams of the Christ Medical field architecture for Belize mission trips.
Field Clinical design system (warm paper, campus brick, college ink, bowman sage).

## Files

| File | What it is |
|------|------------|
| `index.html` | Hub click-through demo — pressable keys, Advance walks home → field → home |
| `architecture.html` | Hub reference — full static slide |
| `data.html` | Data scope — tenant / clinic / shared containment (static) |
| `data-demo.html` | Data walkthrough — ownership tree, Belize checkout, nightly, check-in |
| `styles.css` | Field Clinical tokens + demo chrome |
| `demo.js` / `data-demo.js` | Sequences (`STEPS`) + `<press-button>` |

## The hub model (one paragraph)

Belize routes all internet through official government channels, so there is no
legitimate live cloud link at the clinic. A local **hub** (API + Postgres on a box
at the site) becomes the master record for the duration of a trip. Field tablets
connect to it over a **local travel-router network** with no internet, which gives
live cross-device visibility (the doctor sees the nurse's vitals immediately). Once
a day, at the hotel, the hub does a **one-way bulk push** to the cloud over the legal
connection. On **check-in** at trip end, clinical records reconcile home (checked-out
data wins) and shared dictionary tables (formulary, diagnoses) **merge additively** so
no trip clobbers another's additions.

## The data model (one paragraph)

One ChristMed database holds many **tenants** (churches). A tenant runs many
**clinics** (mission trips — e.g. Belize 2026, Honduras 2026). **Patients and
encounters stay in one clinic.** Formulary, mission workers, and treatments are
**shared across clinics inside the same church**. A Belize checkout takes the Belize
clinic slice plus a snapshot of that shared band; other clinics and other tenants
stay home.

## Click order — hub (`demo.js` → `STEPS`)

1. ChristMed — home/cloud master
2. Reference tables prepared (additive dictionary merge noted)
3. Cornerstone Church prepped
4. First Baptist prepped (multi-church, multi-trip)
5. Checkout — hub becomes trip master
6. Hub travels to the clinic site
7. Tablets connect over local network
8. Nightly one-way push to cloud from the hotel
9. Check-in — Belize hold releases, records reconcile, dictionaries merge

## Click order — data (`data-demo.js` → `STEPS`)

1. One database
2. Tenants are churches
3. Clinics are mission trips
4. Shared @ church (formulary / workers / treatments)
5. Patients stay in one clinic
6. Checkout: Belize slice + shared snapshot (Honduras held)
7. Nightly push
8. Check-in merge

## Run

```sh
make run        # hub walkthrough
make arch       # hub reference
make data       # data-scope static
make data-demo  # data ownership walkthrough
make stop       # free the port if a server got left behind
make            # list targets
```

Still no build, no dependencies: `open index.html` works the same.
Overrides: `make run PORT=9000`, or `make serve HOST=0.0.0.0`.
Fonts load from Google Fonts and degrade to system fonts offline.
