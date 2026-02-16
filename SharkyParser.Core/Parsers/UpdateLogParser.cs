using System.Text.RegularExpressions;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Core.Parsers;

public class UpdateLogParser : BaseLogParser
{
    public override LogType SupportedLogType => LogType.Update;
    public override string ParserName => "Update Logs";
    public override string ParserDescription => "Parses Update Logs";
    
    private static readonly Regex UpdatePattern = new(
        @"(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})?\s*\[(?<component>[^\]]+)\]\s*(?<action>\w+)\s+(?<target>[^:]+):\s*(?<status>\w+)",
        RegexOptions.Compiled);
    
    public UpdateLogParser(IAppLogger logger) : base(logger) { }

    protected override LogEntry? ParseLineCore(string line)
    {
        var match = UpdatePattern.Match(line);
        if (match.Success)
        {
            var action = match.Groups["action"].Value;
            var status = match.Groups["status"].Value;
            var level = GetUpdateLevel(action, status);

            return new LogEntry
            {
                Timestamp = ParseTimestamp(match.Groups["timestamp"].Value),
                Level = level,
                Source = match.Groups["component"].Value,
                Message = $"{action} {match.Groups["target"].Value}: {status}",
                RawData = line
            };
        }

        return null;
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