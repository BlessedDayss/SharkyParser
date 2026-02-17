using SharkyParser.Core;

namespace SharkyParser.Cli.Formatters;

/// <summary>
/// Renders analysis results as a single pipe-delimited line for programmatic consumers.
/// </summary>
public class EmbeddedAnalyzeFormatter : IAnalyzeOutputFormatter
{
    public void Write(LogStatistics stats, string parserName, string filePath)
    {
        Console.WriteLine(
            $"ANALYSIS|{stats.TotalCount}|{stats.ErrorCount}|{stats.WarningCount}" +
            $"|{stats.InfoCount}|{stats.DebugCount}" +
            $"|{(stats.IsHealthy ? "HEALTHY" : "UNHEALTHY")}" +
            $"|{stats.ExtendedData}");
    }
}
