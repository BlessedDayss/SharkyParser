namespace SharkyParser.Api.DTOs;

public record IisLogStatisticsDto(
    Dictionary<string, int> RequestsPerMinute,
    Dictionary<string, int> TopIps,
    List<SlowRequestStatsDto> SlowestRequests,
    Dictionary<string, int> ResponseTimeDistribution
);

public record SlowRequestStatsDto(
    string Url,
    string? Method,
    int DurationMs,
    DateTime Timestamp,
    int StatusCode
);
