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

    private string GetUpdateLevel(string action, string status)
    {
        if (status.ToLower() is "failed" or "error")
            return "ERROR";
        
        if (status.ToLower() is "warning")
            return "WARN";

        return action.ToLower() switch
        {
            "installing" or "downloading" => "INFO",
            "completed" or "success" => "INFO",
            _ => "INFO"
        };
    }

    private DateTime ParseTimestamp(string timestamp)
    {
        return DateTime.TryParse(timestamp, out var dt) ? dt : DateTime.Now;
    }

    private LogEntry? FallbackParse(string line)
    {
        var level = LevelDetector.Detect(line);
        return new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = line.Trim(),
            RawData = line,
            Source = "Update"
        };
    }
}