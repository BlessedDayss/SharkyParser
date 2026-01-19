using System.Globalization;

namespace SharkyParser.Core;

public static partial class TimestampParser
{
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

    public static bool TryParse(string line, out DateTime result, out int length)
    {
        result = default;
        length = 0;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        for (int len = Math.Min(line.Length, 23); len >= 8; len--)
        {
            if (!char.IsDigit(line[0])) return false;

            var potential = line.Substring(0, len);
            
            foreach (var format in Formats)
            {
                if (format.Length == len && 
                    DateTime.TryParseExact(potential, format, CultureInfo.InvariantCulture, 
                        DateTimeStyles.None, out result))
                {
                    if (line.Length == len || char.IsWhiteSpace(line[len]))
                    {
                        length = len;
                        return true;
                    }
                }
            }
        }

        return false;
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
}
