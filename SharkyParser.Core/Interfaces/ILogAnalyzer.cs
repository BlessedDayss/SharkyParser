using SharkyParser.Core.Enums;
using SharkyParser.Core.Models;

namespace SharkyParser.Core.Interfaces;

public interface ILogAnalyzer
{
    bool HasErrors(IEnumerable<LogEntry> entries);
    bool HasWarnings(IEnumerable<LogEntry> entries);
    LogStatistics GetStatistics(IEnumerable<LogEntry> entries, LogType type);
}
