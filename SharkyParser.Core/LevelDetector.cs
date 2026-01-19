namespace SharkyParser.Core;

public static class LevelDetector
{
    public static string Detect(string fullLine)
    {
        var line = fullLine.ToLowerInvariant();
        if (IsFalsePositive(line)) return "INFO";

        if (HasMarker(line, "error", "err", "erro")) return "ERROR";
        if (HasMarker(line, "warn", "warning")) return "WARN";
        if (HasMarker(line, "info")) return "INFO";
        if (HasMarker(line, "debug", "dbg")) return "DEBUG";
        if (HasMarker(line, "trace")) return "TRACE";
        if (ContainsAny(line, "error", "exception", "failed", "fail", "timeout")) return "ERROR";
        if (ContainsAny(line, "warn", "caution")) return "WARN";

        return "INFO";
    }

    private static bool HasMarker(string line, params string[] markers)
    {
        foreach (var m in markers) if (ContainsLevelMarker(line, m)) return true;
        return false;
    }

    private static bool ContainsAny(string line, params string[] keywords)
    {
        foreach (var k in keywords) if (line.Contains(k)) return true;
        return false;
    }

    private static bool ContainsLevelMarker(string line, string level)
    {
        int index = 0;
        int levelLen = level.Length;
        int lineLen = line.Length;

        while ((index = line.IndexOf(level, index, StringComparison.Ordinal)) != -1)
        {
            bool leftOk = index == 0 || char.IsWhiteSpace(line[index - 1]) || line[index - 1] == '[' || line[index - 1] == '<';
            
            bool rightOk = index + levelLen == lineLen || 
                           char.IsWhiteSpace(line[index + levelLen]) || 
                           line[index + levelLen] == ']' || 
                           line[index + levelLen] == '>' || 
                           line[index + levelLen] == ':';

            if (leftOk && rightOk) return true;
            index++;
        }
        return false;
    }

    private static bool IsFalsePositive(string line)
    {
        return line.Contains("0 error") || line.Contains("0 warning") || line.Contains("no error");
    }
}
