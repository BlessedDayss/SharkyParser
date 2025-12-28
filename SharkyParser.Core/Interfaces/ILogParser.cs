namespace SharkyParser.Core.Interfaces;

public interface ILogParser
{
    LogEntry? ParseLine(string line);
    IEnumerable<LogEntry> ParseFile(string path);
}
