using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace InfoScopeDeveloperToolkit.Core.Services;

public sealed class ErrorThreadSummaryService : IErrorThreadSummaryService
{
    private static readonly Regex ThreadRegex = new("\\|(?<id>\\d+)\\|", RegexOptions.Compiled);
    private static readonly Regex TimestampRegex = new(
        "(?<ts>\\d{4}-\\d{2}-\\d{2}[ T]\\d{2}:\\d{2}:\\d{2}(?:[\\.,]\\d{1,7})?)",
        RegexOptions.Compiled);

    private static readonly string[] LevelTokens = ["Error", "Exception", "Warning", "Information", "Info", "Verbose", "Debug", "Trace"];
    private static readonly string[] ErrorKeywords = ["exception", "failed", "érvénytelen", "ssl_", "login failed"];

    public string CreateSummary(string inputLog)
    {
        var threads = new Dictionary<string, ThreadState>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StringReader(inputLog ?? string.Empty);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var parsed = ParseLine(line);
            var threadId = string.IsNullOrWhiteSpace(parsed.ThreadId) ? "unknown" : parsed.ThreadId;
            if (!threads.TryGetValue(threadId, out var state))
            {
                state = new ThreadState(threadId);
                threads[threadId] = state;
            }

            state.Add(parsed);
        }

        var errorThreads = threads.Values.Where(t => t.IsErrorThread).ToList();
        var errorCount = errorThreads.Sum(t => t.ErrorLineCount);

        var sb = new StringBuilder();
        sb.AppendLine("===== ERROR THREAD KIVONAT =====");
        sb.AppendLine($"Error count: {errorCount}");
        sb.AppendLine($"Unique thread count: {errorThreads.Count}");
        sb.AppendLine();

        if (errorThreads.Count == 0)
        {
            sb.AppendLine("Nem található hibás thread a megadott bemenetben.");
            return sb.ToString();
        }

        for (var i = 0; i < errorThreads.Count; i++)
        {
            var thread = errorThreads[i];
            sb.AppendLine($"===== ERROR THREAD #{i + 1} =====");
            sb.AppendLine($"ThreadId: {thread.ThreadId}");
            sb.AppendLine($"Első hiba időpontja: {FormatTimestamp(thread.FirstErrorTimestamp)}");
            sb.AppendLine($"Utolsó sor időpontja: {FormatTimestamp(thread.LastTimestamp)}");
            sb.AppendLine($"Sorok száma: {thread.Lines.Count}");
            sb.AppendLine();
            sb.AppendLine("Fő hiba:");
            sb.AppendLine(thread.GetErrorType());
            sb.AppendLine();
            sb.AppendLine("Kulcssorok:");
            sb.AppendLine($"- Első Error: {thread.FirstErrorLine ?? "n/a"}");
            sb.AppendLine($"- Utolsó Error: {thread.LastErrorLine ?? "n/a"}");
            sb.AppendLine($"- Exception eleje: {thread.ExceptionLineStart ?? "n/a"}");
            sb.AppendLine();
            sb.AppendLine($"Érintett napló sorok (ThreadId: {thread.ThreadId}):");
            for (var lineIndex = 0; lineIndex < thread.Lines.Count; lineIndex++)
            {
                sb.AppendLine($"{lineIndex + 1}. {thread.Lines[lineIndex].Raw}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static ParsedLine ParseLine(string line)
    {
        DateTimeOffset? ts = null;
        var tsMatch = TimestampRegex.Match(line);
        if (tsMatch.Success)
        {
            var tsText = tsMatch.Groups["ts"].Value.Replace(',', '.');
            if (DateTimeOffset.TryParse(tsText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedTs))
            {
                ts = parsedTs;
            }
            else if (DateTime.TryParse(tsText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDt))
            {
                ts = new DateTimeOffset(parsedDt);
            }
        }

        var threadId = ThreadRegex.Match(line) is { Success: true } threadMatch
            ? threadMatch.Groups["id"].Value
            : null;

        string? level = null;
        foreach (var token in LevelTokens)
        {
            if (line.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                level = token;
                break;
            }
        }

        return new ParsedLine(ts, threadId, level, line);
    }

    private static string FormatTimestamp(DateTimeOffset? timestamp) =>
        timestamp.HasValue ? timestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "n/a";

    private sealed record ParsedLine(DateTimeOffset? Timestamp, string? ThreadId, string? Level, string Raw);

    private sealed class ThreadState(string threadId)
    {
        public string ThreadId { get; } = threadId;
        public List<ParsedLine> Lines { get; } = [];
        public DateTimeOffset? FirstErrorTimestamp { get; private set; }
        public DateTimeOffset? LastTimestamp { get; private set; }
        public int ErrorLineCount { get; private set; }
        public string? FirstErrorLine { get; private set; }
        public string? LastErrorLine { get; private set; }
        public string? ExceptionLineStart { get; private set; }

        public bool IsErrorThread { get; private set; }

        public void Add(ParsedLine line)
        {
            Lines.Add(line);

            if (line.Timestamp.HasValue)
            {
                LastTimestamp = line.Timestamp;
            }

            var isError = IsError(line);
            if (!isError)
            {
                return;
            }

            IsErrorThread = true;
            ErrorLineCount++;
            FirstErrorTimestamp ??= line.Timestamp;
            FirstErrorLine ??= BuildLinePreview(line);
            LastErrorLine = BuildLinePreview(line);

            if (ExceptionLineStart is null && line.Raw.Contains("exception", StringComparison.OrdinalIgnoreCase))
            {
                ExceptionLineStart = line.Raw.Length > 240 ? line.Raw[..240] + "..." : line.Raw;
            }
        }

        public string GetErrorType()
        {
            var joined = string.Join('\n', Lines.Select(x => x.Raw));
            if (joined.Contains("SSL_", StringComparison.OrdinalIgnoreCase))
            {
                return "SSL autentikációs hiba";
            }

            if (joined.Contains("login failed", StringComparison.OrdinalIgnoreCase) && joined.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return "Login failed – user not found";
            }

            if (joined.Contains("érvénytelen", StringComparison.OrdinalIgnoreCase))
            {
                return "Érvénytelen azonosító / paraméter";
            }

            if (joined.Contains("exception", StringComparison.OrdinalIgnoreCase))
            {
                return "Kezelés közbeni exception";
            }

            if (joined.Contains("failed", StringComparison.OrdinalIgnoreCase))
            {
                return "Művelet sikertelen (failed)";
            }

            return "Általános alkalmazáshiba";
        }

        private static bool IsError(ParsedLine line)
        {
            if (line.Level is not null &&
                (line.Level.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
                 line.Level.Equals("Exception", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            var raw = line.Raw;
            return ErrorKeywords.Any(keyword => raw.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private static string BuildLinePreview(ParsedLine line)
        {
            var level = line.Level ?? "raw";
            return $"[{level}] {line.Raw}";
        }
    }
}
