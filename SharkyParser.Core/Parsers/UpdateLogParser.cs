using System.Text.RegularExpressions;
using System.Globalization;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;

namespace SharkyParser.Core.Parsers;

public partial class UpdateLogParser : BaseLogParser
{
    public override LogType SupportedLogType => LogType.Update;
    public override string ParserName => "Update Logs";
    public override string ParserDescription => "Parses Update Logs";

    [GeneratedRegex(@"^(?:(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})?\s*\[(?<component>[^\]]+)\]\s*(?<action>\w+)\s+(?<target>[^:]+):\s*(?<status>\w+)|(?<timestamp>\d{2}:\d{2}:\d{2}(?:\.\d+)?)\s+(?<message>.+))$", RegexOptions.Compiled)]
    private static partial Regex UpdatePatternRegex();

    public UpdateLogParser(ILogger logger) : base(logger) { }

    public override IEnumerable<LogEntry> ParseFile(string path)
    {
        var baseDate = ExtractDateFromFileName(path) ?? File.GetLastWriteTime(path).Date;
        var lineNumber = 0;

        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var entry = ParseLineWithDate(line, baseDate);
            if (entry != null)
            {
                yield return entry with
                {
                    FilePath = path,
                    LineNumber = lineNumber
                };
            }
        }
    }

    private LogEntry? ParseLineWithDate(string line, DateTime baseDate)
    {
        var match = UpdatePatternRegex().Match(line);
        if (!match.Success)
            return null;

        var timestampStr = match.Groups["timestamp"].Value;
        var timestamp = ParseTimestamp(timestampStr, baseDate);

        if (match.Groups["component"].Success)
        {
            var component = match.Groups["component"].Value;
            var action = match.Groups["action"].Value;
            var status = match.Groups["status"].Value;
            var target = match.Groups["target"].Value;

            return new LogEntry
            {
                Timestamp = timestamp,
                Level = GetUpdateLevel(action, status),
                Message = $"{action} {target}: {status}",
                RawData = line,
                Fields = new Dictionary<string, string> { ["Component"] = component }
            };
        }
        else
        {
            var message = match.Groups["message"].Value;
            return new LogEntry
            {
                Timestamp = timestamp,
                Level = LevelDetector.Detect(message),
                Message = message,
                RawData = line,
                Fields = new Dictionary<string, string> { ["Source"] = "Update" }
            };
        }
    }

    protected override LogEntry? ParseLineCore(string line) => ParseLineWithDate(line, DateTime.Now.Date);

    public override IReadOnlyList<LogColumn> GetColumns()
    {
        return new List<LogColumn>
        {
            new("Timestamp", "Timestamp", "The date and time of the event.", true),
            new("Level", "Level", "The severity level.", true),
            new("Message", "Message", "Full summary message.", true)
        };
    }

    private static string GetUpdateLevel(string action, string status)
    {
        if (status.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("error", StringComparison.OrdinalIgnoreCase))
            return LogLevel.Error;

        if (status.Equals("warning", StringComparison.OrdinalIgnoreCase))
            return LogLevel.Warn;

        return action.ToLowerInvariant() switch
        {
            "installing" or "downloading" => LogLevel.Info,
            "completed" or "success" => LogLevel.Info,
            _ => LogLevel.Info
        };
    }

    private static DateTime ParseTimestamp(string timestamp, DateTime baseDate)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return DateTime.Now;

        if (DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            if (timestamp.Length <= 13)
            {
                return baseDate.Add(dt.TimeOfDay);
            }
            return dt;
        }

        return DateTime.Now;
    }

    private DateTime? ExtractDateFromFileName(string path)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var parts = fileName.Split('_');

            for (int i = 0; i < parts.Length - 2; i++)
            {
                if (parts[i].Length == 4 && int.TryParse(parts[i], out var year) &&
                    parts[i+1].Length == 2 && int.TryParse(parts[i+1], out var month) &&
                    parts[i+2].Length == 2 && int.TryParse(parts[i+2], out var day))
                {
                    return new DateTime(year, month, day);
                }
            }
        }
        catch { /* Ignore */ }

        return null;
    }
}
