using System.Globalization;
using System.Text.RegularExpressions;

namespace SharkyParser.Core;

public static partial class TimestampParser
{
    private static readonly Regex Pattern = TimestampRegex();
    
    private static readonly string[] Formats =
    {
        "yyyy-MM-dd HH:mm:ss,fff",
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy/MM/dd HH:mm:ss",
        "dd-MM-yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm:ss",
        "HH:mm:ss,fff",
        "HH:mm:ss.fff",
        "HH:mm:ss:fff",
        "HH:mm:ss:ffff",
        "HH:mm:ss"
    };

    public static bool TryMatch(string line, out Match match)
    {
        match = Pattern.Match(line);
        return match.Success && 
               (match.Length == line.Length || char.IsWhiteSpace(line[match.Length]));
    }

    public static bool TryParse(string text, out DateTime result)
    {
        var normalized = text.Trim();
        
        foreach (var format in Formats)
        {
            if (DateTime.TryParseExact(normalized, format, CultureInfo.InvariantCulture, 
                    DateTimeStyles.None, out result))
            {
                return true;
            }
        }

        return DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    [GeneratedRegex(@"^(\d{4}[-/]\d{2}[-/]\d{2}\s+)?(\d{1,2}):(\d{2}):(\d{2})([,.:]\d{1,4})?(?=\s|$)", RegexOptions.Compiled)]
    private static partial Regex TimestampRegex();
}
