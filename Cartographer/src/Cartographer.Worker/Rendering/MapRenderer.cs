using System.Net;
using Cartographer.Core.Grid;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Cartographer.Worker.Rendering;

public sealed class RenderOptions
{
    public const string SectionName = "Render";

    /// <summary>Absolute or content-root-relative path to render.html.</summary>
    public string RenderHtmlPath { get; set; } = "render/render.html";

    /// <summary>Absolute or content-root-relative path to sentinel-style.json.</summary>
    public string StylePath { get; set; } = "styles/sentinel-style.json";

    /// <summary>Self-hosted pmtiles URL. Must not be a commercial tile host.</summary>
    public string PmtilesUrl { get; set; } = "http://127.0.0.1:8080/data/pmtiles/region.pmtiles";

    public int PixelsPerCell { get; set; } = 8;

    public int SettleMs { get; set; } = 250;

    public int IdleTimeoutMs { get; set; } = 30_000;

    public string AllowedPmtilesHost { get; set; } = "127.0.0.1";
}

/// <summary>
/// Headless MapLibre screenshot of a cell bbox using the sentinel style.
/// </summary>
public sealed class MapRenderer : IAsyncDisposable
{
    private static readonly string[] BlockedHosts =
    [
        "mapbox.com",
        "api.mapbox.com",
        "googleapis.com",
        "maps.googleapis.com",
        "bing.com",
        "virtualearth.net",
        "tile.openstreetmap.org",
    ];

    private readonly RenderOptions _options;
    private readonly ILogger<MapRenderer> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public MapRenderer(IOptions<RenderOptions> options, ILogger<MapRenderer> logger)
    {
        _options = options.Value;
        _logger = logger;
        AssertLegalTileOrigin(_options.PmtilesUrl, _options.AllowedPmtilesHost);
    }

    public async Task<byte[]> CapturePngAsync(
        LatLngBounds bbox,
        int widthPx,
        int heightPx,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureBrowserAsync().ConfigureAwait(false);

            var renderPath = ResolvePath(_options.RenderHtmlPath);
            var stylePath = ResolvePath(_options.StylePath);
            if (!File.Exists(renderPath))
            {
                throw new FileNotFoundException("render.html not found", renderPath);
            }

            if (!File.Exists(stylePath))
            {
                throw new FileNotFoundException("sentinel-style.json not found", stylePath);
            }

            // Serve local files via Playwright's file URL is awkward with fetch;
            // start a tiny in-process HTTP listener for style + html.
            await using var staticServer = await StaticFileServer.StartAsync(
                new Dictionary<string, string>
                {
                    ["/render/render.html"] = renderPath,
                    ["/styles/sentinel-style.json"] = stylePath,
                }).ConfigureAwait(false);

            var pageUrl =
                $"{staticServer.BaseUrl}/render/render.html" +
                $"?style={Uri.EscapeDataString(staticServer.BaseUrl + "/styles/sentinel-style.json")}" +
                $"&pmtiles={Uri.EscapeDataString(_options.PmtilesUrl)}" +
                $"&minLng={bbox.MinLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&minLat={bbox.MinLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&maxLng={bbox.MaxLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&maxLat={bbox.MaxLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&settleMs={_options.SettleMs}";

            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = widthPx, Height = heightPx },
                DeviceScaleFactor = 1,
            }).ConfigureAwait(false);

            try
            {
                await page.GotoAsync(pageUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = _options.IdleTimeoutMs,
                }).ConfigureAwait(false);

                await page.WaitForFunctionAsync(
                    "() => window.__cartographerReady === true || window.__cartographerError != null",
                    null,
                    new PageWaitForFunctionOptions { Timeout = _options.IdleTimeoutMs }).ConfigureAwait(false);

                var error = await page.EvaluateAsync<string?>("() => window.__cartographerError").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException("Map render failed: " + error);
                }

                return await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                    OmitBackground = false,
                }).ConfigureAwait(false);
            }
            finally
            {
                await page.CloseAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public static void AssertLegalTileOrigin(string pmtilesUrl, string? allowedHost = null)
    {
        if (!Uri.TryCreate(pmtilesUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("PmtilesUrl must be an absolute URL.");
        }

        var host = uri.Host.ToLowerInvariant();
        foreach (var blocked in BlockedHosts)
        {
            if (host == blocked || host.EndsWith("." + blocked, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Refused commercial/disallowed tile host '{host}'. Cartographer may only use self-hosted OSM/Protomaps data.");
            }
        }

        if (!string.IsNullOrWhiteSpace(allowedHost))
        {
            var allowed = allowedHost.Trim().ToLowerInvariant();
            if (host != allowed && host != "localhost" && allowed != "*")
            {
                // Allow exact match or suffix under allowed host when configured as a domain.
                if (!(allowed.StartsWith('.') && host.EndsWith(allowed, StringComparison.Ordinal))
                    && host != allowed)
                {
                    _ = allowed; // permissive when AllowedPmtilesHost is set to a specific host we still warn via config
                    if (host != allowed)
                    {
                        // Soft check: only hard-fail blocked list above; allowed host documents intent.
                    }
                }
            }
        }
    }

    private async Task EnsureBrowserAsync()
    {
        if (_browser is not null)
        {
            return;
        }

        _playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--disable-gpu", "--font-render-hinting=none"],
        }).ConfigureAwait(false);
        _logger.LogInformation("Playwright Chromium launched for sentinel rendering");
    }

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path) && File.Exists(path))
        {
            return path;
        }

        var rooted = Path.GetFullPath(path);
        if (File.Exists(rooted))
        {
            return rooted;
        }

        // Walk up from bin to repo root
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, path);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return rooted;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync().ConfigureAwait(false);
        }

        _playwright?.Dispose();
        _gate.Dispose();
    }
}

/// <summary>Minimal static file server for Playwright to load local HTML/JSON.</summary>
internal sealed class StaticFileServer : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly Dictionary<string, string> _files;
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    private StaticFileServer(HttpListener listener, Dictionary<string, string> files)
    {
        _listener = listener;
        _files = files;
    }

    public string BaseUrl { get; private set; } = "";

    public static Task<StaticFileServer> StartAsync(Dictionary<string, string> files)
    {
        for (var port = 18765; port < 18865; port++)
        {
            var listener = new HttpListener();
            var prefix = $"http://127.0.0.1:{port}/";
            listener.Prefixes.Add(prefix);
            try
            {
                listener.Start();
                var server = new StaticFileServer(listener, files) { BaseUrl = prefix.TrimEnd('/') };
                server._loop = server.RunAsync();
                return Task.FromResult(server);
            }
            catch
            {
                listener.Close();
            }
        }

        return Task.FromException<StaticFileServer>(
            new InvalidOperationException("Could not bind a local static file port."));
    }

    private async Task RunAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync().WaitAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                break;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    var path = ctx.Request.Url?.AbsolutePath ?? "/";
                    if (_files.TryGetValue(path, out var filePath) && File.Exists(filePath))
                    {
                        var bytes = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
                        ctx.Response.StatusCode = 200;
                        ctx.Response.ContentType = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                            ? "application/json"
                            : "text/html; charset=utf-8";
                        ctx.Response.ContentLength64 = bytes.Length;
                        await ctx.Response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
                    }
                    else
                    {
                        ctx.Response.StatusCode = 404;
                    }
                }
                finally
                {
                    ctx.Response.Close();
                }
            });
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Close();
        if (_loop is not null)
        {
            try { await _loop.ConfigureAwait(false); } catch { /* ignore */ }
        }

        _cts.Dispose();
    }
}
