using System;
using System.Collections.Generic;

namespace SharkyParser.Core.Models;

public record LogEntry
{
    public required DateTime Timestamp { get; init; }
    public string Level { get; init; } = "INFO";
    public string Message { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int LineNumber { get; init; }
    public string RawData { get; init; } = string.Empty;
    
    /// <summary>
    /// Dynamic fields defined by the parser.
    /// </summary>
    public Dictionary<string, string> Fields { get; init; } = new();

    public string Source { get; set; }
}