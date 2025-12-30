using System.Globalization;
using System.Text.RegularExpressions;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Core;

public partial class LogParser : ILogParser
{
    // Flexible timestamp patterns
    private static readonly Regex TimestampRegex = MyTimestampRegex();
    
    // Error keywords for automatic level detection
    private static readonly string[] ErrorKeywords = 
    {
        "error", "exception", "failed", "timeout", "critical", "fatal", "fail"
    };
    
    private static readonly string[] WarningKeywords =
    {
        "warn", "warning", "caution"
    };

    public LogEntry? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var match = TimestampRegex.Match(line);
        
        if (match.Success)
        {
            var timestampText = match.Value;
            var messagePart = line[match.Length..].Trim();
            
            if (TryParseTimestamp(timestampText, out var timestamp))
            {
                var level = DetectLevel(line, messagePart);
                var source = ExtractSource(ref messagePart);
                
                return new LogEntry
                {
                    Timestamp = timestamp,
                    Level = level,
                    Source = source,
                    Message = messagePart,
                    RawData = line
                };
            }
        }
        
        // Line without timestamp - could be continuation or standalone message
        var standaloneLevel = DetectLevel(line, line);
        return new LogEntry
        {
            Timestamp = DateTime.MinValue,
            Level = standaloneLevel,
            Message = line.Trim(),
            RawData = line
        };
    }

    public IEnumerable<LogEntry> ParseFile(string path)
    {
        LogEntry? lastEntry = null;
        var lineNumber = 0;

        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var match = TimestampRegex.Match(line);
            
            if (match.Success && TryParseTimestamp(match.Value, out var timestamp))
            {
                // If we have a previous entry, yield it
                if (lastEntry != null)
                {
                    yield return lastEntry;
                }
                
                var messagePart = line[match.Length..].Trim();
                var level = DetectLevel(line, messagePart);
                var source = ExtractSource(ref messagePart);
                
                lastEntry = new LogEntry
                {
                    Timestamp = timestamp,
                    Level = level,
                    Source = source,
                    Message = messagePart,
                    RawData = line,
                    FilePath = path,
                    LineNumber = lineNumber
                };
            }
            else
            {
                // Line without timestamp - append as stack trace or create standalone entry
                if (lastEntry != null)
                {
                    AppendStackTrace(lastEntry, line);
                }
                else
                {
                    // Standalone line without timestamp
                    var level = DetectLevel(line, line);
                    lastEntry = new LogEntry
                    {
                        Timestamp = DateTime.MinValue,
                        Level = level,
                        Message = line.Trim(),
                        RawData = line,
                        FilePath = path,
                        LineNumber = lineNumber
                    };
                }
            }
        }

        // Don't forget the last entry
        if (lastEntry != null)
        {
            yield return lastEntry;
        }
    }

    private static bool TryParseTimestamp(string text, out DateTime result)
    {
        // Try multiple formats
        string[] formats =
        {
            "yyyy-MM-dd HH:mm:ss,fff",
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
            "dd-MM-yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "HH:mm:ss,fff",
            "HH:mm:ss.fff",
            "HH:mm:ss"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(text.Trim(), format, CultureInfo.InvariantCulture, 
                    DateTimeStyles.None, out result))
            {
                return true;
            }
        }

        // Try generic parse as last resort
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    private static string DetectLevel(string fullLine, string messagePart)
    {
        var lowerLine = fullLine.ToLowerInvariant();
        
        // Check for false positives like "0 errors"
        if (IsZeroErrorOrWarningFalsePositive(lowerLine))
            return "INFO";

        // Check for explicit level markers first
        if (ContainsLevelMarker(lowerLine, "error") || ContainsLevelMarker(lowerLine, "err") || ContainsLevelMarker(lowerLine, "erro"))
            return "ERROR";
        if (ContainsLevelMarker(lowerLine, "fatal"))
            return "FATAL";
        if (ContainsLevelMarker(lowerLine, "critical"))
            return "CRITICAL";
        if (ContainsLevelMarker(lowerLine, "warn") || ContainsLevelMarker(lowerLine, "warning"))
            return "WARN";
        if (ContainsLevelMarker(lowerLine, "debug") || ContainsLevelMarker(lowerLine, "dbg"))
            return "DEBUG";
        if (ContainsLevelMarker(lowerLine, "trace"))
            return "TRACE";
        if (ContainsLevelMarker(lowerLine, "info"))
            return "INFO";

        // Check for error keywords in content
        foreach (var keyword in ErrorKeywords)
        {
            if (lowerLine.Contains(keyword))
                return "ERROR";
        }

        // Check for warning keywords
        foreach (var keyword in WarningKeywords)
        {
            if (lowerLine.Contains(keyword))
                return "WARN";
        }

        return "INFO";
    }

    private static bool ContainsLevelMarker(string line, string level)
    {
        // Look for level as a standalone word or in brackets
        return Regex.IsMatch(line, $@"(\[|\s|^){level}(\]|\s|:|$)", RegexOptions.IgnoreCase);
    }

    private static bool IsZeroErrorOrWarningFalsePositive(string lowerLine)
    {
        return lowerLine.Contains("0 error") ||
               lowerLine.Contains("0 errors") ||
               lowerLine.Contains("0 warning") ||
               lowerLine.Contains("0 warnings") ||
               lowerLine.Contains("no error") ||
               lowerLine.Contains("no errors");
    }

    private static string ExtractSource(ref string messagePart)
    {
        var levelPattern = @"^(INFO|ERROR|WARN|WARNING|DEBUG|TRACE|FATAL|CRITICAL|ERR|ERRO)\s+";
        var levelMatch = Regex.Match(messagePart, levelPattern, RegexOptions.IgnoreCase);
        if (levelMatch.Success)
        {
            messagePart = messagePart[levelMatch.Length..];
        }

        var sourceMatch = Regex.Match(messagePart, @"^\[([^\]]+)\]\s*");
        if (sourceMatch.Success)
        {
            var source = sourceMatch.Groups[1].Value;
            messagePart = messagePart[sourceMatch.Length..];
            return source;
        }

        return string.Empty;
    }

    private static void AppendStackTrace(LogEntry entry, string line)
    {
        if (string.IsNullOrEmpty(entry.StackTrace))
        {
            entry.StackTrace = line;
        }
        else
        {
            entry.StackTrace += Environment.NewLine + line;
        }
        
        // If stack trace contains error keywords, upgrade entry level
        var lowerLine = line.ToLowerInvariant();
        if (!IsZeroErrorOrWarningFalsePositive(lowerLine))
        {
            foreach (var keyword in ErrorKeywords)
            {
                if (lowerLine.Contains(keyword) && entry.Level == "INFO")
                {
                    // Can't modify record property, but StackTrace is set
                    break;
                }
            }
        }
    }

    // Regex patterns for timestamp detection
    // Matches: 2025-12-30 14:30:45.123, 2025-12-30 14:30:45, 14:30:45, etc.
    [GeneratedRegex(@"^(\d{4}[-/]\d{2}[-/]\d{2}\s+)?\d{1,2}:\d{2}:\d{2}([,.:]\d{1,3})?", RegexOptions.Compiled)]
    private static partial Regex MyTimestampRegex();
}
