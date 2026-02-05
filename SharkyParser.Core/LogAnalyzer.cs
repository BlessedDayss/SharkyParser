using SharkyParser.Core.Interfaces;

namespace SharkyParser.Core;

public class LogAnalyzer : ILogAnalyzer
{
    private static readonly string[] ErrorLevels = ["ERROR", "FATAL", "CRITICAL"];
    private static readonly string[] WarningLevels = ["WARN", "WARNING"];

    public bool HasErrors(IEnumerable<LogEntry> entries)
        => entries.Any(e => ErrorLevels.Any(level => 
            e.Level.Contains(level, StringComparison.OrdinalIgnoreCase)));

    public bool HasWarnings(IEnumerable<LogEntry> entries)
        => entries.Any(e => WarningLevels.Any(level => 
            e.Level.Contains(level, StringComparison.OrdinalIgnoreCase)));

    public LogStatistics GetStatistics(IEnumerable<LogEntry> entries)
    {
        var list = entries.ToList();
        
        var errorCount = list.Count(e => ErrorLevels.Any(level => 
            e.Level.Contains(level, StringComparison.OrdinalIgnoreCase)));
        
        var warningCount = list.Count(e => WarningLevels.Any(level => 
            e.Level.Contains(level, StringComparison.OrdinalIgnoreCase)));
        
        var infoCount = list.Count(e => 
            e.Level.Contains("INFO", StringComparison.OrdinalIgnoreCase));
        
        var debugCount = list.Count(e => 
            e.Level.Contains("DEBUG", StringComparison.OrdinalIgnoreCase) ||
            e.Level.Contains("TRACE", StringComparison.OrdinalIgnoreCase));

        return new LogStatistics(
            TotalCount: list.Count,
            ErrorCount: errorCount,
            WarningCount: warningCount,
            InfoCount: infoCount,
            DebugCount: debugCount,
            IsHealthy: errorCount == 0
        );
    }
}
