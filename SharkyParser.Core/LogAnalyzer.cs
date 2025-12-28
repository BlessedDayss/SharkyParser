namespace SharkyParser.Core;

public class LogAnalyzer
{
    public bool HasErrors(IEnumerable<LogEntry> entries)
        => entries.Any(e => e.Level.Contains("ERROR") || e.Level.Contains("FATAL"));
}
