namespace SharkyParser.Api.DTOs;

public record LogEntryDto
{
    public required string Timestamp { get; init; }
    public required string Level { get; init; }
    public required string Message { get; init; }
    public required string Source { get; init; }
    public string StackTrace { get; init; } = string.Empty;
    public int LineNumber { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string RawData { get; init; } = string.Empty;
}
