using System.Globalization;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;

namespace SharkyParser.Core.Parsers;

public class IISLogParser(ILogger logger) : BaseLogParser(logger)
{
    public override LogType SupportedLogType => LogType.IIS;
    public override string ParserName => "IIS Logs";
    public override string ParserDescription => "Parses IIS Logs (W3C format) with dynamic header detection and streaming.";

    /// <summary>
    /// Mutable per-file state. Reset at the start of every ParseFile call so
    /// a single cached instance does not bleed headers from one file to the next.
    /// </summary>
    private string[]? _headers;

    // ── ParseFile override: reset per-file state ─────────────────────────────

    public override IEnumerable<LogEntry> ParseFile(string path)
    {
        _headers = null;
        return base.ParseFile(path);
    }

    public override async Task<IEnumerable<LogEntry>> ParseFileAsync(string path)
    {
        _headers = null;
        return await Task.Run(() => base.ParseFile(path));
    }

    // ── Core parsing ─────────────────────────────────────────────────────────

    protected override LogEntry? ParseLineCore(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        if (line.StartsWith("#"))
        {
            if (line.StartsWith("#Fields:"))
                _headers = line.Substring(8).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return null;
        }

        if (_headers == null)
            return null;

        return ParseIisLine(line, _headers);
    }

    private static LogEntry? ParseIisLine(string line, string[] headers)
    {
        var fields = SplitLine(line);
        if (fields.Length != headers.Length)
            return null;

        var dynamicFields = new Dictionary<string, string>();
        string? datePart = null;
        string? timePart = null;

        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i];
            var value = fields[i];

            if (value == "-") continue;

            if (header.Equals("date", StringComparison.OrdinalIgnoreCase))
                datePart = value;
            else if (header.Equals("time", StringComparison.OrdinalIgnoreCase))
                timePart = value;
            else
                dynamicFields[header] = value;
        }

        // Compute timestamp
        DateTime timestamp = DateTime.MinValue;
        if (datePart != null && timePart != null)
        {
            DateTime.TryParseExact(
                $"{datePart} {timePart}",
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out timestamp);
        }
        else if (datePart != null)
        {
            DateTime.TryParse(datePart, out timestamp);
        }

        // Compute level and message from IIS fields
        var level = dynamicFields.TryGetValue("sc-status", out var sc)
            ? GetLevelFromStatusCode(sc)
            : LogLevel.Info;

        var message = dynamicFields.TryGetValue("cs-uri-stem", out var uri) ? uri : string.Empty;
        var source = dynamicFields.TryGetValue("s-sitename", out var site) ? site : "IIS Web Server";

        return new LogEntry
        {
            Timestamp = timestamp,
            Level = level,
            Message = message,
            Source = source,
            RawData = line,
            Fields = dynamicFields
        };
    }

    private static string GetLevelFromStatusCode(string statusCode)
    {
        if (int.TryParse(statusCode, out int code))
        {
            if (code >= 100 && code < 400) return LogLevel.Info;
            if (code >= 400 && code < 500) return LogLevel.Warn;
            if (code >= 500 && code < 600) return LogLevel.Error;
        }
        return LogLevel.Info;
    }

    public override IReadOnlyList<LogColumn> GetColumns()
    {
        var columns = new List<LogColumn>
        {
            new("Timestamp", "Timestamp", "The date and time of the request.", true),
            new("Level", "Level", "The severity level (derived from HTTP status).", true),
            new("Message", "URI", "The requested URI stem.", true)
        };

        if (_headers != null)
        {
            foreach (var header in _headers)
            {
                if (header.Equals("date", StringComparison.OrdinalIgnoreCase) ||
                    header.Equals("time", StringComparison.OrdinalIgnoreCase) ||
                    header.Equals("cs-uri-stem", StringComparison.OrdinalIgnoreCase))
                    continue;

                columns.Add(new LogColumn(header, GetFriendlyFieldName(header), GetFieldDescription(header), false));
            }
        }

        return columns;
    }

    private static string GetFriendlyFieldName(string w3cField) => w3cField switch
    {
        "s-sitename"       => "Site Name",
        "s-computername"   => "Server Name",
        "s-ip"             => "Server IP",
        "cs-method"        => "Method",
        "cs-uri-query"     => "Query String",
        "s-port"           => "Server Port",
        "cs-username"      => "Username",
        "c-ip"             => "Client IP",
        "cs-version"       => "Protocol Version",
        "cs(User-Agent)"   => "User Agent",
        "cs(Cookie)"       => "Cookie",
        "cs(Referer)"      => "Referer",
        "cs-host"          => "Host",
        "sc-status"        => "Status",
        "sc-substatus"     => "Sub Status",
        "sc-win32-status"  => "Win32 Status",
        "sc-bytes"         => "Bytes Sent",
        "cs-bytes"         => "Bytes Received",
        "time-taken"       => "Time Taken (ms)",
        _                  => w3cField
    };

    private static string? GetFieldDescription(string w3cField) => w3cField switch
    {
        "s-sitename"       => "The Internet service name and instance number",
        "s-computername"   => "The name of the server",
        "s-ip"             => "The IP address of the server",
        "cs-method"        => "The HTTP method (GET, POST, etc.)",
        "cs-uri-query"     => "The query string portion of the URI",
        "s-port"           => "The server port number",
        "cs-username"      => "The authenticated user name",
        "c-ip"             => "The IP address of the client",
        "cs-version"       => "The protocol version (HTTP/1.1, etc.)",
        "cs(User-Agent)"   => "The browser/client identification string",
        "cs(Cookie)"       => "The content of the cookie sent or received",
        "cs(Referer)"      => "The site that the user last visited",
        "cs-host"          => "The host header name",
        "sc-status"        => "The HTTP status code",
        "sc-substatus"     => "The HTTP substatus code",
        "sc-win32-status"  => "The Windows status code",
        "sc-bytes"         => "The number of bytes sent by the server",
        "cs-bytes"         => "The number of bytes received by the server",
        "time-taken"       => "The time taken to process the request in milliseconds",
        _                  => null
    };

    private static string[] SplitLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        foreach (char c in line)
        {
            if (c == '"')
                inQuotes = !inQuotes;
            else if (c == ' ' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
                current.Append(c);
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}
