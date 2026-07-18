using System.Text;
using System.Text.Json;
using Jobscan.Config;
using Jobscan.Filtering;
using Jobscan.Model;
using Jobscan.Scoring;

namespace Jobscan.Reporting;

public sealed record ScanStats
{
    public int Sources { get; init; }
    public int Seen { get; init; }
    public int New { get; init; }
    public int Rejected { get; init; }
}

public sealed record Hit(Posting Posting, Verdict Verdict, ScoreResult Score,
                         IReadOnlyList<string>? Flags = null)
{
    public IReadOnlyList<string> Flags { get; init; } = Flags ?? [];
}

/// <summary>
/// backlog.md is the coffee document: ranked, skimmable, shows its work.
/// rejected.md is the audit trail: every reject with its reason.
///
/// House style (brief.md section 6): no em dashes in generated output. Ever.
/// </summary>
public static class Reports
{
    private static string Clip(string? s, int n)
    {
        s = string.Join(' ', (s ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return s.Length <= n ? s : s[..(n - 1)] + "\u2026";
    }

    private static string Comp(Verdict v) => v.Comp switch
    {
        { Kind: CompKind.Salary, Low: { } lo, High: { } hi } => $"${lo / 1000}K-${hi / 1000}K",
        { Kind: CompKind.Hourly, Low: { } lo, High: { } hi } => $"${lo}-${hi}/hr",
        _ => "not stated",
    };

    public static string Backlog(IReadOnlyList<Hit> rows, Profile prof, ScanStats stats)
    {
        var now = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'");
        var pri = prof.Thresholds.PriorityMinScore;
        var sb = new StringBuilder();

        sb.AppendLine("# Backlog").AppendLine();
        sb.AppendLine($"Generated {now}. Sources scanned: {stats.Sources}. " +
                      $"Postings seen: {stats.Seen}. Filtered out: {stats.Rejected}. " +
                      $"New since last run: {stats.New}.").AppendLine();
        sb.AppendLine("Review, then say go on the ones worth a kit. Kits are never automatic.")
          .AppendLine();

        if (rows.Count == 0)
        {
            sb.AppendLine("Nothing cleared the filter this run. " +
                          "See `rejected.md` for what was cut and why.");
            return sb.ToString();
        }

        sb.AppendLine("| # | Score | Company | Role | Comp | Where | Age |");
        sb.AppendLine("|---|-------|---------|------|------|-------|-----|");
        for (var i = 0; i < rows.Count; i++)
        {
            var (p, v, s) = (rows[i].Posting, rows[i].Verdict, rows[i].Score);
            var flag = s.Total >= pri ? " **P**" : "";
            var age = s.Why.FirstOrDefault(w => w.StartsWith("freshness"))?.Split(": ").Last() ?? "?";
            sb.AppendLine($"| {i + 1}{flag} | {s.Total} | {Clip(p.Company, 22)} | " +
                          $"[{Clip(p.Title, 38)}]({p.Url}) | {Comp(v)} | " +
                          $"{(Clip(p.Location, 18) is { Length: > 0 } l ? l : "?")} | {age} |");
        }
        sb.AppendLine();
        sb.AppendLine($"**P** = priority tier (score >= {pri}).").AppendLine();
        sb.AppendLine("---").AppendLine();

        for (var i = 0; i < rows.Count; i++)
        {
            var (p, v, s) = (rows[i].Posting, rows[i].Verdict, rows[i].Score);
            sb.AppendLine($"## {i + 1}. {p.Company} : {p.Title}  `{s.Total}`").AppendLine();
            sb.AppendLine(p.Url).AppendLine();
            sb.AppendLine($"- **Comp:** {Comp(v)}");
            sb.AppendLine($"- **Location:** {(p.Location is { Length: > 0 } ? p.Location : "not stated")}");
            sb.AppendLine($"- **Day shape:** {v.DayShape}");
            sb.AppendLine($"- **Source:** {p.Source}");
            sb.AppendLine($"- **id:** `{p.Id}`");
            foreach (var f in rows[i].Flags) sb.AppendLine($"- **FLAG:** {f}");
            sb.AppendLine();
            sb.AppendLine("**Why this scored:**").AppendLine();
            foreach (var w in s.Why) sb.AppendLine($"- {w}");
            sb.AppendLine();
            if (Clip(p.Body, 400) is { Length: > 0 } snip) sb.AppendLine("> " + snip).AppendLine();
            sb.AppendLine($"**Go:** `make kit ID={p.Id}`").AppendLine();
            sb.AppendLine("---").AppendLine();
        }

        return sb.ToString();
    }

    public static string BacklogJson(string profile, IReadOnlyList<Hit> rows,
                                     Profile prof, ScanStats stats)
    {
        var doc = new
        {
            profile,
            generated = DateTimeOffset.UtcNow,
            priority_min = prof.Thresholds.PriorityMinScore,
            stats = new { stats.Sources, stats.Seen, stats.New, stats.Rejected },
            hits = rows.Select(h => new
            {
                id = h.Posting.Id,
                score = h.Score.Total,
                priority = h.Score.Total >= prof.Thresholds.PriorityMinScore,
                company = h.Posting.Company,
                title = h.Posting.Title,
                url = h.Posting.Url,
                location = h.Posting.Location,
                comp = Comp(h.Verdict),
                day_shape = h.Verdict.DayShape,
                source = h.Posting.Source,
                domain = h.Posting.Domain,
                why = h.Score.Why,
                flags = h.Flags,
            }),
        };
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }

    public static string Rejected(IReadOnlyList<(Posting P, Verdict V)> rejects)
    {
        var now = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'");
        var sb = new StringBuilder();

        sb.AppendLine($"# Rejected  ({rejects.Count})").AppendLine();
        sb.AppendLine($"Generated {now}. Audit trail for the three-line filter.");
        sb.AppendLine("If something good is in here, the filter is wrong. " +
                      "Fix `profile.jsonc`, not the posting.").AppendLine();

        var buckets = rejects
            .GroupBy(r => r.V.Reason.Split(':')[0].Trim())
            .OrderByDescending(g => g.Count());

        foreach (var g in buckets)
        {
            sb.AppendLine($"## {g.Key}  ({g.Count()})").AppendLine();
            foreach (var (p, v) in g.Take(40))
                sb.AppendLine($"- **{Clip(p.Company, 24)}** : {Clip(p.Title, 44)} <br> " +
                              $"`{v.Reason}` <br> {p.Url}");
            if (g.Count() > 40) sb.AppendLine($"- _...and {g.Count() - 40} more_");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
