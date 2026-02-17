using SharkyParser.Api.DTOs;
using SharkyParser.Api.Infrastructure;
using SharkyParser.Api.Interfaces;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Api.Services;

/// <summary>
/// Orchestrates log file parsing: temp file management + delegation to Core services.
/// Mapping is handled by DtoMapper (SRP).
/// </summary>
public sealed class LogParsingService : ILogParsingService
{
    private readonly ILogParserFactory _parserFactory;
    private readonly ILogAnalyzer _analyzer;
    private readonly IAppLogger _logger;

    public LogParsingService(
        ILogParserFactory parserFactory,
        ILogAnalyzer analyzer,
        IAppLogger logger)
    {
        _parserFactory = parserFactory;
        _analyzer = analyzer;
        _logger = logger;
    }

    public IReadOnlyList<LogTypeDto> GetAvailableLogTypes()
    {
        return _parserFactory.GetAvailableTypes()
            .Select(DtoMapper.ToDto)
            .ToList()
            .AsReadOnly();
    }

    public async Task<ParseResultDto> ParseFileAsync(Stream fileStream, LogType logType, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"sharky_{Guid.NewGuid():N}.log");

        try
        {
            await using (var fs = new FileStream(tempPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs, ct);
            }

            var parser = _parserFactory.GetParserForType(logType);
            var entries = (await parser.ParseFileAsync(tempPath)).ToList();
            var columns = parser.GetColumns();
            var statistics = _analyzer.GetStatistics(entries, logType);

            _logger.LogFileProcessed(tempPath);

            return new ParseResultDto(
                entries.Select(DtoMapper.ToDto).ToList(),
                columns.Select(DtoMapper.ToDto).ToList(),
                DtoMapper.ToDto(statistics)
            );
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            }
        }
    }
}
