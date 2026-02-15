using System.Text.RegularExpressions;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Core.Parsers;

public class InstallationLogParser : BaseLogParser
{
    public override LogType SupportedLogType => LogType.Installation;
    public override string ParserName => "Installation Logs";
    public override string ParserDescription => "Parses Installation Logs";
    
    public StackTraceMode StackTraceMode { get; set; } = StackTraceMode.AllToStackTrace;

    private static readonly Regex TimestampPattern = new(
        @"^(\[(?<timestamp>\d{2}:\d{2}:\d{2})\]|(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(?:[.,]\d{3})?)|(?<timestamp>\d{2}:\d{2}:\d{2}(?:[:.]\d{1,4})?)(?=\s))",
        RegexOptions.Compiled);

    public InstallationLogParser(IAppLogger logger)
        : base(logger)
    {
    }

    public override IEnumerable<LogEntry> ParseFile(string path)
    {
        LogEntry? currentEntry = null;
        var lineNumber = 0;
        
        var baseDate = ExtractDateFromFileName(path) ?? File.GetLastWriteTime(path).Date;
        
        _logger.LogInfo("Started to parse Installation log file: " + path);

        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
    
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (TimestampPattern.IsMatch(line))
            {
                if (currentEntry != null)
                {
                    yield return currentEntry;
                }
        
                currentEntry = CreateLogEntry(line, path, lineNumber, baseDate);
            }
            else if (currentEntry != null)
            {
                if (StackTraceMode == StackTraceMode.AllToStackTrace)
                {
                    currentEntry = currentEntry with
                    {
                        StackTrace = string.IsNullOrEmpty(currentEntry.StackTrace)
                            ? line
                            : currentEntry.StackTrace + Environment.NewLine + line,
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
        {
            yield return currentEntry;
        }
    }
    
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
            _logger.LogError($"Error extracting date from file name '{path}': {ex}");
        }

        return null;
    }


    protected override LogEntry? ParseLineCore(string line)
    {
        return CreateLogEntry(line, "", 0, DateTime.Now.Date);
    }

    private LogEntry CreateLogEntry(string line, string filePath, int lineNumber, DateTime baseDate)
    {
        var match = TimestampPattern.Match(line);
        DateTime timestamp = baseDate;
        string message = line;
        
        if (match.Success)
        {
            var timestampStr = match.Groups["timestamp"].Value;
            
            // Handle HH:MM:SS (8 chars) or HH:MM:SS:NNNN / HH:MM:SS.NNNN (bare time with optional ms)
            var timePart = timestampStr;
            // Normalize "21:21:47:6804" → keep only HH:MM:SS for TimeSpan parsing, preserve ms
            var colonCount = timePart.Split(':').Length - 1;
            TimeSpan parsedTime = default;
            bool timeOk = false;

            if (colonCount >= 3)
            {
                // Format like 21:21:47:6804 — 4th segment is milliseconds
                var segments = timePart.Split(':');
                if (TimeSpan.TryParse($"{segments[0]}:{segments[1]}:{segments[2]}", out parsedTime))
                {
                    if (segments.Length > 3 && int.TryParse(segments[3], out var ms))
                    {
                        parsedTime = parsedTime.Add(TimeSpan.FromMilliseconds(ms > 999 ? ms / 10.0 : ms));
                    }
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
            Source = "Installation",
            Message = message,
            RawData = line,
            FilePath = filePath,
            LineNumber = lineNumber
        };
    }
}