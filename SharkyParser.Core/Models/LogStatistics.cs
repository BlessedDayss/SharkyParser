namespace SharkyParser.Core;

public record LogStatistics(
    int TotalCount,
    int ErrorCount,
    int WarningCount,
    int InfoCount,
    int DebugCount,
    bool IsHealthy,
    string ExtendedData = "",
    SharkyParser.Core.Models.IisLogStatistics? IisStatistics = null
);
