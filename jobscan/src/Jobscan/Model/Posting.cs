using System.Security.Cryptography;
using System.Text;

namespace Jobscan.Model;

/// <summary>
/// The one shape everything downstream speaks. Board adapters normalize into this;
/// the filter and scorer never know or care where a posting came from.
/// </summary>
public sealed record Posting
{
    public required string Source { get; init; }      // greenhouse | lever | ashby | rss | paste ...
    public required string Company { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public string Location { get; init; } = "";
    public string Body { get; init; } = "";           // full text, tags stripped
    public DateTimeOffset? PostedAt { get; init; }
    public string Domain { get; init; } = "other";    // from companies.jsonc
    public string CompRaw { get; init; } = "";        // whatever comp string the board handed us

    private string? _id;

    /// <summary>Stable id across runs. Seeded from company+title+url, not from body,
    /// so a reworded posting is still the same posting.</summary>
    public string Id
    {
        get
        {
            if (_id is not null) return _id;
            var seed = $"{Company}|{Title}|{Url}".ToLowerInvariant();
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(seed));
            _id = Convert.ToHexString(hash)[..12].ToLowerInvariant();
            return _id;
        }
    }

    private string? _haystack;

    /// <summary>Lowercased title + location + body. Cached: the scorer hits this a lot.</summary>
    public string Haystack => _haystack ??= $"{Title}\n{Location}\n{Body}".ToLowerInvariant();
}
