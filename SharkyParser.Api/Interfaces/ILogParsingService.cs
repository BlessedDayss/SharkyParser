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
    /// Parses an uploaded log file, saves it to the database, and returns results.
    /// </summary>
    Task<ParseResultDto> ParseFileAsync(Stream fileStream, string fileName, LogType logType, CancellationToken ct = default);

    /// <summary>
    /// Returns recently processed files from the database.
    /// </summary>
    Task<IEnumerable<FileRecordDto>> GetRecentFilesAsync(int count = 10, CancellationToken ct = default);

}
