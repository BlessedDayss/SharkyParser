namespace SharkyParser.Core;

public static class SourceExtractor
{
    private static readonly string[] Levels = 
        { "INFO", "ERROR", "WARN", "WARNING", "DEBUG", "TRACE", "FATAL", "CRITICAL", "ERR", "ERRO" };

    public static string Extract(ref string messagePart)
    {
        foreach (var level in Levels)
        {
            if (messagePart.StartsWith(level, StringComparison.OrdinalIgnoreCase))
            {
                int endOfLevel = level.Length;
                if (endOfLevel < messagePart.Length && char.IsWhiteSpace(messagePart[endOfLevel]))
                {
                    // Skip whitespaces after level
                    int startOfNext = endOfLevel;
                    while (startOfNext < messagePart.Length && char.IsWhiteSpace(messagePart[startOfNext]))
                        startOfNext++;
                    
                    messagePart = messagePart[startOfNext..];
                    break;
                }
            }
        }

        // 2. Extract [Source]
        if (messagePart.StartsWith('['))
        {
            int closeBracketIndex = messagePart.IndexOf(']');
            if (closeBracketIndex > 1) // Must have at least one char between [ and ]
            {
                string source = messagePart[1..closeBracketIndex];
                
                int startOfMessage = closeBracketIndex + 1;
                while (startOfMessage < messagePart.Length && char.IsWhiteSpace(messagePart[startOfMessage]))
                    startOfMessage++;
                
                messagePart = messagePart[startOfMessage..];
                return source;
            }
        }

        return string.Empty;
    }
}
