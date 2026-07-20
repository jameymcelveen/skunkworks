# Scanner session deltas, 2026-07-18 (Bill)

For Homer to fold into Applications_In_Flight.md, and for Garfield's queue.

## Shipped this session
- Monorepo: repo restructured as skunkworks/, scanner lives in jobscan/.
- Multi-profile layout: profiles/<name>/ holds config + results. CLI --profile,
  --all-profiles. Boys onboard by copying profiles/jamey and editing.
- Flags system: advisory annotations on hits, never score-changing. Live rules:
  990 check on mission-domain, agentic-primary on requirements-block AI language,
  relocation-premium text markers (interim until geo layer).
- backlog.json alongside backlog.md. Web dashboard (jobscan/web) with profile
  selector. `--serve` (in-box HttpListener, verified by curl incl. traversal guard).
  `--daemon` = serve + rescan sweep every SCAN_INTERVAL_HOURS (default 6).
- Dockerfile (single container) + railway.toml at repo root. NOT container-tested
  here (no docker daemon in sandbox): Garfield validates first deploy.
- Paste header gains optional 4th field: Company | Title | Url | Domain.
- Actions workflow updated for monorepo paths.
- Tests: 18/18 green after restructure.

## Backlog hits scored this session (live postings, profile jamey)
- 90 American Bible Society, Staff Engineer. FLAGS: 990, agentic-primary.
  Claude Code is literally their named tooling. Comp unposted: ask early.
- 87 Samaritan Ministries, Senior Software Engineer, $99-154.4K. FLAG: 990.
  CTJ listing page says CLOSED but company/tag pages show active 4d ago:
  VERIFY at samaritanministries.org before any effort. Posting notes starting
  pay usually lower half of band: floor conversation required.
- 76 1Password, Senior .Net/C# Developer SaaS Manager, $153-214K. FLAG:
  agentic-primary. Already queued behind Called per board.
Rejected correctly: Ligonier ($120K top, comp), FOCL ($65K, comp),
Ascension (Salesforce+manager), Virtuous (Terraform gate).

## Still deferred (Garfield / next Bill session)
- Geo layer: 4 anchors (Florence 45/60, Daytona 90, Toccoa 90 + north-of-Atlanta
  min-lat, Long Beach 90), +$25K premium as effective $160K floor. Interim text
  markers are in ComputeFlags.
- Workday adapter (Philips, Wolters Kluwer) per 2026-07-18 handoff section 2.1.
- Live board token verification (greenhouse/lever tokens still VERIFY).
- O'Reilly wrap-tolerant gate script into scripts/verify-kit.sh (spec in handoff 2.3).
- SQLite: deferred until a real query needs it.

## Session addendum, 2026-07-18 evening (Bill)
- Profiles seth + slater live (configs, watchlists, NOTES.md hunting maps).
- Engine: remote_ok=false enforcement (Seth: remote banned, scam surface),
  scam-signal hard gate from brief section 11, ordered ABOVE location/level so
  the audit trail names the real reason. Tests now 20/20.
- Deployed: https://jobscan.up.railway.app (single container, daemon mode).
- Garfield queue additions: USAJOBS API adapter (free key) for Slater,
  Adzuna keys for both boys, Workday adapter now unlocks Duke/Dominion/
  Google-Moncks-Corner/SRS for Slater in addition to Philips/Wolters Kluwer.
