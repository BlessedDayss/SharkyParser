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

    [GeneratedRegex(@"(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})?\s*\[(?<component>[^\]]+)\]\s*(?<action>\w+)\s+(?<target>[^:]+):\s*(?<status>\w+)", RegexOptions.Compiled)]
    private static partial Regex UpdatePatternRegex();

    public UpdateLogParser(IAppLogger logger) : base(logger) { }

    protected override LogEntry? ParseLineCore(string line)
    {
        var match = UpdatePatternRegex().Match(line);
        if (match.Success)
        {
            var action = match.Groups["action"].Value;
            var status = match.Groups["status"].Value;
            var level = GetUpdateLevel(action, status);

            var entry = new LogEntry
            {
                Timestamp = ParseTimestamp(match.Groups["timestamp"].Value),
                Level = level,
                Message = $"{action} {match.Groups["target"].Value}: {status}",
                RawData = line
            };

            entry.Fields["Component"] = match.Groups["component"].Value;
            entry.Fields["Action"] = action;
            entry.Fields["Target"] = match.Groups["target"].Value;
            entry.Fields["Status"] = status;

            return entry;
        }

        return null;
    }

    public override IReadOnlyList<LogColumn> GetColumns()
    {
        return new List<LogColumn>
        {
            new("Timestamp", "Timestamp", "The date and time of the event.", true),
            new("Level", "Level", "The severity level.", true),
            new("Component", "Component", "The component reporting the event.", false),
            new("Action", "Action", "The action performed.", false),
            new("Target", "Target", "The target of the action.", false),
            new("Status", "Status", "The result of the action.", false),
            new("Message", "Message", "Full summary message.", true)
        };
    }

    private static string GetUpdateLevel(string action, string status)
    {
        if (status.Equals("failed", StringComparison.OrdinalIgnoreCase) || 
            status.Equals("error", StringComparison.OrdinalIgnoreCase))
            return "ERROR";
        
        if (status.Equals("warning", StringComparison.OrdinalIgnoreCase))
            return "WARN";

        return action.ToLowerInvariant() switch
        {
            "installing" or "downloading" => "INFO",
            "completed" or "success" => "INFO",
            _ => "INFO"
        };
    }

    private static DateTime ParseTimestamp(string timestamp)
    {
        return DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) 
            ? dt 
            : DateTime.Now;
    }
}
