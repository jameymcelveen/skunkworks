using System.Text.RegularExpressions;
using Jobscan.Config;
using Jobscan.Model;

namespace Jobscan.Filtering;

public sealed record Verdict
{
    public required bool Passed { get; init; }
    public string Reason { get; init; } = "";
    public CompRange Comp { get; init; } = CompRange.None;
    public IReadOnlyList<string> StackHits { get; init; } = [];
    public string DayShape { get; init; } = "unclear";

    public static Verdict Reject(string reason, CompRange comp = default,
                                 IReadOnlyList<string>? hits = null) =>
        new() { Passed = false, Reason = reason, Comp = comp, StackHits = hits ?? [] };

    public static Verdict Pass(CompRange comp, IReadOnlyList<string> hits, string shape) =>
        new() { Passed = true, Comp = comp, StackHits = hits, DayShape = shape };
}

/// <summary>
/// The three-line filter (brief.md section 3). Runs BEFORE scoring.
///
///   1. Comp      - inside or above the floor?
///   2. Quals     - do load-bearing keywords overlap? Not the title. The requirements.
///   3. Day shape - building or running? Jamey builds.
///
/// Every reject carries a reason. Reasons land in rejected.md so the filter can be
/// audited instead of trusted. When something good shows up in there, the rule is
/// wrong: fix profile.jsonc, not the posting.
/// </summary>
public static partial class ThreeLineFilter
{
    [GeneratedRegex(@"remote\s*[-(,]?\s*(india|emea|uk|canada|europe|latam|philippines)",
        RegexOptions.IgnoreCase)]
    private static partial Regex RegionLocked();

    private static List<string> Hits(IEnumerable<string> needles, string hay) =>
        needles.Where(n => hay.Contains(n, StringComparison.Ordinal)).ToList();

    private static bool Any(IEnumerable<string> needles, string hay) =>
        needles.Any(n => hay.Contains(n, StringComparison.Ordinal));

    // -- pre-checks ---------------------------------------------------------

    private static string? CheckLocation(Posting p, Profile prof)
    {
        var loc = $"{p.Location} {p.Title}".ToLowerInvariant();
        var head = p.Body.Length > 900 ? p.Body[..900].ToLowerInvariant() : p.Body.ToLowerInvariant();

        if (loc.Contains("remote") || head.Contains("remote") || loc.Contains("anywhere"))
        {
            // "Remote" sometimes means "remote (India)". Coarse sanity check.
            if (RegionLocked().IsMatch(loc)) return "location: remote but region-locked outside US";
            // Profiles can exclude remote entirely (e.g. student profiles: the
            // remote part-time listing pool is heavily scam-contaminated).
            if (!prof.Location.RemoteOk && !Any(prof.Location.OnsiteAllow, loc))
                return "location: remote-only posting, profile excludes remote";
            return null;
        }

        if (Any(prof.Location.OnsiteAllow, loc)) return null;
        if (Any(prof.Location.OnsiteDeny, loc))
            return $"location: onsite in {p.Location}, outside 75mi and not remote";

        // An unknown location is a question, not a reject.
        if (string.IsNullOrWhiteSpace(p.Location)) return null;

        return $"location: onsite in {p.Location}, not remote and not in range";
    }

    private static string? CheckLevel(Posting p, Profile prof)
    {
        var t = $" {p.Title.ToLowerInvariant()} ";
        if (Any(prof.Level.Reject, t)) return $"level: '{p.Title}' reads junior";
        // An ambiguous title is not a reject. The quals check handles it.
        return null;
    }

    // -- the filter ---------------------------------------------------------

    public static Verdict Evaluate(Posting p, Profile prof)
    {
        var hay = p.Haystack;
        var req = Requirements.Block(p.Body).ToLowerInvariant();
        var compText = $"{p.CompRaw} {p.Body}";

        // scam gate FIRST: brief.md section 11 taxonomy, posting-side. It outranks
        // location and level so rejected.md names the real reason, not "wrong city".
        var scam = Hits(prof.ScamSignals, hay);
        if (scam.Count > 0)
            return Verdict.Reject($"scam signals: {string.Join(", ", scam.Take(3))}");

        // line 0: cheap pre-checks
        if (CheckLocation(p, prof) is { } locFail) return Verdict.Reject(locFail);
        if (CheckLevel(p, prof) is { } lvlFail) return Verdict.Reject(lvlFail);

        // line 1: comp
        var comp = CompParser.Parse(compText);
        if (comp.Kind == CompKind.Salary && comp.High is { } hi && hi < prof.Comp.SalaryFloor)
            return Verdict.Reject(
                $"comp: tops out at ${hi:N0}, floor is ${prof.Comp.SalaryFloor:N0}", comp);
        if (comp.Kind == CompKind.Hourly && comp.High is { } hhi && hhi < prof.Comp.HourlyFloor)
            return Verdict.Reject(
                $"comp: tops out at ${hhi}/hr, floor is ${prof.Comp.HourlyFloor}/hr", comp);

        // line 2: load-bearing quals
        var primary = Hits(prof.Stack.Primary.Keys, req);
        var adjacent = Hits(prof.Stack.Adjacent.Keys, req);

        if (primary.Count == 0 && adjacent.Count == 0)
        {
            // Some boards bury the stack in a "Tech we use" section outside the
            // requirements block. Check the whole body before giving up.
            primary = Hits(prof.Stack.Primary.Keys, hay);
            if (primary.Count == 0)
                return Verdict.Reject("quals: no load-bearing stack overlap in requirements", comp);
        }

        if (primary.Count == 0 && adjacent.Count < 2)
            return Verdict.Reject(
                $"quals: only adjacent overlap ({string.Join(", ", adjacent)}), no primary stack", comp);

        // gap map: hard walls (brief.md section 4)
        // A keyword that clears an ATS but collapses in the first technical screen
        // is worse than not applying. These are gates, not preferences.
        var gated = Hits(prof.GapsHard, req);
        if (gated.Count > 0)
            return Verdict.Reject(
                $"gap-gated on required quals: {string.Join(", ", gated)}",
                comp, [.. primary, .. adjacent]);

        // line 3: day shape
        var run = Hits(prof.DayShape.RunSignals, hay);
        var build = Hits(prof.DayShape.BuildSignals, hay);
        var shape = build.Count > run.Count ? "build" : run.Count > 0 ? "run" : "unclear";

        if (run.Count >= 3 && run.Count > build.Count)
            return Verdict.Reject(
                $"day shape: run-the-system role ({string.Join(", ", run.Take(4))})",
                comp, [.. primary, .. adjacent]);

        return Verdict.Pass(comp, [.. primary, .. adjacent], shape);
    }
}
