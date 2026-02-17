using SharkyParser.Api.Data.Models;
using SharkyParser.Api.Data.Repositories;
using SharkyParser.Api.DTOs;
using SharkyParser.Api.Infrastructure;
using SharkyParser.Api.Interfaces;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using System.Text.Json;
using ILogger = SharkyParser.Core.Interfaces.ILogger;

namespace SharkyParser.Api.Services;

public sealed class LogParsingService : ILogParsingService
{
    private readonly IFileRepository _fileRepository;
    private readonly ILogParserFactory _parserFactory;
    private readonly ILogAnalyzer _analyzer;
    private readonly ILogger _logger;

    public LogParsingService(
        IFileRepository fileRepository,
        ILogParserFactory parserFactory,
        ILogAnalyzer analyzer,
        ILogger logger)
    {
        _fileRepository = fileRepository;
        _parserFactory  = parserFactory;
        _analyzer       = analyzer;
        _logger         = logger;
    }

    public IReadOnlyList<LogTypeDto> GetAvailableLogTypes()
        => _parserFactory.GetAvailableTypes().Select(DtoMapper.ToDto).ToList().AsReadOnly();

    public async Task<ParseResultDto> ParseFileAsync(
        Stream fileStream, string fileName, LogType logType, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"sharky_{Guid.NewGuid():N}.log");

        try
        {
            // Single read: write to temp file and accumulate bytes in one pass.
            byte[] fileBytes;
            await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(fs, ct);
            }
            fileBytes = await File.ReadAllBytesAsync(tempPath, ct);

            var parser     = _parserFactory.CreateParser(logType);
            var entries    = (await parser.ParseFileAsync(tempPath)).ToList();
            var columns    = parser.GetColumns();
            var statistics = _analyzer.GetStatistics(entries, logType);

            var record = new FileRecord
            {
                FileName       = fileName,
                FileSize       = fileBytes.Length,
                LogType        = logType.ToString(),
                Content        = fileBytes,
                AnalysisResult = JsonSerializer.Serialize(statistics)
            };

            await _fileRepository.AddAsync(record, ct);
            _logger.LogInfo($"Processed and saved: {fileName} (id={record.Id})");

            return new ParseResultDto(
                record.Id,
                entries.Select(DtoMapper.ToDto).ToList(),
                columns.Select(DtoMapper.ToDto).ToList(),
                DtoMapper.ToDto(statistics)
            );
        }
        finally
        {
            if (File.Exists(tempPath))
                try { File.Delete(tempPath); } catch { /* best-effort */ }
        }
    }

    public async Task<IEnumerable<FileRecordDto>> GetRecentFilesAsync(
        int count = 10, CancellationToken ct = default)
    {
        var records = await _fileRepository.GetRecentAsync(count, ct);
        return records.Select(DtoMapper.ToDto);
    }

    public async Task<ParseResultDto> GetEntriesAsync(Guid id, CancellationToken ct = default)
    {
        var record = await _fileRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"File record {id} not found.");

        if (!Enum.TryParse<LogType>(record.LogType, out var logType))
            throw new InvalidOperationException($"Unknown log type stored: {record.LogType}");

        var tempPath = Path.Combine(Path.GetTempPath(), $"sharky_{Guid.NewGuid():N}.log");
        try
        {
            await File.WriteAllBytesAsync(tempPath, record.Content, ct);

            var parser     = _parserFactory.CreateParser(logType);
            var entries    = (await parser.ParseFileAsync(tempPath)).ToList();
            var columns    = parser.GetColumns();
            var statistics = _analyzer.GetStatistics(entries, logType);

            return new ParseResultDto(
                id,
                entries.Select(DtoMapper.ToDto).ToList(),
                columns.Select(DtoMapper.ToDto).ToList(),
                DtoMapper.ToDto(statistics)
            );
        }
        finally
        {
            if (File.Exists(tempPath))
                try { File.Delete(tempPath); } catch { /* best-effort */ }
        }
    }
}
