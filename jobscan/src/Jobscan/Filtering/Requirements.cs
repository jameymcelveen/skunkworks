using System.Text.RegularExpressions;

namespace Jobscan.Filtering;

/// <summary>
/// Titles lie. Requirement lists don't. (brief.md section 3)
/// This pulls the load-bearing section so the filter reads the gate, not the marketing.
/// </summary>
public static partial class Requirements
{
    [GeneratedRegex(
        @"(what you.?ll need|requirements|qualifications|what we.?re looking for|" +
        @"who you are|must have|required skills|basic qualifications|minimum qualifications|" +
        @"you have|skills? (?:and|&) experience|experience required)",
        RegexOptions.IgnoreCase)]
    private static partial Regex ReqHead();

    [GeneratedRegex(
        @"(nice to have|preferred|bonus|benefits|perks|compensation|salary|about (?:us|the team)|" +
        @"why join|equal opportunity|eeo|what we offer|our stack|responsibilities)",
        RegexOptions.IgnoreCase)]
    private static partial Regex NextHead();

    /// <summary>
    /// Returns the requirements block, or the whole body if we can't find headers.
    /// Falling back to the whole body is safe: it only makes the filter more
    /// permissive, and a human reads every survivor anyway. Being too strict here
    /// silently drops good jobs, which is the expensive failure.
    /// </summary>
    public static string Block(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "";

        var m = ReqHead().Match(body);
        if (!m.Success) return body;

        var start = m.Index + m.Length;
        if (start >= body.Length) return body;

        var searchFrom = Math.Min(start + 40, body.Length);
        var n = NextHead().Match(body, searchFrom);
        var end = n.Success ? n.Index : Math.Min(body.Length, start + 2500);

        var block = body[start..end].Trim();
        return block.Length > 120 ? block : body;
    }
}

public enum CompKind { Unknown, Salary, Hourly }

public readonly record struct CompRange(int? Low, int? High, CompKind Kind)
{
    public static readonly CompRange None = new(null, null, CompKind.Unknown);
}

public static partial class CompParser
{
    [GeneratedRegex(
        @"\$\s?(\d{2,3})(?:,(\d{3}))?\s?(k\b)?(?:\s?(?:-|–|—|to)\s?\$?\s?(\d{2,3})(?:,(\d{3}))?\s?(k\b)?)?",
        RegexOptions.IgnoreCase)]
    private static partial Regex Salary();

    [GeneratedRegex(@"\$\s?(\d{2,3})(?:\.\d+)?\s?(?:/|\s?per\s?)\s?(?:hr|hour)", RegexOptions.IgnoreCase)]
    private static partial Regex Hourly();

    private static int? Normalize(string a, string b, string k)
    {
        if (!string.IsNullOrEmpty(b)) return int.Parse(a + b);      // 135,000
        var n = int.Parse(a);
        if (!string.IsNullOrEmpty(k)) return n * 1000;              // 135k
        if (n < 500) return n * 1000;                              // bare "$135" means 135k here
        return n;
    }

    public static CompRange Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return CompRange.None;

        var hourly = Hourly().Matches(text)
            .Select(m => int.Parse(m.Groups[1].Value))
            .Where(v => v is >= 10 and <= 400)
            .ToList();
        if (hourly.Count > 0)
            return new CompRange(hourly.Min(), hourly.Max(), CompKind.Hourly);

        (int Low, int High)? best = null;
        foreach (Match m in Salary().Matches(text))
        {
            var lo = Normalize(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
            if (lo is null or < 30_000 or > 900_000) continue;

            var hi = m.Groups[4].Success
                ? Normalize(m.Groups[4].Value, m.Groups[5].Value, m.Groups[6].Value) ?? lo
                : lo;

            // Take the widest top: postings often mention several numbers
            // (equity, bonus, other bands). The real band is usually the highest.
            if (best is null || hi > best.Value.High)
                best = (lo.Value, hi.Value);
        }

        return best is null
            ? CompRange.None
            : new CompRange(best.Value.Low, best.Value.High, CompKind.Salary);
    }
}
