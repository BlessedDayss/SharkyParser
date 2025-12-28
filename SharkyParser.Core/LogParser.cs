using System.Globalization;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Core;

public class LogParser : ILogParser
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss,fff";

    public LogEntry? ParseLine(string line)
    {
        var parts = line.Split(' ', 5);
        if (parts.Length < 5)
            return null;

        var timestampText = parts[0] + " " + parts[1];

        if (!DateTime.TryParseExact(timestampText, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
            return null;

        return new LogEntry
        {
            Timestamp = timestamp,
            Level = parts[2],
            Source = parts[3].Trim('[', ']'),
            Message = parts[4]
        };
    }

    public IEnumerable<LogEntry> ParseFile(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            var entry = ParseLine(line);
            if (entry != null)
                yield return entry;
        }
    }
}
