namespace SharkyParser.Core;

/// <summary>
/// Immutable statistics about log entries.
/// </summary>
public record LogStatistics(
    int TotalCount,
    int ErrorCount,
    int WarningCount,
    int InfoCount,
    bool IsHealthy
);
