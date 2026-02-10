using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace InfoScopeDeveloperToolkit.Core.Services;

public static partial class ErrorThreadSummaryGenerator
{
    private static readonly string[] SuspiciousTerms = ["Exception", "failed", "érvénytelen", "SSL_", "Login failed"];

    public static string BuildSummary(string input)
    {
        var threads = new Dictionary<string, ThreadBucket>(StringComparer.OrdinalIgnoreCase);
        var errorLineCount = 0;

        using var reader = new StringReader(input ?? string.Empty);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var parsed = ParseLine(line);
            var bucketKey = string.IsNullOrWhiteSpace(parsed.ThreadId) ? "unknown" : parsed.ThreadId;
            if (!threads.TryGetValue(bucketKey, out var bucket))
            {
                bucket = new ThreadBucket(bucketKey);
                threads.Add(bucketKey, bucket);
            }

            bucket.TotalLines++;
            bucket.LastTimestamp = parsed.Timestamp ?? bucket.LastTimestamp;

            if (bucket.FirstTimestamp is null && parsed.Timestamp is not null)
            {
                bucket.FirstTimestamp = parsed.Timestamp;
            }

            if (IsErrorLine(parsed))
            {
                errorLineCount++;
                bucket.HasError = true;
                if (bucket.FirstErrorTimestamp is null)
                {
                    bucket.FirstErrorTimestamp = parsed.Timestamp;
                }

                bucket.FirstErrorLine ??= FormatLine(parsed);
                bucket.LastErrorLine = FormatLine(parsed);

                bucket.ErrorKind = string.IsNullOrWhiteSpace(bucket.ErrorKind)
                    ? ClassifyError(parsed.RawLine)
                    : bucket.ErrorKind;
            }

            if (bucket.ExceptionHead is null && parsed.RawLine.Contains("Exception", StringComparison.OrdinalIgnoreCase))
            {
                bucket.ExceptionHead = parsed.RawLine.Length > 220
                    ? parsed.RawLine[..220] + "..."
                    : parsed.RawLine;
            }
        }

        var errorThreads = threads.Values
            .Where(t => t.HasError)
            .OrderBy(t => t.FirstErrorTimestamp ?? DateTimeOffset.MaxValue)
            .ThenBy(t => t.ThreadId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var sb = new StringBuilder();
        sb.AppendLine($"Error count: {errorLineCount}");
        sb.AppendLine($"Unique thread count: {errorThreads.Length}");

        for (var i = 0; i < errorThreads.Length; i++)
        {
            var thread = errorThreads[i];
            sb.AppendLine();
            sb.AppendLine($"===== ERROR THREAD #{i + 1} =====");
            sb.AppendLine($"ThreadId: {thread.ThreadId}");
            sb.AppendLine($"Első hiba időpontja: {FormatTimestamp(thread.FirstErrorTimestamp)}");
            sb.AppendLine($"Utolsó sor időpontja: {FormatTimestamp(thread.LastTimestamp)}");
            sb.AppendLine($"Sorok száma: {thread.TotalLines}");
            sb.AppendLine();
            sb.AppendLine("Fő hiba:");
            sb.AppendLine(string.IsNullOrWhiteSpace(thread.ErrorKind) ? "Ismeretlen hiba minta" : thread.ErrorKind);
            sb.AppendLine();
            sb.AppendLine("Kulcssorok:");
            sb.AppendLine(thread.FirstErrorLine ?? "(nincs első hiba sor)");
            sb.AppendLine(thread.LastErrorLine ?? "(nincs utolsó hiba sor)");
            if (!string.IsNullOrWhiteSpace(thread.ExceptionHead))
            {
                sb.AppendLine($"Exception eleje: {thread.ExceptionHead}");
            }
        }

        if (errorThreads.Length == 0)
        {
            sb.AppendLine();
            sb.AppendLine("Nem találtam hibás threadet a megadott logban.");
        }

        return sb.ToString();
    }

