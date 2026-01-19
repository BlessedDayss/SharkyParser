using System.Text.RegularExpressions;

namespace SharkyParser.Core;

public static partial class LevelDetector
{
    private const int RegexTimeoutMs = 100;

    [GeneratedRegex(@"\b(error|err|erro)\b", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMs)]
    private static partial Regex ErrorMarkerRegex();

    [GeneratedRegex(@"\b(exception|failed?|timeout)\b", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMs)]
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

    [GeneratedRegex(@"(0\s+(error|warning)|no\s+error)", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: RegexTimeoutMs)]
    private static partial Regex FalsePositiveRegex();

    public static string Detect(string fullLine)
    {
        if (string.IsNullOrWhiteSpace(fullLine))
            return LogLevel.Info;

        try
        {
            if (IsFalsePositive(fullLine))
                return LogLevel.Info;

            if (ErrorMarkerRegex().IsMatch(fullLine)) return LogLevel.Error;
            if (WarnMarkerRegex().IsMatch(fullLine)) return LogLevel.Warn;
            if (DebugMarkerRegex().IsMatch(fullLine)) return LogLevel.Debug;
            if (TraceMarkerRegex().IsMatch(fullLine)) return LogLevel.Trace;
            if (InfoMarkerRegex().IsMatch(fullLine)) return LogLevel.Info;
            if (ErrorKeywordRegex().IsMatch(fullLine)) return LogLevel.Error;
            return WarnKeywordRegex().IsMatch(fullLine) ? LogLevel.Warn : LogLevel.Info;
        }
        catch (RegexMatchTimeoutException)
        {
            //TODO: ADD LOGGING
            return LogLevel.Info;
        }
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