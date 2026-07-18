using System.Net;
using System.Text.RegularExpressions;

namespace Jobscan.Fetch;

/// <summary>Shared HTTP + text cleanup. Nothing clever, deliberately.</summary>
public static partial class Http
{
    public const string UserAgent =
        "jobscan/0.1 (+https://github.com/jameymcelveen/jobscan) personal job search";

    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(10),
        })
        {
            Timeout = TimeSpan.FromSeconds(20),
        };
        c.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return c;
    }

    /// <summary>
    /// GET, returning null on any failure. A dead board must never take down a run:
    /// one wrong token in the watchlist should cost one source, not the whole scan.
    /// </summary>
    public static async Task<string?> GetStringOrNull(string url, CancellationToken ct = default)
    {
        try
        {
            using var resp = await Client.GetAsync(url, ct);
            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"  [404] {url}");
                Console.WriteLine("        token probably wrong, fix companies.jsonc");
                return null;
            }
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(ct);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Console.WriteLine($"  [err] {url}: {e.Message}");
            return null;
        }
    }

    [GeneratedRegex(@"<(br|/p|/div|/li)\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BreakTag();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex AnyTag();

    [GeneratedRegex(@"[ \t\r\f\v]+")]
    private static partial Regex HSpace();

    [GeneratedRegex(@"\n\s*\n+")]
    private static partial Regex BlankLines();

    /// <summary>Tags out, entities decoded, whitespace sane. Structure is not preserved
    /// beyond paragraph breaks, because the filter reads words, not layout.</summary>
    public static string StripHtml(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = WebUtility.HtmlDecode(s);
        s = BreakTag().Replace(s, "\n");
        s = AnyTag().Replace(s, " ");
        s = WebUtility.HtmlDecode(s);       // entities can survive one pass
        s = HSpace().Replace(s, " ");
        s = BlankLines().Replace(s, "\n\n");
        return s.Trim();
    }
}
