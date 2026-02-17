using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;

namespace SharkyParser.Core.Parsers;

public abstract class BaseLogParser(IAppLogger logger) : ILogParser
{
    public abstract LogType SupportedLogType { get; }
    public abstract string ParserName { get; }
    public abstract string ParserDescription { get; }

    protected readonly IAppLogger Logger = logger;

    public virtual LogEntry? ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        try
        {
            return ParseLineCore(line);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to parse line: {line}. Error: {ex.Message}");
            return CreateErrorEntry(line, ex.Message);
        }
    }
    
    protected abstract LogEntry? ParseLineCore(string line);

    public virtual IEnumerable<LogEntry> ParseFile(string path)
    {
        var entries = new List<LogEntry>();
        var lineNumber = 0;

        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            var entry = ParseLine(line);
            if (entry != null)
            {
                entry = entry with
                {
                    FilePath = path, 
                    LineNumber = lineNumber
                };
                entries.Add(entry);
            }
        }
        return entries;
    }

    public virtual async Task<IEnumerable<LogEntry>> ParseFileAsync(string path)
    {
        return await Task.Run(() => ParseFile(path));
    }

    public virtual IReadOnlyList<LogColumn> GetColumns()
    {
        return new List<LogColumn>
        {
            new("Timestamp", "Timestamp", "The date and time of the log entry.", true),
            new("Level", "Level", "The severity level of the log.", true),
            new("Message", "Message", "The log message.", true)
        };
    }

    private LogEntry CreateErrorEntry(string rawLine, string error)
    {
        return new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = "ERROR",
            Message = $"Parse error: {error}",
            RawData = rawLine
        };
    }
}
