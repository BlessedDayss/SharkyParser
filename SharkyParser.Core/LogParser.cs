using SharkyParser.Core.Interfaces;

namespace SharkyParser.Core;

public class LogParser : ILogParser
{
    public LogEntry? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        if (TimestampParser.TryMatch(line, out var match) && 
            TimestampParser.TryParse(match.Value, out var timestamp))
        {
            var messagePart = line[match.Length..].Trim();
            var level = LevelDetector.Detect(line, messagePart);
            var source = SourceExtractor.Extract(ref messagePart);
            
            return new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Source = source,
                Message = messagePart,
                RawData = line
            };
        }
        
        var standaloneLevel = LevelDetector.Detect(line, line);
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

            if (TimestampParser.TryMatch(line, out var match) && 
                TimestampParser.TryParse(match.Value, out var timestamp))
            {
                if (lastEntry != null)
                    yield return lastEntry;
                
                var messagePart = line[match.Length..].Trim();
                var level = LevelDetector.Detect(line, messagePart);
                var source = SourceExtractor.Extract(ref messagePart);
                
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
                var trimmedLine = line.TrimStart();
                var isStackTrace = line.StartsWith(" ") || 
                                   line.StartsWith("\t") || 
                                   trimmedLine.StartsWith("at ") ||
                                   trimmedLine.StartsWith("---") ||
                                   trimmedLine.StartsWith("^");

                if (lastEntry != null && isStackTrace)
                {
                    AppendStackTrace(lastEntry, line);
                }
                else
                {
                    if (lastEntry != null)
                        yield return lastEntry;
                    
                    var level = LevelDetector.Detect(line, line);
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

        if (lastEntry != null)
            yield return lastEntry;
    }

    private static void AppendStackTrace(LogEntry entry, string line)
    {
        entry.StackTrace = string.IsNullOrEmpty(entry.StackTrace) 
            ? line 
            : entry.StackTrace + Environment.NewLine + line;
    }
}