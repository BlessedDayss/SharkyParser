using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;

namespace SharkyParser.Cli.Formatters;

public interface IParseOutputFormatter
{
    void Write(IReadOnlyList<LogEntry> logs, ILogParser parser, int totalEntries);
}
