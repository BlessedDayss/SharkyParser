namespace SharkyParser.Api.DTOs;

public record ParseResultDto(
    Guid FileId,
    IReadOnlyList<LogEntryDto> Entries,
    IReadOnlyList<LogColumnDto> Columns,
    LogStatisticsDto Statistics
);
