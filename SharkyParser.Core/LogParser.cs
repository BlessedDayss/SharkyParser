using System.Globalization;

namespace SharkyParser.Core;

public class LogParser
{
    public LogEntry? ParseLine(string line)
    {


        var parts = line.Split(' ', 5);
        if (parts.Length < 5)
            return null;

        var timestampText = parts[0] + " " + parts[1];

        return new LogEntry
        {
            Timestamp = DateTime.ParseExact(timestampText, "yyyy-MM-dd HH:mm:ss,fff", CultureInfo.InvariantCulture),
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
