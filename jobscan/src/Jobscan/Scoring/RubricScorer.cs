using System.Text.RegularExpressions;
using Jobscan.Config;
using Jobscan.Filtering;
using Jobscan.Model;

namespace Jobscan.Scoring;

public sealed record ScoreResult
{
    public int Total { get; init; }
    public Dictionary<string, int> Parts { get; init; } = [];
    public List<string> Why { get; init; } = [];
}

/// <summary>
/// The rubric (brief.md section 13):
///   stack depth 35 / domain 15 / level 15 / comp 10 / freshness 10 / remote 10 / ai 5
///
/// Only survivors of the three-line filter reach this. Every component carries a
/// one-line rationale so backlog.md shows its work instead of an opaque number.
/// </summary>
public static partial class RubricScorer
{
    [GeneratedRegex(@"\b(ehr|emr|patient|clinical|hipaa|fhir|hl7|provider|payer)\b")]
    private static partial Regex Healthcare();

    [GeneratedRegex(@"\b(ministry|church|nonprofit|mission|faith|donor|giving)\b")]
    private static partial Regex Mission();

    [GeneratedRegex(@"\b(fully remote|100% remote|remote-first|distributed team)\b")]
    private static partial Regex RemoteFirst();

    [GeneratedRegex(@"hybrid|in.office|on.?site")]
    private static partial Regex Onsiteish();

    /// <summary>Raw stack points at which the match is considered saturated.</summary>
    private const double StackSaturation = 40.0;

    public static ScoreResult Score(Posting p, Verdict v, Profile prof)
    {
        var r = prof.Rubric;
        var hay = p.Haystack;
        var req = Requirements.Block(p.Body).ToLowerInvariant();
        var parts = new Dictionary<string, int>();
        var why = new List<string>();

        // -- stack depth (35) ------------------------------------------------
        // Hits inside the requirements block count double: that's the gate.
        double raw = 0;
        var hits = new HashSet<string>();

        foreach (var (kw, w) in prof.Stack.Primary)
        {
            if (req.Contains(kw)) { raw += w * 2; hits.Add(kw); }
            else if (hay.Contains(kw)) { raw += w; hits.Add(kw); }
        }
        foreach (var (kw, w) in prof.Stack.Adjacent)
        {
            if (req.Contains(kw)) { raw += w; hits.Add(kw); }
            else if (hay.Contains(kw)) { raw += w * 0.5; hits.Add(kw); }
        }

        var pts = (int)Math.Round(r.StackDepth * Math.Min(raw / StackSaturation, 1.0));
        parts["stack"] = pts;
        why.Add($"stack {pts}/{r.StackDepth}: " +
                (hits.Count > 0 ? string.Join(", ", hits.Order().Take(8)) : "none"));

        // -- domain (15) -----------------------------------------------------
        var d = prof.DomainAffinity.ContainsKey(p.Domain) ? p.Domain : "other";
        // Let the text upgrade the tag: a "saas" company hiring for an EHR is healthcare.
        if (Healthcare().IsMatch(hay)) d = "healthcare";
        else if (Mission().IsMatch(hay) && prof.DomainAffinity.ContainsKey("mission")) d = "mission";

        var aff = prof.DomainAffinity.TryGetValue(d, out var a) ? a : 0.2;
        pts = (int)Math.Round(r.Domain * aff);
        parts["domain"] = pts;
        why.Add($"domain {pts}/{r.Domain}: {d}");

        // -- level (15) ------------------------------------------------------
        var t = p.Title.ToLowerInvariant();
        string lbl;
        if (t.Contains("principal") || t.Contains("staff") ||
            t.Contains("distinguished") || t.Contains("architect"))
            (pts, lbl) = (r.Level, "principal/staff tier");
        else if (t.Contains("lead") || t.Contains("senior") || t.Contains("sr."))
            (pts, lbl) = ((int)Math.Round(r.Level * 0.8), "senior/lead tier");
        else
            (pts, lbl) = ((int)Math.Round(r.Level * 0.4), "level unclear from title");
        parts["level"] = pts;
        why.Add($"level {pts}/{r.Level}: {lbl}");

        // -- comp (10) -------------------------------------------------------
        var floor = prof.Comp.SalaryFloor;
        if (v.Comp.Kind == CompKind.Salary && v.Comp.High is { } chi)
        {
            if (chi >= floor * 1.25)
                (pts, lbl) = (r.Comp, $"${v.Comp.Low:N0}-${chi:N0}, well over floor");
            else if (chi >= floor)
                (pts, lbl) = ((int)Math.Round(r.Comp * 0.7), $"${v.Comp.Low:N0}-${chi:N0}, clears floor");
            else
                (pts, lbl) = (0, "under floor");
        }
        else if (v.Comp.Kind == CompKind.Hourly && v.Comp.High is { } hhi)
        {
            pts = hhi >= prof.Comp.HourlyFloor * 1.3 ? r.Comp : (int)Math.Round(r.Comp * 0.7);
            lbl = $"${v.Comp.Low}-${hhi}/hr";
        }
        else
        {
            (pts, lbl) = ((int)Math.Round(r.Comp * 0.4), "not stated, ask");
        }
        parts["comp"] = pts;
        why.Add($"comp {pts}/{r.Comp}: {lbl}");

        // -- freshness (10) --------------------------------------------------
        if (p.PostedAt is { } posted)
        {
            var age = (int)(DateTimeOffset.UtcNow - posted).TotalDays;
            (pts, lbl) = age switch
            {
                <= 3 => (r.Freshness, $"{age}d old"),
                <= 10 => ((int)Math.Round(r.Freshness * 0.7), $"{age}d old"),
                <= 30 => ((int)Math.Round(r.Freshness * 0.4), $"{age}d old"),
                _ => (0, $"{age}d old, likely stale"),
            };
        }
        else
        {
            (pts, lbl) = ((int)Math.Round(r.Freshness * 0.5), "no date");
        }
        parts["freshness"] = pts;
        why.Add($"freshness {pts}/{r.Freshness}: {lbl}");

        // -- remote culture (10) ---------------------------------------------
        if (RemoteFirst().IsMatch(hay))
            (pts, lbl) = (r.RemoteCulture, "remote-first language");
        else if (hay.Contains("remote") && !Onsiteish().IsMatch(hay))
            (pts, lbl) = ((int)Math.Round(r.RemoteCulture * 0.8), "remote");
        else if (hay.Contains("hybrid"))
            (pts, lbl) = ((int)Math.Round(r.RemoteCulture * 0.3), "hybrid");
        else if (prof.Location.OnsiteAllow.Any(hay.Contains))
            (pts, lbl) = ((int)Math.Round(r.RemoteCulture * 0.5), "onsite but in range");
        else
            (pts, lbl) = ((int)Math.Round(r.RemoteCulture * 0.2), "unclear");
        parts["remote"] = pts;
        why.Add($"remote {pts}/{r.RemoteCulture}: {lbl}");

        // -- ai friendly (5) -------------------------------------------------
        if (prof.AiFriendlySignals.Negative.Any(hay.Contains))
            (pts, lbl) = (0, "AI use restricted");
        else if (prof.AiFriendlySignals.Positive.Any(hay.Contains))
            (pts, lbl) = (r.AiFriendly, "AI-positive language");
        else
            (pts, lbl) = ((int)Math.Round(r.AiFriendly * 0.4), "neutral");
        parts["ai"] = pts;
        why.Add($"ai {pts}/{r.AiFriendly}: {lbl}");

        return new ScoreResult { Total = parts.Values.Sum(), Parts = parts, Why = why };
    }
}
