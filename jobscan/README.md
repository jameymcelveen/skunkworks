# jobscan

Reads public job boards, filters hard, ranks the survivors, writes `backlog.md`.
A human reads the backlog over coffee and decides what gets an apply kit.

Not an auto-applier. See `CLAUDE.md` for why that is a permanent decision.

.NET 8. Zero NuGet packages.

## Why there is no CAPTCHA problem

The job splits in two, and the walls are all in the second half.

**Discovery** has no walls. Greenhouse, Lever, and Ashby publish public JSON.
Remotive, RemoteOK, and Adzuna have free APIs. christiantechjobs.io has RSS.
These endpoints exist so machines can read them. This is where the volume is and
it automates cleanly, legitimately, forever.

**Application** has walls everywhere, and that is fine, because a human was
always going to click submit. Simplify autofills most of a Workday form, a human
reviews it and sends. Roughly 10 minutes per application. The friction is the
filter that thins a hundred click-throughs into a real pile.

Where a target only posts on a walled surface, `data/paste/` is the door: copy
the JD text into a `.txt` file and it scores identically to an API-sourced one.

## Zero dependencies

`nuget.config` clears all package sources. Everything used is in-box .NET 8:
`HttpClient`, `System.Text.Json` (JSONC config via `ReadCommentHandling.Skip`),
`System.Xml.Linq` for RSS, `Regex` with source generators.

No restore. No supply chain to audit. CI is setup-dotnet, build, test. A tool
that runs unattended on a schedule against the open internet is a tool worth
keeping small.

## Setup

```bash
make test        # 18 checks, should be green
make paste       # score the fixtures
make scan        # hit the live boards
```

## The filter

From `brief.md` section 3. Titles lie, requirement lists don't, so
`Filtering/Requirements.cs` extracts the requirements block and the filter reads
that instead of the marketing.

1. **Comp** in or above floor ($135K salary, $65/hr contract)
2. **Quals** load-bearing keyword overlap in the requirements block, not the title
3. **Day shape** building or running

Plus the gap map as hard walls: an IaC-gated or Power Platform-gated role is
rejected outright, because a keyword that clears an ATS and collapses in the
technical screen is worse than not applying.

Current fixture behavior:

| Fixture | Expected | Caught by |
|---|---|---|
| Flowers Foods "Cloud Engineer/Architect" | reject | comp floor, and independently the Power Platform gate |
| Tithe.ly "Senior Platform Engineer" | reject | Terraform/ArgoCD/Helm gate |
| Acme "Junior .NET Developer" | reject | level |
| OPENLANE "Sr. Software Engineer (C# .NET)" | pass, 79 | |
| Thorne "Principal Software Engineer" | pass, 93 | |

## Tuning

`profile.jsonc` is the knobs. When `rejected.md` holds something good, do not
special-case the posting: find the rule that misfired, fix it, run `make test`.
The filter is the product.

## Status

Filter, scorer, comp parser, and reports are tested and green. Scores match a
reference implementation exactly.

**Not yet verified:** the live board tokens in `companies.jsonc`. They are
guesses from company slugs and were never tested against the real endpoints. The
first `make scan` prints a `[404]` for each wrong one. Fix or deactivate.

**Not yet built:** the kit generator. Scoped in `CLAUDE.md`. It is a Claude Code
task, not a cron task.
