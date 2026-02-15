namespace SharkyParser.Api.DTOs;

public record ParseResultDto(
    IReadOnlyList<LogEntryDto> Entries,
    LogStatisticsDto Statistics
);