    private static bool IsErrorLine(ParsedLine line)
    {
        var levelHit = line.Level.Equals("Error", StringComparison.OrdinalIgnoreCase)
            || line.Level.Equals("Exception", StringComparison.OrdinalIgnoreCase)
            || line.RawLine.Contains("Exception", StringComparison.OrdinalIgnoreCase);

        if (levelHit)
        {
            return true;
        }

        return SuspiciousTerms.Any(term => line.RawLine.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string ClassifyError(string line)
    {
        if (line.Contains("SSL_", StringComparison.OrdinalIgnoreCase) || line.Contains("ssl", StringComparison.OrdinalIgnoreCase))
        {
            return "SSL autentikációs hiba";
        }

        if (line.Contains("Login failed", StringComparison.OrdinalIgnoreCase))
        {
            return "Login failed – felhasználó hitelesítési hiba";
        }

        if (line.Contains("érvénytelen", StringComparison.OrdinalIgnoreCase))
        {
            return "Érvénytelen adat vagy azonosító";
        }

        if (line.Contains("Exception", StringComparison.OrdinalIgnoreCase))
        {
            return "Kivétel a feldolgozás során";
        }

        if (line.Contains("failed", StringComparison.OrdinalIgnoreCase))
        {
            return "Általános sikertelen művelet";
        }

        return "Nem besorolt hiba";
    }

    private static string FormatLine(ParsedLine line)
    {
        var level = string.IsNullOrWhiteSpace(line.Level) ? "raw" : line.Level;
        var message = string.IsNullOrWhiteSpace(line.Message) ? line.RawLine : line.Message;
        return $"[{level}] {message}";
    }

    private static string FormatTimestamp(DateTimeOffset? timestamp)
        => timestamp?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "ismeretlen";

    private static ParsedLine ParseLine(string line)
    {
        var threadId = ExtractThreadId(line);
        var timestamp = ExtractTimestamp(line);
        var level = ExtractLevel(line);
        var message = ExtractMessage(line, level);

        return new ParsedLine(line, timestamp, threadId, level, message);
    }

    private static string ExtractThreadId(string line)
    {
        var match = ThreadIdRegex().Match(line);
        return match.Success ? match.Groups[1].Value : "unknown";
    }

    private static DateTimeOffset? ExtractTimestamp(string line)
    {
        var match = TimestampRegex().Match(line);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Value;
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
        {
            return parsed;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
        {
            return new DateTimeOffset(dateTime);
        }

        return null;
    }

    private static string ExtractLevel(string line)
    {
        var knownLevels = new[] { "Error", "Warning", "Information", "Verbose", "Debug", "Trace", "Exception" };
        foreach (var level in knownLevels)
        {
            if (line.Contains(level, StringComparison.OrdinalIgnoreCase))
            {
                return level;
            }
        }

        return "raw";
    }

    private static string ExtractMessage(string line, string level)
    {
        if (level == "raw")
        {
            return line;
        }

        var index = line.IndexOf(level, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return line;
        }

        var start = index + level.Length;
        if (start >= line.Length)
        {
            return line;
        }

        return line[start..].Trim(' ', ':', '-', '|', '\t');
    }

    [GeneratedRegex(@"\|(\d+)\|")]
    private static partial Regex ThreadIdRegex();

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}")]
    private static partial Regex TimestampRegex();

    private sealed class ThreadBucket(string threadId)
    {
        public string ThreadId { get; } = threadId;
        public DateTimeOffset? FirstTimestamp { get; set; }
        public DateTimeOffset? LastTimestamp { get; set; }
        public DateTimeOffset? FirstErrorTimestamp { get; set; }
        public int TotalLines { get; set; }
        public bool HasError { get; set; }
        public string? ErrorKind { get; set; }
        public string? FirstErrorLine { get; set; }
        public string? LastErrorLine { get; set; }
        public string? ExceptionHead { get; set; }
    }

    private sealed record ParsedLine(string RawLine, DateTimeOffset? Timestamp, string ThreadId, string Level, string Message);
}
