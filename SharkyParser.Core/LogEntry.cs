namespace SharkyParser.Core;

public record LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string StackTrace { get; init; } = string.Empty;
}