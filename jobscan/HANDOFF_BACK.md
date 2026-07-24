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

## 2026-07-22 board deltas (Bill)
- ABS: APPLIED. AI review opted in, $150K stated, Q7 led with calling.
- Samaritan Senior SWE: CLOSED at the source (careers portal ground truth). Kit archived,
  threads recyclable on repost. Their open AWS SWE role evaluated and SKIPPED: 2-5yr
  mid-level band ($87.9-135.3K, lower-half start), IaC-gated (Terraform/CloudFormation
  in requirements), run-shaped. Three-line filter fails on all three lines.
- PROCESS RULE (learned today): posting liveness is verified ONLY at the employer's own
  ATS/careers portal. Aggregators (CTJ company pages, LinkedIn, ZipRecruiter) cache
  stale listings for weeks. Garfield: encode a "verify at source" flag on any hit older
  than 14 days.
- Watch item: Samaritan senior seat may repost (was real 22d ago). Monthly portal glance.
- Samaritan history: Jamey applied in the past and was passed over (timing/stage TBD).
  Repost playbook updated: referral-first re-entry, not cold re-apply; new-candidate
  framing (SecureGive/Christ Medical/agentic practice postdate prior application).
  Do not relitigate; mine any prior feedback if it exists.
- Samaritan prior feedback (few months ago, email stage): "went with someone with more
  experience." Read: near-certain overqualification decline in polite phrasing, the
  exact failure mode brief section 5 addresses. Current kit pre-answers it (senior IC
  by choice + explicit floor). Repost play: referral-first, objection named early.
- Take Command: APPLIED 7/22, day-of-posting. Office-cadence question answered NO
  (truthfully; Florence SC vs Richardson/Austin). If auto-rejected on it, the posting
  was TX-hybrid in a remote costume and the filter worked. Kit archived.
- Day tally 7/22: ABS applied, Take Command applied, Samaritan closed/archived.
  Remaining queue: Thorne Rohan tap (due today), Hallow, 1Password, OPENLANE staleness check.
- Hallow: APPLIED 7/22 evening. No cover slot; theses delivered via form essays
  (why-Hallow, role-fit, AI-native project example featuring the three-seat jobscan
  build with humans-as-gate framing). Kit archived alongside.
- FINAL 7/22 TALLY: three applications sent (ABS 93, Take Command 69/top-band,
  Hallow 60/human-override), Samaritan closed and decoded, kits built: 4 total.
- Tomorrow's queue: 1Password kit + apply, OPENLANE staleness check, Rohan follow-through.
- REPO NOTE for Garfield: this checkout IS the latest state (packaged 7/22 evening
  with full git history). GitHub is behind it by one commit. Step zero of your first
  session: `git push origin main` from this checkout, which also triggers the Railway
  redeploy. The commit includes the quals-fallback filter fix (primary-stack
  whole-body fallback runs whenever req-block primary is empty, regardless of
  adjacent count; born from "rag" substring-matching inside "pragmatic").
