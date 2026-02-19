    using System.Globalization;
using System.Text.RegularExpressions;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;

namespace SharkyParser.Core.Parsers;

/// <summary>
/// Parses TeamCity build logs — both plain timestamped lines and
/// <c>##teamcity[...]</c> service messages.
///
/// Supported formats:
///   [HH:mm:ss]           Message text
///   [HH:mm:ss] :         [Step 1/3]  Step description
///   [HH:mm:ss]W:         Warning message
///   [HH:mm:ss]E:         Error message
///   [HH:mm:ss]i:         Info message
///   [2024-01-15 10:30:45]  Full datetime
///   ##teamcity[message text='...' status='ERROR']
///   ##teamcity[buildProblem description='...']
///   ##teamcity[testFailed name='...' message='...' details='...']
///   ##teamcity[buildStatus status='FAILURE' text='...']
/// </summary>
public partial class TeamCityLogParser : BaseLogParser
{
    public override LogType SupportedLogType => LogType.TeamCity;
    public override string ParserName => "TeamCity Logs";
    public override string ParserDescription => "Parses TeamCity CI/CD build logs";

    // ── Regex patterns ────────────────────────────────────────────────────

    // [HH:mm:ss]  or  [yyyy-MM-dd HH:mm:ss]  with optional level marker (W:/E:/i:) or " : " separator
    [GeneratedRegex(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2}|\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2})\](?:(?<marker>[WEi]):)?\s*(?::?\s*)?(?:\[(?<step>[^\]]+)\]\s*)?(?<message>.*)$",
        RegexOptions.Compiled)]
    private static partial Regex TimestampedLineRegex();

    // ##teamcity[messageName key='value' ...]
    [GeneratedRegex(
        @"^##teamcity\[(?<name>\w+)\s+(?<attrs>.+)\]$",
        RegexOptions.Compiled)]
    private static partial Regex ServiceMessageRegex();

    // key='value' pairs inside service messages (handles TeamCity's pipe-based escaping |')
    [GeneratedRegex(
        @"(?<key>\w+)='(?<value>(?:[^'|]|\|.)*?)'",
        RegexOptions.Compiled)]
    private static partial Regex AttributeRegex();

    private DateTime _baseDate = DateTime.Now.Date;

    public TeamCityLogParser(ILogger logger) : base(logger) { }

    // ── File-level override to capture base date ──────────────────────────
    public override IEnumerable<LogEntry> ParseFile(string path)
    {
        _baseDate = File.GetLastWriteTime(path).Date;
        return base.ParseFile(path);
    }

    protected override LogEntry? ParseLineCore(string line)
    {
        // ── Service messages: ##teamcity[...] ─────────────────────────────
        var svcMatch = ServiceMessageRegex().Match(line);
        if (svcMatch.Success)
            return ParseServiceMessage(svcMatch, line);

        // ── Timestamped lines: [HH:mm:ss] ... ────────────────────────────
        var tsMatch = TimestampedLineRegex().Match(line);
        if (tsMatch.Success)
            return ParseTimestampedLine(tsMatch, line);

        // ── Plain text (continuation / unstructured) ──────────────────────
        if (!string.IsNullOrWhiteSpace(line))
        {
            return new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = LevelDetector.Detect(line),
                Message = line.Trim(),
                RawData = line
            };
        }

        return null;
    }

    // ── Timestamped line parsing ──────────────────────────────────────────

    private LogEntry ParseTimestampedLine(Match match, string rawLine)
    {
        var timestamp = ParseTimestamp(match.Groups["timestamp"].Value);
        var marker = match.Groups["marker"].Value;
        var step = match.Groups["step"].Success ? match.Groups["step"].Value : null;
        var message = match.Groups["message"].Value.Trim();

        var level = marker switch
        {
            "E" => LogLevel.Error,
            "W" => LogLevel.Warn,
            "i" => LogLevel.Info,
            _   => LevelDetector.Detect(message)
        };

        var fields = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(step))
            fields["Step"] = step;
        fields["Source"] = "TeamCity";

        return new LogEntry
        {
            Timestamp = timestamp,
            Level = level,
            Message = message,
            RawData = rawLine,
            Fields = fields
        };
    }

    // ── Service message parsing ───────────────────────────────────────────

    private LogEntry ParseServiceMessage(Match match, string rawLine)
    {
        var name = match.Groups["name"].Value;
        var attrsRaw = match.Groups["attrs"].Value;

        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in AttributeRegex().Matches(attrsRaw))
            attrs[m.Groups["key"].Value] = Unescape(m.Groups["value"].Value);

        // Extract timestamp from service message if present
        var timestamp = attrs.TryGetValue("timestamp", out var ts)
            ? ParseServiceTimestamp(ts)
            : DateTime.Now;

        var level = DetermineServiceMessageLevel(name, attrs);
        var message = BuildServiceMessageText(name, attrs);

        var fields = new Dictionary<string, string>
        {
            ["Source"] = "TeamCity",
            ["MessageType"] = name
        };

        if (attrs.TryGetValue("name", out var testName))
            fields["TestName"] = testName;
        if (attrs.TryGetValue("flowId", out var flowId))
            fields["FlowId"] = flowId;
        if (attrs.TryGetValue("details", out var details))
            fields["Details"] = details;
        if (attrs.TryGetValue("errorDetails", out var errorDetails))
            fields["Details"] = errorDetails;
        if (attrs.TryGetValue("identity", out var identity))
            fields["ProblemId"] = identity;
        if (attrs.TryGetValue("duration", out var duration))
            fields["Duration"] = duration;

        return new LogEntry
        {
            Timestamp = timestamp,
            Level = level,
            Message = message,
            RawData = rawLine,
            Fields = fields
        };
    }

    private static string DetermineServiceMessageLevel(string name, Dictionary<string, string> attrs)
    {
        // Explicit status attribute
        if (attrs.TryGetValue("status", out var status))
        {
            return status.ToUpperInvariant() switch
            {
                "ERROR" or "FAILURE" => LogLevel.Error,
                "WARNING"           => LogLevel.Warn,
                _                   => LogLevel.Info
            };
        }

        // Level by message type
        return name.ToLowerInvariant() switch
        {
            "testfailed" or "buildproblem" or "buildfailure" => LogLevel.Error,
            "testignored"                                     => LogLevel.Warn,
            "teststarted" or "testfinished" or "teststdout"   => LogLevel.Debug,
            "compilationstarted" or "compilationfinished"     => LogLevel.Info,
            "progressstart" or "progressfinish"               => LogLevel.Info,
            "blockopened" or "blockclosed"                     => LogLevel.Debug,
            _                                                 => LogLevel.Info
        };
    }

    private static string BuildServiceMessageText(string name, Dictionary<string, string> attrs)
    {
        return name.ToLowerInvariant() switch
        {
            "message"      => attrs.GetValueOrDefault("text", name),
            "buildproblem"  => $"Build Problem: {attrs.GetValueOrDefault("description", "unknown")}",
            "buildstatus"   => $"Build Status: {attrs.GetValueOrDefault("text", attrs.GetValueOrDefault("status", "unknown"))}",
            "teststarted"   => $"Test Started: {attrs.GetValueOrDefault("name", "?")}",
            "testfinished"  => $"Test Finished: {attrs.GetValueOrDefault("name", "?")}",
            "testfailed"    => $"Test Failed: {attrs.GetValueOrDefault("name", "?")} — {attrs.GetValueOrDefault("message", "")}",
            "testignored"   => $"Test Ignored: {attrs.GetValueOrDefault("name", "?")} — {attrs.GetValueOrDefault("message", "")}",
            "blockopened"   => $"▶ {attrs.GetValueOrDefault("name", "")}",
            "blockclosed"   => $"◀ {attrs.GetValueOrDefault("name", "")}",
            "progressstart" => $"Progress: {attrs.GetValueOrDefault("message", "")}",
            "progressfinish"=> $"Progress Done: {attrs.GetValueOrDefault("message", "")}",
            "compilationstarted"  => $"Compilation: {attrs.GetValueOrDefault("compiler", "")}",
            "compilationfinished" => $"Compilation Done: {attrs.GetValueOrDefault("compiler", "")}",
            _ => $"{name}: {string.Join(", ", attrs.Select(kv => $"{kv.Key}={kv.Value}"))}"
        };
    }

    // ── Timestamp helpers ─────────────────────────────────────────────────

    private DateTime ParseTimestamp(string ts)
    {
        // Full datetime: 2024-01-15 10:30:45
        if (DateTime.TryParse(ts, CultureInfo.InvariantCulture, DateTimeStyles.None, out var full))
        {
            return ts.Length > 10 ? full : _baseDate.Add(full.TimeOfDay);
        }

        // Time-only: 10:30:45
        if (TimeSpan.TryParse(ts, out var time))
            return _baseDate.Add(time);

        return DateTime.Now;
    }

    private static DateTime ParseServiceTimestamp(string ts)
    {
        // TeamCity format: yyyy-MM-dd'T'HH:mm:ss.SSSZ
        var formats = new[]
        {
            "yyyy-MM-dd'T'HH:mm:ss.fffzzz",
            "yyyy-MM-dd'T'HH:mm:ss.fff",
            "yyyy-MM-dd'T'HH:mm:sszzz",
            "yyyy-MM-dd'T'HH:mm:ss"
        };

        return DateTime.TryParseExact(ts, formats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.Now;
    }

    private static string Unescape(string value)
    {
        return value
            .Replace("|'", "'")
            .Replace("|n", "\n")
            .Replace("|r", "\r")
            .Replace("||", "|")
            .Replace("|[", "[")
            .Replace("|]", "]")
            .Replace("''", "'");
    }

    // ── Column definitions ────────────────────────────────────────────────

    public override IReadOnlyList<LogColumn> GetColumns()
    {
        return new List<LogColumn>
        {
            new("Timestamp", "Timestamp", "The date and time of the log entry.", true),
            new("Level", "Level", "The severity level.", true),
            new("Message", "Message", "The log message.", true),
            new("Step", "Step", "Build step name.", false),
            new("MessageType", "Type", "TeamCity service message type.", false),
            new("TestName", "Test", "Test name (for test messages).", false)
        };
    }
}
