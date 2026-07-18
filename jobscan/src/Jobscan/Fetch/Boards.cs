using System.Text.Json;
using System.Xml.Linq;
using Jobscan.Config;
using Jobscan.Model;

namespace Jobscan.Fetch;

/// <summary>
/// Public JSON/RSS board adapters.
///
/// Every source here publishes machine-readable endpoints on purpose. There is no
/// bot wall to defeat because none of these have one. Walled boards (LinkedIn,
/// Indeed, Workday tenants) are handled by <see cref="PasteAdapter"/>: a human
/// copies the text in. See CLAUDE.md.
/// </summary>
public static class Boards
{
    private static string Str(JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "" : "";

    private static DateTimeOffset? Date(string s) =>
        DateTimeOffset.TryParse(s, out var d) ? d : null;

    // -- greenhouse ---------------------------------------------------------

    public static async Task<List<Posting>> Greenhouse(string company, string token, string domain)
    {
        var url = $"https://boards-api.greenhouse.io/v1/boards/{token}/jobs?content=true";
        var raw = await Http.GetStringOrNull(url);
        if (raw is null) return [];

        var list = new List<Posting>();
        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("jobs", out var jobs)) return [];

        foreach (var j in jobs.EnumerateArray())
        {
            var comp = "";
            if (j.TryGetProperty("metadata", out var meta) && meta.ValueKind == JsonValueKind.Array)
            {
                foreach (var m in meta.EnumerateArray())
                {
                    var name = Str(m, "name");
                    if (name.Contains("salary", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("compensation", StringComparison.OrdinalIgnoreCase))
                        comp += " " + Str(m, "value");
                }
            }

            list.Add(new Posting
            {
                Source = "greenhouse",
                Company = company,
                Title = Str(j, "title"),
                Url = Str(j, "absolute_url"),
                Location = j.TryGetProperty("location", out var loc) ? Str(loc, "name") : "",
                Body = Http.StripHtml(Str(j, "content")),
                PostedAt = Date(Str(j, "updated_at")) ?? Date(Str(j, "first_published")),
                Domain = domain,
                CompRaw = comp.Trim(),
            });
        }
        return list;
    }

    // -- lever --------------------------------------------------------------

    public static async Task<List<Posting>> Lever(string company, string token, string domain)
    {
        var url = $"https://api.lever.co/v0/postings/{token}?mode=json";
        var raw = await Http.GetStringOrNull(url);
        if (raw is null) return [];

        var list = new List<Posting>();
        using var doc = JsonDocument.Parse(raw);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];

        foreach (var j in doc.RootElement.EnumerateArray())
        {
            var body = Http.StripHtml(
                Str(j, "descriptionPlain") is { Length: > 0 } p ? p : Str(j, "description"));

            // Lever splits requirements into `lists`. That's exactly the block we care
            // about most, so append it rather than dropping it.
            if (j.TryGetProperty("lists", out var lists) && lists.ValueKind == JsonValueKind.Array)
                foreach (var l in lists.EnumerateArray())
                    body += "\n\n" + Http.StripHtml(Str(l, "text")) + "\n" + Http.StripHtml(Str(l, "content"));

            DateTimeOffset? posted = null;
            if (j.TryGetProperty("createdAt", out var ts) && ts.TryGetInt64(out var ms))
                posted = DateTimeOffset.FromUnixTimeMilliseconds(ms);

            var cats = j.TryGetProperty("categories", out var c) ? c : default;

            list.Add(new Posting
            {
                Source = "lever",
                Company = company,
                Title = Str(j, "text"),
                Url = Str(j, "hostedUrl"),
                Location = cats.ValueKind == JsonValueKind.Object ? Str(cats, "location") : "",
                Body = body.Trim(),
                PostedAt = posted,
                Domain = domain,
            });
        }
        return list;
    }

    // -- ashby --------------------------------------------------------------

    public static async Task<List<Posting>> Ashby(string company, string token, string domain)
    {
        var url = $"https://api.ashbyhq.com/posting-api/job-board/{token}?includeCompensation=true";
        var raw = await Http.GetStringOrNull(url);
        if (raw is null) return [];

        var list = new List<Posting>();
        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("jobs", out var jobs)) return [];

