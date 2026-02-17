using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;

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


    public LogStatistics GetStatistics(IEnumerable<LogEntry> entries, LogType type)
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
            IsHealthy: errorCount == 0,
            ExtendedData: "",
            IisStatistics: type == LogType.IIS ? GetIisStatistics(list) : null
        );
    }

    private static IisLogStatistics GetIisStatistics(List<LogEntry> entries)
    {
        // 1. Preparation
        var iisEntries = entries.Select(e => new
            {
                Entry = e,
                Duration = int.TryParse(e.Fields.GetValueOrDefault("time-taken"), out var d) ? d : 0,
                ClientIp = e.Fields.GetValueOrDefault("c-ip", "Unknown"),
                Method = e.Fields.GetValueOrDefault("cs-method", "GET"),
                Url = e.Fields.GetValueOrDefault("cs-uri-stem", "/"),
                StatusCode = int.TryParse(e.Fields.GetValueOrDefault("sc-status", "200"), out var s) ? s : 200
            })
            .ToList();

        // 2. Requests Per Minute
        var requestsPerMinute = iisEntries
            .GroupBy(x => new DateTime(x.Entry.Timestamp.Year, x.Entry.Timestamp.Month, x.Entry.Timestamp.Day, x.Entry.Timestamp.Hour, x.Entry.Timestamp.Minute, 0))
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        // 3. Top IPs
        var topIps = iisEntries
            .GroupBy(x => x.ClientIp)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        // 4. Slowest Requests
        var slowestRequests = iisEntries
            .OrderByDescending(x => x.Duration)
            .Take(10)
            .Select(x => new SlowRequestStats(
                x.Url,
                x.Method,
                x.Duration,
                x.Entry.Timestamp,
                x.StatusCode
            ))
            .ToList();

        // 5. Response Time Distribution
        var distribution = new Dictionary<string, int>
        {
            { "< 200ms", 0 },
            { "200-500ms", 0 },
            { "500-1000ms", 0 },
            { "1000-2000ms", 0 },
            { "2000-5000ms", 0 },
            { "> 5000ms", 0 }
        };

        foreach (var item in iisEntries)
        {
            if (item.Duration < 200) distribution["< 200ms"]++;
            else if (item.Duration < 500) distribution["200-500ms"]++;
            else if (item.Duration < 1000) distribution["500-1000ms"]++;
            else if (item.Duration < 2000) distribution["1000-2000ms"]++;
            else if (item.Duration < 5000) distribution["2000-5000ms"]++;
            else distribution["> 5000ms"]++;
        }

        return new IisLogStatistics(
            requestsPerMinute,
            topIps,
            slowestRequests,
            distribution
        );
    }
}
