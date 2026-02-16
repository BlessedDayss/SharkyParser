using SharkyParser.Api.DTOs;
using SharkyParser.Core.Enums;

namespace SharkyParser.Api.Interfaces;

/// <summary>
/// Service responsible for log file parsing and analysis.
/// Extracts business logic from the controller layer (SRP).
/// </summary>
public interface ILogParsingService
{
    /// <summary>
    /// Returns all available log types with their descriptions.
    /// </summary>
    IReadOnlyList<LogTypeDto> GetAvailableLogTypes();

    /// <summary>
    /// Parses an uploaded log file and returns structured results with statistics.
    /// </summary>
    Task<ParseResultDto> ParseFileAsync(Stream fileStream, LogType logType, CancellationToken ct = default);
}
