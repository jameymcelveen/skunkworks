using Jobscan.Model;

namespace Jobscan.Fetch;

/// <summary>
/// The manual door for walled boards.
///
/// LinkedIn, Indeed, and Workday tenants are not scraped: ToS violation and
/// account-ban risk, and defeating a bot wall is out of scope permanently.
/// When a target company only posts behind one, a human copies the JD text into
/// data/paste/whatever.txt and it scores identically to an API-sourced posting.
/// One manual step. No ban risk. No CAPTCHA.
///
/// Format:
///   line 1: Company | Title | Url | Domain     (Url and Domain optional)
///   rest:   the JD text
/// </summary>
public static class PasteAdapter
{
    public const string Dir = "data/paste";

    public static List<Posting> Load(string dir = Dir, string domainDefault = "other")
    {
        if (!Directory.Exists(dir)) return [];

        var list = new List<Posting>();
        foreach (var f in Directory.GetFiles(dir, "*.txt").OrderBy(x => x))
        {
            var raw = File.ReadAllText(f).Trim();
            if (raw.Length == 0) continue;

            var nl = raw.IndexOf('\n');
            var head = nl < 0 ? raw : raw[..nl];
            var body = nl < 0 ? "" : raw[(nl + 1)..];

            var bits = head.Split('|', StringSplitOptions.TrimEntries);
            var stem = Path.GetFileNameWithoutExtension(f);
            var domain = bits.Length > 3 && bits[3].Length > 0 ? bits[3] : domainDefault;

            list.Add(new Posting
            {
                Source = "paste",
                Company = bits.Length > 0 && bits[0].Length > 0 ? bits[0] : stem,
                Title = bits.Length > 1 ? bits[1] : stem,
                Url = bits.Length > 2 ? bits[2] : $"file://{Path.GetFullPath(f)}",
                Body = body.Trim(),
                PostedAt = new DateTimeOffset(File.GetLastWriteTimeUtc(f), TimeSpan.Zero),
                Domain = domain,
            });
        }
        return list;
    }
}
