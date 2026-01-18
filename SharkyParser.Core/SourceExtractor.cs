using System.Text.RegularExpressions;

namespace SharkyParser.Core;

public static class SourceExtractor
{
    private static readonly Regex LevelPattern = 
        new(@"^(INFO|ERROR|WARN|WARNING|DEBUG|TRACE|FATAL|CRITICAL|ERR|ERRO)\s+", RegexOptions.IgnoreCase);

    public static string Extract(ref string messagePart)
    {
        var levelMatch = LevelPattern.Match(messagePart);
        if (levelMatch.Success)
        {
            messagePart = messagePart[levelMatch.Length..];
        }

        var sourceMatch = Regex.Match(messagePart, @"^\[([^\]]+)\]\s*");
        if (sourceMatch.Success)
        {
            var source = sourceMatch.Groups[1].Value;
            messagePart = messagePart[sourceMatch.Length..];
            return source;
        }

        return string.Empty;
    }
}
