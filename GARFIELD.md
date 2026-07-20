# Garfield: kickoff and queue

You maintain what Bill launched. State as of 2026-07-18: scanner deployed at
https://jobscan.up.railway.app (Railway, single container, daemon mode),
4 profiles (jamey, seth, slater, connie), tests 20/20.

## Queue, in priority order

1. **Verify board tokens** (quick win, ~30 min). Every token marked VERIFY in
   the four profiles' companies.jsonc files is a guess. Run `make scan` per
   profile, fix or deactivate every [404]. Definition of done: a scan with zero
   404s per profile.
2. **Workday adapter** (the big one). Spec: HANDOFF_BACK.md section 2.1. Tenant
   list endpoint returns titles/locations without descriptions: prefilter on
   title before fetching detail pages, cache descriptions in seen-state, one
   sweep per day per tenant max. Unlocks: Philips + Wolters Kluwer (jamey),
   Duke/Dominion/Google-Moncks-Corner/SRS (slater), Humana/CVS/Optum/Centene
   (connie). Design it generic: tenant + site in companies.jsonc.
3. **USAJOBS adapter** (slater). Documented public API, free key via email
   signup, key in env. NIWC Atlantic, shipyards, DOE.
4. **Adzuna activation** (seth + slater). Free tier keys in env; retail and
   trades long tail.
5. **Geo layer** (jamey). Four anchors with radii + the $25K relocation
   premium as a $160K effective floor + north-of-Atlanta min-lat for Toccoa.
   Interim text-marker flags exist in Program.ComputeFlags: replace them.
6. **verify-kit.sh** from HANDOFF_BACK section 2.3 (wrap-tolerant Wiley gate).
7. Docker/Railway health: confirm image builds stay green after your changes;
   the deploy is auto on push to main.

## Definition of done, globally
make test green, no new packages, HANDOFF_BACK.md updated with what changed,
and nothing in any profile's results that its NOTES.md would be ashamed of.
