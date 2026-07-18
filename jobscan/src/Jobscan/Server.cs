using System.Net;
using System.Text;

namespace Jobscan;

/// <summary>
/// Dashboard server + daemon mode. Zero packages: System.Net.HttpListener.
///
/// Serves web/index.html at "/" and repo files under /profiles/ (read-only,
/// path-traversal guarded). In daemon mode (Railway), a background loop rescans
/// every profile on an interval; the container is the whole deployment.
/// </summary>
public static class Server
{
    private static readonly Dictionary<string, string> Mime = new()
    {
        [".html"] = "text/html; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
        [".md"] = "text/markdown; charset=utf-8",
        [".svg"] = "image/svg+xml",
        [".css"] = "text/css",
        [".js"] = "text/javascript",
    };

    public static async Task<int> Run(bool scanLoop)
    {
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        var interval = TimeSpan.FromHours(
            double.TryParse(Environment.GetEnvironmentVariable("SCAN_INTERVAL_HOURS"), out var h) ? h : 6);

        if (scanLoop)
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        Console.WriteLine($"[daemon] scan sweep starting {DateTimeOffset.UtcNow:u}");
                        foreach (var p in Program.ResolveProfiles(["--all-profiles"]))
                            await Program.ScanProfile(p, []);
                    }
                    catch (Exception e)
                    {
                        // A failed sweep must never kill the server. Log and wait.
                        Console.Error.WriteLine($"[daemon] sweep failed: {e.Message}");
                    }
                    await Task.Delay(interval);
                }
            });
        }

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://*:{port}/");
        listener.Start();
        Console.WriteLine($"[serve] dashboard on http://localhost:{port}/  (root: {Directory.GetCurrentDirectory()})");

        while (true)
        {
            var ctx = await listener.GetContextAsync();
            _ = Task.Run(() => Handle(ctx));
        }
    }

    private static void Handle(HttpListenerContext ctx)
    {
        try
        {
            var raw = ctx.Request.Url?.AbsolutePath ?? "/";
            var path = raw switch
            {
                "/" => "web/index.html",
                "/favicon.svg" => "web/favicon.svg",
                _ => raw.TrimStart('/'),
            };

            // Read-only allowlist: the dashboard needs web/ and profiles/. Nothing else
            // is served, and resolved paths must stay inside the repo root.
            var full = Path.GetFullPath(path);
            var root = Path.GetFullPath(Directory.GetCurrentDirectory());
            var ok = full.StartsWith(root, StringComparison.Ordinal) &&
                     (path.StartsWith("web/") || path.StartsWith("profiles/"));

            if (!ok || !File.Exists(full)) { Write(ctx, 404, "text/plain", "not found"); return; }

            var ext = Path.GetExtension(full).ToLowerInvariant();
            Write(ctx, 200, Mime.GetValueOrDefault(ext, "application/octet-stream"), File.ReadAllBytes(full));
        }
        catch (Exception e)
        {
            try { Write(ctx, 500, "text/plain", e.Message); } catch { /* client gone */ }
        }
    }

    private static void Write(HttpListenerContext ctx, int status, string mime, string body) =>
        Write(ctx, status, mime, Encoding.UTF8.GetBytes(body));

    private static void Write(HttpListenerContext ctx, int status, string mime, byte[] body)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = mime;
        ctx.Response.ContentLength64 = body.Length;
        ctx.Response.OutputStream.Write(body);
        ctx.Response.Close();
    }
}