        foreach (var j in jobs.EnumerateArray())
        {
            var comp = j.TryGetProperty("compensation", out var c) && c.ValueKind == JsonValueKind.Object
                ? Str(c, "compensationTierSummary") : "";

            list.Add(new Posting
            {
                Source = "ashby",
                Company = company,
                Title = Str(j, "title"),
                Url = Str(j, "jobUrl"),
                Location = Str(j, "location"),
                Body = Http.StripHtml(
                    Str(j, "descriptionHtml") is { Length: > 0 } h ? h : Str(j, "descriptionPlain")),
                PostedAt = Date(Str(j, "publishedAt")),
                Domain = domain,
                CompRaw = comp,
            });
        }
        return list;
    }

    // -- rss ----------------------------------------------------------------

    public static async Task<List<Posting>> Rss(string company, string feedUrl, string domain)
    {
        var raw = await Http.GetStringOrNull(feedUrl);
        if (raw is null) return [];

        XDocument doc;
        try { doc = XDocument.Parse(raw); }
        catch (System.Xml.XmlException e)
        {
            Console.WriteLine($"  [err] bad xml {feedUrl}: {e.Message}");
            return [];
        }

        var list = new List<Posting>();
        foreach (var item in doc.Descendants("item"))
        {
            string T(string n) => item.Element(n)?.Value.Trim() ?? "";
            list.Add(new Posting
            {
                Source = "rss",
                Company = T("author") is { Length: > 0 } a ? a : company,
                Title = T("title"),
                Url = T("link"),
                Body = Http.StripHtml(T("description")),
                PostedAt = Date(T("pubDate")),
                Domain = domain,
            });
        }
        return list;
    }

    // -- aggregators --------------------------------------------------------

    public static async Task<List<Posting>> Remotive(string url, string domain)
    {
        var raw = await Http.GetStringOrNull(url);
        if (raw is null) return [];

        var list = new List<Posting>();
        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("jobs", out var jobs)) return [];

        foreach (var j in jobs.EnumerateArray())
        {
            list.Add(new Posting
            {
                Source = "remotive",
                Company = Str(j, "company_name"),
                Title = Str(j, "title"),
                Url = Str(j, "url"),
                Location = Str(j, "candidate_required_location") is { Length: > 0 } l ? l : "Remote",
                Body = Http.StripHtml(Str(j, "description")),
                PostedAt = Date(Str(j, "publication_date")),
                Domain = domain,
                CompRaw = Str(j, "salary"),
            });
        }
        return list;
    }

    public static async Task<List<Posting>> RemoteOk(string url, string domain)
    {
        var raw = await Http.GetStringOrNull(url);
        if (raw is null) return [];

        var list = new List<Posting>();
        using var doc = JsonDocument.Parse(raw);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];

        foreach (var j in doc.RootElement.EnumerateArray())
        {
            // First element of the RemoteOK feed is a legal notice blob, not a job.
            if (j.ValueKind != JsonValueKind.Object) continue;
            var position = Str(j, "position");
            if (string.IsNullOrWhiteSpace(position)) continue;

            var sal = "";
            if (j.TryGetProperty("salary_min", out var lo) && lo.ValueKind == JsonValueKind.Number)
            {
                var hi = j.TryGetProperty("salary_max", out var h) && h.ValueKind == JsonValueKind.Number
                    ? h.GetInt64() : lo.GetInt64();
                sal = $"${lo.GetInt64()}-${hi}";
            }

            list.Add(new Posting
            {
                Source = "remoteok",
                Company = Str(j, "company"),
                Title = position,
                Url = Str(j, "url"),
                Location = Str(j, "location") is { Length: > 0 } l ? l : "Remote",
                Body = Http.StripHtml(Str(j, "description")),
                PostedAt = Date(Str(j, "date")),
                Domain = domain,
                CompRaw = sal,
            });
        }
        return list;
    }

    // -- orchestration ------------------------------------------------------

    public static async Task<List<Posting>> FetchAll(Watchlist cfg)
    {
        var all = new List<Posting>();

        foreach (var c in cfg.Companies.Where(x => x.Active && x.Token is not null))
        {
            Func<string, string, string, Task<List<Posting>>>? fn = c.Board switch
            {
                "greenhouse" => Greenhouse,
                "lever" => Lever,
                "ashby" => Ashby,
                _ => null,
            };
            if (fn is null)
            {
                if (c.Board is not "workday")
                    Console.WriteLine($"  [skip] {c.Name}: no adapter for '{c.Board}'");
                continue;
            }

            Console.WriteLine($"  {c.Name} ({c.Board})...");
            var got = await fn(c.Name, c.Token!, c.Domain);
            Console.WriteLine($"    {got.Count} postings");
            all.AddRange(got);
            await Task.Delay(500);   // be a polite client
        }

        foreach (var f in cfg.Feeds.Where(x => x.Active && x.Token is not null))
        {
            Console.WriteLine($"  {f.Name} (rss)...");
            var got = await Rss(f.Name, f.Token!, f.Domain);
            Console.WriteLine($"    {got.Count} postings");
            all.AddRange(got);
            await Task.Delay(500);
        }

        foreach (var a in cfg.Aggregators.Where(x => x.Active))
        {
            Func<string, string, Task<List<Posting>>>? fn = a.Kind switch
            {
                "remotive" => Remotive,
                "remoteok" => RemoteOk,
                _ => null,
            };
            if (fn is null) continue;

            Console.WriteLine($"  {a.Name} ({a.Kind})...");
            var got = await fn(a.Url, a.Domain);
            Console.WriteLine($"    {got.Count} postings");
            all.AddRange(got);
            await Task.Delay(500);
        }

        return all.DistinctBy(p => p.Id).ToList();
    }
}
