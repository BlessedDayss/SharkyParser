using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Core.Parsers;

public abstract class BaseLogParser : ILogParser
{
    public abstract LogType SupportedLogType { get; }
    public abstract string ParserName { get; }
    public abstract string ParserDescription { get; }

    private readonly IAppLogger _logger;
    
    
    protected BaseLogParser(IAppLogger logger)
    {
        _logger = logger;
    }

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
            _logger.LogError($"Failed to parse line: {line}. Error: {ex.Message}");
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

    private LogEntry CreateErrorEntry(string rawLine, string error)
    {
        return new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = "ERROR",
            Message = $"Parse error: {error}",
            RawData = rawLine,
            Source = ParserName
        };
    }
}