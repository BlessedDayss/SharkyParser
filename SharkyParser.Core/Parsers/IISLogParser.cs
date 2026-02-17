using System.Globalization;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;

namespace SharkyParser.Core.Parsers;

public class IISLogParser(IAppLogger logger) : BaseLogParser(logger)
{
    public override LogType SupportedLogType => LogType.IIS;
    public override string ParserName => "IIS Logs";
    public override string ParserDescription => "Parses IIS Logs (W3C format) with dynamic header detection and streaming.";

    private string[]? _headers;

    private LogEntry? ParseIisLine(string line, string[] headers) {
        var fields = SplitLine(line);
        if (fields.Length != headers.Length) {
            return null;
        }

        var entry = new LogEntry {
            Timestamp = DateTime.MinValue,
            RawData = line
        };

        string? datePart = null;
        string? timePart = null;

        for (int i = 0; i < headers.Length; i++) {
            var header = headers[i];
            var value = fields[i];

            if (value == "-") continue;

            if (header.Equals("date", StringComparison.OrdinalIgnoreCase)) {
                datePart = value;
            } else if (header.Equals("time", StringComparison.OrdinalIgnoreCase)) {
                timePart = value;
            } else {
                entry.Fields[header] = value;
            }
        }

        if (datePart != null && timePart != null) {
            if (DateTime.TryParseExact($"{datePart} {timePart}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)) {
                entry = entry with { Timestamp = dt };
            }
        } else if (datePart != null && DateTime.TryParse(datePart, out var d)) {
            entry = entry with { Timestamp = d };
        }
        
        // Compute Level from Status Code
        if (entry.Fields.TryGetValue("sc-status", out var statusCode)) {
            entry = entry with { Level = GetLevelFromStatusCode(statusCode) };
        }

        return entry;
    }

    private static string GetLevelFromStatusCode(string statusCode) {
        if (int.TryParse(statusCode, out int code)) {
            if (code >= 100 && code < 400) {
                return LogLevel.Info;
            }
            if (code >= 400 && code < 500) {
                return LogLevel.Warn;
            }
            if (code >= 500 && code < 600) {
                return LogLevel.Error;
            }
        }
        return LogLevel.Info; // Default to Info if parsing fails or code is outside expected ranges
    }

    protected override LogEntry? ParseLineCore(string line) {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        if (line.StartsWith("#")) {
            if (line.StartsWith("#Fields:")) {
                _headers = line.Substring(8).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            }
            return null;
        }

        if (_headers == null)
            return null;

        return ParseIisLine(line, _headers);
    }

    public override IReadOnlyList<LogColumn> GetColumns() {
        var columns = new List<LogColumn> {
            new("Timestamp", "Timestamp", "The date and time of the request.", true),
            new("Level", "Level", "The severity level (usually INFO for IIS).", true),
            new("Message", "URI", "The requested URI stem.", true)
        };

        if (_headers != null) {
            foreach (var header in _headers) {
                if (header.Equals("date", StringComparison.OrdinalIgnoreCase) || 
                    header.Equals("time", StringComparison.OrdinalIgnoreCase) ||
                    header.Equals("cs-uri-stem", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                var friendlyName = GetFriendlyFieldName(header);
                var description = GetFieldDescription(header);
                columns.Add(new LogColumn(header, friendlyName, description, false));
            }
        }

        return columns;
    }

    private static string GetFriendlyFieldName(string w3cField) {
        return w3cField switch {
            "s-sitename" => "Site Name",
            "s-computername" => "Server Name",
            "s-ip" => "Server IP",
            "cs-method" => "Method",
            "cs-uri-query" => "Query String",
            "s-port" => "Server Port",
            "cs-username" => "Username",
            "c-ip" => "Client IP",
            "cs-version" => "Protocol Version",
            "cs(User-Agent)" => "User Agent",
            "cs(Cookie)" => "Cookie",
            "cs(Referer)" => "Referer",
            "cs-host" => "Host",
            "sc-status" => "Status",
            "sc-substatus" => "Sub Status",
            "sc-win32-status" => "Win32 Status",
            "sc-bytes" => "Bytes Sent",
            "cs-bytes" => "Bytes Received",
            "time-taken" => "Time Taken (ms)",
            _ => w3cField
        };
    }

    private static string? GetFieldDescription(string w3cField) {
        return w3cField switch {
            "s-sitename" => "The Internet service name and instance number",
            "s-computername" => "The name of the server",
            "s-ip" => "The IP address of the server",
            "cs-method" => "The HTTP method (GET, POST, etc.)",
            "cs-uri-query" => "The query string portion of the URI",
            "s-port" => "The server port number",
            "cs-username" => "The authenticated user name",
            "c-ip" => "The IP address of the client",
            "cs-version" => "The protocol version (HTTP/1.1, etc.)",
            "cs(User-Agent)" => "The browser/client identification string",
            "cs(Cookie)" => "The content of the cookie sent or received",
            "cs(Referer)" => "The site that the user last visited",
            "cs-host" => "The host header name",
            "sc-status" => "The HTTP status code",
            "sc-substatus" => "The HTTP substatus code",
            "sc-win32-status" => "The Windows status code",
            "sc-bytes" => "The number of bytes sent by the server",
            "cs-bytes" => "The number of bytes received by the server",
            "time-taken" => "The time taken to process the request in milliseconds",
            _ => null
        };
    }

    private static string[] SplitLine(string line) {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++) {
            char c = line[i];
            if (c == '\"') {
                inQuotes = !inQuotes;
            } else if (c == ' ' && !inQuotes) {
                result.Add(current.ToString());
                current.Clear();
            } else {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}
