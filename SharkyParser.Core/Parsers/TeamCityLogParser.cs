using System.Globalization;
using System.Text.RegularExpressions;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;

namespace SharkyParser.Core.Parsers;

/// <summary>
/// Parses TeamCity build logs - both plain timestamped lines and
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
public partial class TeamCityLogParser : BaseLogParser, ITeamCityBlockConfigurableParser
{
    public override LogType SupportedLogType => LogType.TeamCity;
    public override string ParserName => "TeamCity Logs";
    public override string ParserDescription => "Parses TeamCity CI/CD build logs";

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

    // Extracts TeamCity line tail for block-scope filtering.
    [GeneratedRegex(
        @"^\[(?<timestamp>\d{2}:\d{2}:\d{2}|\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2})\](?:(?<marker>[WEi]):)?(?<tail>.*)$",
        RegexOptions.Compiled)]
    private static partial Regex ContextLineRegex();

    private DateTime _baseDate = DateTime.Now.Date;
    private HashSet<string> _selectedBlocks = new(StringComparer.OrdinalIgnoreCase);

    public TeamCityLogParser(ILogger logger) : base(logger) { }

    public void ConfigureBlocks(IEnumerable<string>? blocks)
    {
        _selectedBlocks = blocks?
            .Where(static b => !string.IsNullOrWhiteSpace(b))
            .Select(static b => b.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    // File-level override to capture base date
    public override IEnumerable<LogEntry> ParseFile(string path)
    {
        _baseDate = File.GetLastWriteTime(path).Date;

        if (_selectedBlocks.Count == 0)
            return base.ParseFile(path);

        return ParseFileWithSelectedBlocks(path);
    }

    protected override LogEntry? ParseLineCore(string line)
    {
        // Service messages: ##teamcity[...]
        var svcMatch = ServiceMessageRegex().Match(line);
        if (svcMatch.Success)
            return ParseServiceMessage(svcMatch, line);

        // Timestamped lines: [HH:mm:ss] ...
        var tsMatch = TimestampedLineRegex().Match(line);
        if (tsMatch.Success)
            return ParseTimestampedLine(tsMatch, line);

        // Plain text (continuation / unstructured)
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
            _ => LevelDetector.Detect(message)
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

    private LogEntry ParseServiceMessage(Match match, string rawLine)
    {
        var name = match.Groups["name"].Value;
        var attrsRaw = match.Groups["attrs"].Value;

        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in AttributeRegex().Matches(attrsRaw))
            attrs[m.Groups["key"].Value] = Unescape(m.Groups["value"].Value);

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
        if (attrs.TryGetValue("status", out var status))
        {
            return status.ToUpperInvariant() switch
            {
                "ERROR" or "FAILURE" => LogLevel.Error,
                "WARNING" => LogLevel.Warn,
                _ => LogLevel.Info
            };
        }

        return name.ToLowerInvariant() switch
        {
            "testfailed" or "buildproblem" or "buildfailure" => LogLevel.Error,
            "testignored" => LogLevel.Warn,
            "teststarted" or "testfinished" or "teststdout" => LogLevel.Debug,
            "compilationstarted" or "compilationfinished" => LogLevel.Info,
            "progressstart" or "progressfinish" => LogLevel.Info,
            "blockopened" or "blockclosed" => LogLevel.Debug,
            _ => LogLevel.Info
        };
    }

    private static string BuildServiceMessageText(string name, Dictionary<string, string> attrs)
    {
        return name.ToLowerInvariant() switch
        {
            "message" => attrs.GetValueOrDefault("text", name),
            "buildproblem" => $"Build Problem: {attrs.GetValueOrDefault("description", "unknown")}",
            "buildstatus" => $"Build Status: {attrs.GetValueOrDefault("text", attrs.GetValueOrDefault("status", "unknown"))}",
            "teststarted" => $"Test Started: {attrs.GetValueOrDefault("name", "?")}",
            "testfinished" => $"Test Finished: {attrs.GetValueOrDefault("name", "?")}",
            "testfailed" => $"Test Failed: {attrs.GetValueOrDefault("name", "?")} - {attrs.GetValueOrDefault("message", "")}",
            "testignored" => $"Test Ignored: {attrs.GetValueOrDefault("name", "?")} - {attrs.GetValueOrDefault("message", "")}",
            "blockopened" => $"> {attrs.GetValueOrDefault("name", "")}",
            "blockclosed" => $"< {attrs.GetValueOrDefault("name", "")}",
            "progressstart" => $"Progress: {attrs.GetValueOrDefault("message", "")}",
            "progressfinish" => $"Progress Done: {attrs.GetValueOrDefault("message", "")}",
            "compilationstarted" => $"Compilation: {attrs.GetValueOrDefault("compiler", "")}",
            "compilationfinished" => $"Compilation Done: {attrs.GetValueOrDefault("compiler", "")}",
            _ => $"{name}: {string.Join(", ", attrs.Select(kv => $"{kv.Key}={kv.Value}"))}"
        };
    }

    private DateTime ParseTimestamp(string ts)
    {
        if (DateTime.TryParse(ts, CultureInfo.InvariantCulture, DateTimeStyles.None, out var full))
            return ts.Length > 10 ? full : _baseDate.Add(full.TimeOfDay);

        if (TimeSpan.TryParse(ts, out var time))
            return _baseDate.Add(time);

        return DateTime.Now;
    }

    private static DateTime ParseServiceTimestamp(string ts)
    {
        string[] formats =
        [
            "yyyy-MM-dd'T'HH:mm:ss.fffzzz",
            "yyyy-MM-dd'T'HH:mm:ss.fff",
            "yyyy-MM-dd'T'HH:mm:sszzz",
            "yyyy-MM-dd'T'HH:mm:ss"
        ];

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

    private IEnumerable<LogEntry> ParseFileWithSelectedBlocks(string path)
    {
        var lineNumber = 0;
        var blockPath = new List<string>();
        var insideSelectedBlock = false;

        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;

            var includeLine = ShouldIncludeLine(line, blockPath, ref insideSelectedBlock);
            if (!includeLine)
                continue;

            var entry = ParseLine(line);
            if (entry == null)
                continue;

            yield return entry with
            {
                FilePath = path,
                LineNumber = lineNumber
            };
        }
    }

    private bool ShouldIncludeLine(string line, List<string> blockPath, ref bool insideSelectedBlock)
    {
        if (string.IsNullOrWhiteSpace(line))
            return insideSelectedBlock;

        if (!TryParseContextLine(line, out var context))
            return insideSelectedBlock;

        if (!string.IsNullOrWhiteSpace(context.Bracket))
        {
            SetPathAtDepth(blockPath, context.Depth, context.Bracket!);
        }
        else if (context.Depth <= 0)
        {
            blockPath.Clear();
        }
        else
        {
            TrimPathToDepth(blockPath, context.Depth);
        }

        var matchedBoundary = TryMatchSelectedBoundary(context.Message, out var boundaryName);
        if (matchedBoundary)
            SetPathAtDepth(blockPath, context.Depth + 1, boundaryName!);

        var pathMatched = blockPath.Any(_selectedBlocks.Contains);
        insideSelectedBlock = pathMatched || matchedBoundary;

        return insideSelectedBlock;
    }

    private bool TryParseContextLine(string line, out TeamCityContextLine context)
    {
        context = default;

        var match = ContextLineRegex().Match(line);
        if (!match.Success)
            return false;

        var tail = match.Groups["tail"].Value;
        var index = 0;

        while (index < tail.Length && tail[index] == ' ')
            index++;

        if (index < tail.Length && tail[index] == ':')
            index++;

        var depth = 0;
        while (index < tail.Length)
        {
            if (tail[index] == '\t')
            {
                depth++;
                index++;
                continue;
            }

            if (tail[index] == ' ')
            {
                index++;
                continue;
            }

            break;
        }

        string? bracket = null;
        if (index < tail.Length && tail[index] == '[')
        {
            var closing = tail.IndexOf(']', index + 1);
            if (closing > index)
            {
                bracket = tail.Substring(index + 1, closing - index - 1).Trim();
                index = closing + 1;
            }
        }

        var message = index < tail.Length
            ? tail[index..].Trim()
            : string.Empty;

        context = new TeamCityContextLine(depth, bracket, message);
        return true;
    }

    private bool TryMatchSelectedBoundary(string message, out string? blockName)
    {
        blockName = null;
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var colonIndex = message.IndexOf(':');
        if (colonIndex > 0)
        {
            var candidate = message[..colonIndex].Trim();
            if (_selectedBlocks.Contains(candidate))
            {
                blockName = candidate;
                return true;
            }
        }

        var durationSeparator = message.LastIndexOf(" (", StringComparison.Ordinal);
        if (durationSeparator > 0 && message.EndsWith(")", StringComparison.Ordinal))
        {
            var candidate = message[..durationSeparator].Trim();
            if (_selectedBlocks.Contains(candidate))
            {
                blockName = candidate;
                return true;
            }
        }

        return false;
    }

    private static void SetPathAtDepth(List<string> blockPath, int depth, string name)
    {
        if (depth < 0)
            depth = 0;

        if (blockPath.Count > depth)
            blockPath.RemoveRange(depth, blockPath.Count - depth);

        while (blockPath.Count < depth)
            blockPath.Add(string.Empty);

        if (blockPath.Count == depth)
            blockPath.Add(name);
        else
            blockPath[depth] = name;
    }

    private static void TrimPathToDepth(List<string> blockPath, int depth)
    {
        var keep = Math.Max(0, depth);
        if (blockPath.Count > keep)
            blockPath.RemoveRange(keep, blockPath.Count - keep);
    }

    private readonly record struct TeamCityContextLine(int Depth, string? Bracket, string Message);
}
