namespace SharkyParser.Core;

public record LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public string RawData { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int LineNumber { get; init; }
}