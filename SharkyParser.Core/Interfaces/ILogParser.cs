using SharkyParser.Core.Enums;

namespace SharkyParser.Core.Interfaces;

public interface ILogParser
{
    
    LogType SupportedLogType { get; }
    string ParserName { get; }
    string ParserDescription { get; }
    LogEntry? ParseLine(string line);
    IEnumerable<LogEntry> ParseFile(string path);
    Task<IEnumerable<LogEntry>> ParseFileAsync(string path);
}
