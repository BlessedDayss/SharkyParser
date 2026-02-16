using System.ComponentModel;
using SharkyParser.Api.DTOs;
using SharkyParser.Api.Interfaces;
using SharkyParser.Core;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Api.Services;

/// <summary>
/// Encapsulates all log parsing and analysis logic.
/// Handles temp file lifecycle, parser resolution, DTO mapping.
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
            .Select(t => new LogTypeDto(
                (int)t,
                t.ToString(),
                GetDescription(t)
            ))
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
            var statistics = _analyzer.GetStatistics(entries);

            _logger.LogFileProcessed(tempPath);

            var entriesDto = entries.Select(MapToDto).ToList();
            var statsDto = MapToDto(statistics);

            return new ParseResultDto(entriesDto, statsDto);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            }
        }
    }

    private static LogEntryDto MapToDto(LogEntry entry)
    {
        return new LogEntryDto
        {
            Timestamp = entry.Timestamp.ToString("O"),
            Level = entry.Level,
            Message = entry.Message,
            Source = entry.Source,
            StackTrace = entry.StackTrace,
            LineNumber = entry.LineNumber,
            FilePath = entry.FilePath,
            RawData = entry.RawData
        };
    }

    private static LogStatisticsDto MapToDto(LogStatistics statistics)
    {
        return new LogStatisticsDto(
            statistics.TotalCount,
            statistics.ErrorCount,
            statistics.WarningCount,
            statistics.InfoCount,
            statistics.DebugCount,
            statistics.IsHealthy,
            statistics.ExtendedData
        );
    }

    private static string GetDescription(LogType type)
    {
        var field = typeof(LogType).GetField(type.ToString());
        if (field == null) return type.ToString();

        var attr = field.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .OfType<DescriptionAttribute>()
            .FirstOrDefault();

        return attr?.Description ?? type.ToString();
    }
}
