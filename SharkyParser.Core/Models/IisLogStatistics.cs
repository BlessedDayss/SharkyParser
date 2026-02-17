namespace SharkyParser.Core.Models;

public record IisLogStatistics(
    Dictionary<DateTime, int> RequestsPerMinute,
    Dictionary<string, int> TopIps,
    List<SlowRequestStats> SlowestRequests,
    Dictionary<string, int> ResponseTimeDistribution
);

public record SlowRequestStats(
    string Url,
    string? Method,
    int DurationMs,
    DateTime Timestamp,
    int StatusCode
);
