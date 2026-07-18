using Jobscan.Config;
using Jobscan.Fetch;
using Jobscan.Filtering;
using Jobscan.Model;
using Jobscan.Reporting;
using Jobscan.Scoring;
using Jobscan.Storage;

namespace Jobscan;

/// <summary>
/// jobscan. Find the jobs, filter them hard, rank the survivors, hand over a list.
///
///   jobscan                          scan default profile (jamey)
///   jobscan --profile riker          scan one profile
///   jobscan --all-profiles           scan every profile in profiles/index.json
///   jobscan --paste-only             only score profiles/&lt;p&gt;/data/paste/*.txt
///   jobscan --all                    include previously seen postings
///   jobscan --dry                    print, write nothing
///   jobscan --serve                  serve the dashboard (PORT env or 8080)
///   jobscan --daemon                 serve + rescan all profiles every 6 hours (Railway mode)
///
/// Kits are never generated here. Human-triggered only. See CLAUDE.md.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var root = FindRepoRoot();
        Directory.SetCurrentDirectory(root);

        if (args.Contains("--serve")) return await Server.Run(scanLoop: false);
        if (args.Contains("--daemon")) return await Server.Run(scanLoop: true);

        var profiles = ResolveProfiles(args);
        var rc = 0;
        foreach (var p in profiles)
            rc = Math.Max(rc, await ScanProfile(p, args));
        return rc;
    }

    public static List<string> ResolveProfiles(string[] args)
    {
        if (args.Contains("--all-profiles"))
        {
            using var idx = System.Text.Json.JsonDocument.Parse(File.ReadAllText("profiles/index.json"));
            return idx.RootElement.GetProperty("profiles").EnumerateArray()
                .Select(e => e.GetString()!).ToList();
        }
        var i = Array.IndexOf(args, "--profile");
        return [i >= 0 && i + 1 < args.Length ? args[i + 1] : "jamey"];
    }

    public static async Task<int> ScanProfile(string name, string[] args)
    {
        var all = args.Contains("--all");
        var pasteOnly = args.Contains("--paste-only");
        var dry = args.Contains("--dry");
        var dir = Path.Combine("profiles", name);

        Profile profile;
        Watchlist watchlist;
        try
        {
            profile = ConfigLoader.Load<Profile>(Path.Combine(dir, "profile.jsonc"));
            watchlist = ConfigLoader.Load<Watchlist>(Path.Combine(dir, "companies.jsonc"));
        }
        catch (Exception e) when (e is FileNotFoundException or InvalidDataException)
        {
            Console.Error.WriteLine($"[{name}] config error: {e.Message}");
            return 2;
        }

        if (profile.Rubric.Total != 100)
            Console.WriteLine($"  [warn] rubric weights sum to {profile.Rubric.Total}, not 100");

        Console.WriteLine($"[{name}] fetching...");
        var postings = pasteOnly ? [] : await Boards.FetchAll(watchlist);

        var pasted = PasteAdapter.Load(Path.Combine(dir, "data", "paste"));
        if (pasted.Count > 0) Console.WriteLine($"  {pasted.Count} pasted postings");
        postings.AddRange(pasted);
        Console.WriteLine($"total: {postings.Count} postings\n");

        var store = new SeenStore(Path.Combine(dir, "data", "seen.json"));
        var fresh = store.MarkAndReturnNew(postings);
        var working = all ? postings : fresh;
        Console.WriteLine($"new since last run: {fresh.Count}");
        Console.WriteLine($"scoring {working.Count}...\n");

        var keep = new List<Hit>();
        var rejects = new List<(Posting, Verdict)>();

        foreach (var p in working)
        {
            var v = ThreeLineFilter.Evaluate(p, profile);
            if (!v.Passed) { rejects.Add((p, v)); continue; }

            var s = RubricScorer.Score(p, v, profile);
            if (s.Total < profile.Thresholds.BacklogMinScore)
            {
                rejects.Add((p, v with
                {
                    Reason = $"score: {s.Total}, below backlog threshold " +
                             $"{profile.Thresholds.BacklogMinScore}",
                }));
                continue;
            }
            keep.Add(new Hit(p, v, s, ComputeFlags(p, v, profile)));
        }

        keep = [.. keep.OrderByDescending(h => h.Score.Total)];

        var stats = new ScanStats
        {
            Sources = watchlist.Companies.Count(c => c.Active),
            Seen = postings.Count,
            New = fresh.Count,
            Rejected = rejects.Count,
        };

        var backlog = Reports.Backlog(keep, profile, stats);
        if (dry) { Console.WriteLine(backlog); return 0; }

        await File.WriteAllTextAsync(Path.Combine(dir, "backlog.md"), backlog);
        await File.WriteAllTextAsync(Path.Combine(dir, "rejected.md"), Reports.Rejected(rejects));
        await File.WriteAllTextAsync(Path.Combine(dir, "backlog.json"),
            Reports.BacklogJson(name, keep, profile, stats));
        store.Save();

        var pri = profile.Thresholds.PriorityMinScore;
        Console.WriteLine($"[{name}] backlog: {keep.Count} postings " +
                          $"({keep.Count(h => h.Score.Total >= pri)} priority tier), " +
                          $"{rejects.Count} rejected");
        foreach (var h in keep.Take(8))
            Console.WriteLine($"  {h.Score.Total,3}  {h.Posting.Company}: {h.Posting.Title}" +
                              (h.Flags.Count > 0 ? $"  [{string.Join("; ", h.Flags)}]" : ""));
        return 0;
    }

    /// <summary>
    /// The flags system: advisory annotations that never change the score.
    /// Scores stay comparable across the whole backlog; judgment calls stay visible
    /// and stay human. New rules land here, not in the scorer.
    /// </summary>
    public static List<string> ComputeFlags(Posting p, Verdict v, Profile prof)
    {
        var flags = new List<string>();

        if (p.Domain is "mission" or "faith")
            flags.Add("check the 990: nonprofit band may be aspirational");

        var req = Requirements.Block(p.Body).ToLowerInvariant();
        if (req.Contains("agentic") || req.Contains("ai agent") ||
            (req.Contains("llm") && req.Contains("agent")))
            flags.Add("agentic-primary: AI fleet is the qualification here");

        var loc = p.Location.ToLowerInvariant();
        string[] premiumMarkers = ["daytona", "orlando", "toccoa", "gainesville ga",
                                   "long beach", "los angeles", "anaheim", "irvine"];
        if (premiumMarkers.Any(loc.Contains))
            flags.Add("relocation premium: effective floor $160K, geo layer pending");

        return flags;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "profiles")) &&
                Directory.Exists(Path.Combine(dir.FullName, "src"))) return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }
}
