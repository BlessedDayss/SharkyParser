using System.Text.RegularExpressions;

namespace SharkyParser.Core;

public static class LevelDetector
{
    private static readonly string[] ErrorKeywords = 
        { "error", "exception", "failed", "timeout", "critical", "fatal", "fail" };
    
    private static readonly string[] WarningKeywords = 
        { "warn", "warning", "caution" };

    public static string Detect(string fullLine, string messagePart)
    {
        var lowerLine = fullLine.ToLowerInvariant();
        var lowerMessage = messagePart.Trim().ToLowerInvariant();
        
        if (IsFalsePositive(lowerLine))
            return "INFO";

        if (lowerMessage is "error" or "err" or "erro" || 
            ContainsLevelMarker(lowerLine, "error") || 
            ContainsLevelMarker(lowerLine, "err") || 
            ContainsLevelMarker(lowerLine, "erro"))
            return "ERROR";
        
        if (lowerMessage == "fatal" || ContainsLevelMarker(lowerLine, "fatal"))
            return "FATAL";
        
        if (lowerMessage == "critical" || ContainsLevelMarker(lowerLine, "critical"))
            return "CRITICAL";
        
        if (lowerMessage is "warn" or "warning" || 
            ContainsLevelMarker(lowerLine, "warn") || 
            ContainsLevelMarker(lowerLine, "warning"))
            return "WARN";
        
        if (lowerMessage is "debug" or "dbg" || 
            ContainsLevelMarker(lowerLine, "debug") || 
            ContainsLevelMarker(lowerLine, "dbg"))
            return "DEBUG";
        
        if (lowerMessage == "trace" || ContainsLevelMarker(lowerLine, "trace"))
            return "TRACE";
        
        if (lowerMessage == "info" || ContainsLevelMarker(lowerLine, "info"))
            return "INFO";

        foreach (var keyword in ErrorKeywords)
        {
            if (lowerLine.Contains(keyword))
                return "ERROR";
        }

        foreach (var keyword in WarningKeywords)
        {
            if (lowerLine.Contains(keyword))
                return "WARN";
        }

        return "INFO";
    }

    private static bool ContainsLevelMarker(string line, string level)
    {
        return Regex.IsMatch(line, $@"(\[|\s|^){level}(\]|\s|:|$)", RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(500));
    }

    private static readonly Regex FalsePositiveRegex =
        new(
            @"\b(0\s+(errors?|warnings?)|no\s+errors?)\b",
            RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(50)
        );
    private static bool IsFalsePositive(string lowerLine)
    {
        return FalsePositiveRegex.IsMatch(lowerLine);
    }
}
