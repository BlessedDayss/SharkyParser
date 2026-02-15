namespace SharkyParser.Api.DTOs;

public record LogStatisticsDto(
    int Total,
    int Errors,
    int Warnings,
    int Info,
    int Debug,
    bool IsHealthy,
    string ExtendedData = ""
);
