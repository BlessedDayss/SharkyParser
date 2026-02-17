using System.Text;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Models;

namespace SharkyParser.Cli.Formatters;

/// <summary>
/// Renders parsed log entries in pipe-delimited format for programmatic consumers
/// (e.g. the Rider plugin or any external tool integrating via stdout).
/// </summary>
public class EmbeddedParseFormatter : IParseOutputFormatter
{
    public void Write(IReadOnlyList<LogEntry> logs, ILogParser parser, int totalEntries)
    {
        var errors   = logs.Count(l => l.Level.Equals("ERROR",   StringComparison.OrdinalIgnoreCase));
        var warnings = logs.Count(l => l.Level.Equals("WARN",    StringComparison.OrdinalIgnoreCase)
                                    || l.Level.Equals("WARNING", StringComparison.OrdinalIgnoreCase));
        var info     = logs.Count(l => l.Level.Equals("INFO",    StringComparison.OrdinalIgnoreCase));
        var debug    = logs.Count(l => l.Level.Equals("DEBUG",   StringComparison.OrdinalIgnoreCase)
                                    || l.Level.Equals("TRACE",   StringComparison.OrdinalIgnoreCase));

        Console.WriteLine($"STATS|{totalEntries}|{errors}|{warnings}|{info}|{debug}");

        var dynamicColumns = parser.GetColumns().Where(c => !c.IsPredefined).ToList();

        if (dynamicColumns.Count > 0)
            Console.WriteLine("COLUMNS|" + string.Join("|", dynamicColumns.Select(c => Escape(c.Name))));

        foreach (var log in logs)
        {
            var line = new StringBuilder();
            line.Append("ENTRY|");
            line.Append(log.Timestamp.ToString("o"));   line.Append('|');
            line.Append(log.Level);                     line.Append('|');
            line.Append(Escape(log.Message));           line.Append('|');
            line.Append(Escape(log.Source));            line.Append('|');
            line.Append(log.LineNumber);                line.Append('|');
            line.Append(Escape(log.FilePath));          line.Append('|');
            line.Append(Escape(log.RawData));

            foreach (var column in dynamicColumns)
            {
                line.Append('|');
                if (log.Fields.TryGetValue(column.Name, out var value))
                    line.Append(Escape(value));
            }

            Console.WriteLine(line.ToString());
        }
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("|", "\\|").Replace("\n", "\\n").Replace("\r", "");
    }
}
