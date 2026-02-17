using System.Text.RegularExpressions;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;

namespace SharkyParser.Core.Parsers;

/// <summary>
/// Parses installation logs with multi-line stack-trace support.
/// Implements IConfigurableParser so factory and commands use the interface
/// instead of casting to the concrete type (OCP/LSP).
/// </summary>
public class InstallationLogParser : BaseLogParser, IConfigurableParser
{
    public override LogType SupportedLogType => LogType.Installation;
    public override string ParserName => "Installation Logs";
    public override string ParserDescription => "Parses Installation Logs";

    public StackTraceMode StackTraceMode { get; private set; } = StackTraceMode.AllToStackTrace;

    private static readonly Regex TimestampPattern = new(
        @"^(\[(?<timestamp>\d{2}:\d{2}:\d{2})\]|(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:[.,]\d{3})?)|(?<timestamp>\d{2}:\d{2}:\d{2}(?:[:.]\d{1,4})?)(?=\s))",
        RegexOptions.Compiled);

    public InstallationLogParser(ILogger logger) : base(logger) { }

    // ── IConfigurableParser ──────────────────────────────────────────────────

    public void Configure(StackTraceMode mode) => StackTraceMode = mode;

    public string GetConfigurationSummary() => $"Stack Trace Mode: {StackTraceMode}";

    // ── Parsing ──────────────────────────────────────────────────────────────

    public override IEnumerable<LogEntry> ParseFile(string path)
    {
        LogEntry? currentEntry = null;
        var lineNumber = 0;

        var baseDate = ExtractDateFromFileName(path) ?? File.GetLastWriteTime(path).Date;

        Logger.LogInfo("Started to parse Installation log file: " + path);

        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (TimestampPattern.IsMatch(line))
            {
                if (currentEntry != null)
                    yield return currentEntry;

                currentEntry = CreateLogEntry(line, path, lineNumber, baseDate);
            }
            else if (currentEntry != null)
            {
                if (StackTraceMode == StackTraceMode.AllToStackTrace)
                {
                    var currentST = currentEntry.Fields.TryGetValue("StackTrace", out var st) ? st : "";
                    currentEntry.Fields["StackTrace"] = string.IsNullOrEmpty(currentST)
                        ? line
                        : currentST + Environment.NewLine + line;

                    currentEntry = currentEntry with
                    {
                        RawData = currentEntry.RawData + Environment.NewLine + line
                    };
                }
                else
                {
                    currentEntry = currentEntry with
                    {
                        Message = currentEntry.Message + Environment.NewLine + line.Trim(),
                        RawData = currentEntry.RawData + Environment.NewLine + line
                    };
                }
            }
            else
            {
                currentEntry = CreateLogEntry(line, path, lineNumber, baseDate);
            }
        }

        if (currentEntry != null)
            yield return currentEntry;
    }

    public override IReadOnlyList<LogColumn> GetColumns()
    {
        return new List<LogColumn>
        {
            new("Timestamp", "Timestamp", "The date and time of the installation event.", true),
            new("Level", "Level", "The severity level.", true),
            new("Message", "Message", "The installation log message.", true),
            new("Source", "Source", "The source component.", false),
            new("StackTrace", "Stack Trace", "Detailed error information.", false)
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private DateTime? ExtractDateFromFileName(string path)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var parts = fileName.Split('_');

            for (int i = 0; i < parts.Length - 5; i++)
            {
                if (parts[i].Length == 4 && int.TryParse(parts[i], out var year) &&
                    parts[i + 1].Length == 2 && int.TryParse(parts[i + 1], out var month) &&
                    parts[i + 2].Length == 2 && int.TryParse(parts[i + 2], out var day))
                {
                    return new DateTime(year, month, day);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error extracting date from file name '{path}': {ex}");
        }

        return null;
    }

    protected override LogEntry? ParseLineCore(string line)
        => CreateLogEntry(line, "", 0, DateTime.Now.Date);

    private LogEntry CreateLogEntry(string line, string filePath, int lineNumber, DateTime baseDate)
    {
        var match = TimestampPattern.Match(line);
        DateTime timestamp = baseDate;
        string message = line;

        if (match.Success)
        {
            var timestampStr = match.Groups["timestamp"].Value;

            var timePart = timestampStr;
            var colonCount = timePart.Split(':').Length - 1;
            TimeSpan parsedTime = default;
            bool timeOk = false;

            if (colonCount >= 3)
            {
                var segments = timePart.Split(':');
                if (TimeSpan.TryParse($"{segments[0]}:{segments[1]}:{segments[2]}", out parsedTime))
                {
                    if (segments.Length > 3 && int.TryParse(segments[3], out var ms))
                        parsedTime = parsedTime.Add(TimeSpan.FromMilliseconds(ms > 999 ? ms / 10.0 : ms));

                    timeOk = true;
                }
            }
            else if (timePart.Length <= 12 && TimeSpan.TryParse(timePart.Replace('.', ':'), out parsedTime))
            {
                timeOk = true;
            }

            if (timeOk)
            {
                timestamp = baseDate.Add(parsedTime);
                message = line.Substring(match.Length).Trim();
            }
            else if (DateTime.TryParse(timestampStr.Replace(',', '.'), out var dateTime))
            {
                timestamp = dateTime;
                message = line.Substring(match.Length).Trim();
            }
        }

        return new LogEntry
        {
            Timestamp = timestamp,
            Level = LevelDetector.Detect(message),
            Message = message,
            RawData = line,
            FilePath = filePath,
            LineNumber = lineNumber,
            Fields = new Dictionary<string, string> { ["Source"] = "Installation" }
        };
    }
}
