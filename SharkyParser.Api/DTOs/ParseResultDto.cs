namespace SharkyParser.Api.DTOs;

public record ParseResultDto(
    IReadOnlyList<LogEntryDto> Entries,
    IReadOnlyList<LogColumnDto> Columns,
    LogStatisticsDto Statistics
);
