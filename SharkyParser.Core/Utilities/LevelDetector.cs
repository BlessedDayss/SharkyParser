using System.Text.RegularExpressions;

namespace SharkyParser.Core;

public static partial class LevelDetector
{
    private const int RegexTimeoutMs = 500;

    [GeneratedRegex(@"\b(error|err|erro|fatal|critical)\b", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMs)]
    private static partial Regex ErrorMarkerRegex();

    [GeneratedRegex(@"\b(exception|failed?|timeout|crash)\b", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMs)]
    private static partial Regex ErrorKeywordRegex();

    [GeneratedRegex(@"\b(warn|warning)\b", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMs)]
    private static partial Regex WarnMarkerRegex();

    [GeneratedRegex(@"\b(caution)\b", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMs)]
    private static partial Regex WarnKeywordRegex();

    [GeneratedRegex(@"\b(info)\b", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMs)]
    private static partial Regex InfoMarkerRegex();

    [GeneratedRegex(@"\b(debug|dbg)\b", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMs)]
    private static partial Regex DebugMarkerRegex();

    [GeneratedRegex(@"\b(trace)\b", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMs)]
    private static partial Regex TraceMarkerRegex();

    [GeneratedRegex(@"(?:0\s+(?:error|warning)|no\s+error|.*\.sql)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMs)]
    private static partial Regex FalsePositiveRegex();

    public static string Detect(string fullLine)
    {
        if (string.IsNullOrWhiteSpace(fullLine))
            return LogLevel.Info;

        try
        {
            var line = fullLine.Trim();

            if (IsFalsePositive(line))
                return LogLevel.Info;

            // Check prefixes first (fast path)
            var prefixLevel = CheckPrefixLevel(line);
            if (prefixLevel != null)
                return prefixLevel;

            // Check regex patterns (slower path)
            var regexLevel = CheckRegexPatterns(line);
            if (regexLevel != null)
                return regexLevel;

            return LogLevel.Info;
        }
        catch (RegexMatchTimeoutException)
        {
            return LogLevel.Info;
        }
    }

    private static string? CheckPrefixLevel(string line)
    {
        if (StartsWithAny(line, "ERROR", "ERR ", "ERRO ", "FATAL", "CRITICAL")) return LogLevel.Error;
        if (StartsWithAny(line, "WARN", "WARNING")) return LogLevel.Warn;
        if (StartsWithAny(line, "DEBUG", "DBG ")) return LogLevel.Debug;
        if (StartsWithAny(line, "TRACE")) return LogLevel.Trace;
        if (StartsWithAny(line, "INFO")) return LogLevel.Info;

        return null;
    }

    private static string? CheckRegexPatterns(string line)
    {
        if (ErrorMarkerRegex().IsMatch(line)) return LogLevel.Error;
        if (WarnMarkerRegex().IsMatch(line)) return LogLevel.Warn;
        if (DebugMarkerRegex().IsMatch(line)) return LogLevel.Debug;
        if (TraceMarkerRegex().IsMatch(line)) return LogLevel.Trace;
        if (InfoMarkerRegex().IsMatch(line)) return LogLevel.Info;
        if (ErrorKeywordRegex().IsMatch(line)) return LogLevel.Error;
        if (WarnKeywordRegex().IsMatch(line)) return LogLevel.Warn;

        return null;
    }

    private static bool StartsWithAny(string text, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool IsFalsePositive(string line)
    {
        try
        {
            return FalsePositiveRegex().IsMatch(line);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}