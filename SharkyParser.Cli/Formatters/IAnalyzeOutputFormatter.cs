using SharkyParser.Core;

namespace SharkyParser.Cli.Formatters;

public interface IAnalyzeOutputFormatter
{
    void Write(LogStatistics stats, string parserName, string filePath);
}
