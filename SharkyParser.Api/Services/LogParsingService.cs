using Microsoft.EntityFrameworkCore;
using SharkyParser.Api.Data;
using SharkyParser.Api.Data.Models;
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
    private readonly AppDbContext _dbContext;
    private readonly ILogParserFactory _parserFactory;
    private readonly ILogAnalyzer _analyzer;
    private readonly ILogger _logger;

    public LogParsingService(
        AppDbContext dbContext,
        ILogParserFactory parserFactory,
        ILogAnalyzer analyzer,
        ILogger logger)
    {
        _dbContext = dbContext;
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

    public async Task<ParseResultDto> ParseFileAsync(Stream fileStream, string fileName, LogType logType, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"sharky_{Guid.NewGuid():N}.log");

        try
        {
            // Read stream into byte array for DB and file stream for parsing
            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms, ct);
            var fileBytes = ms.ToArray();

            await File.WriteAllBytesAsync(tempPath, fileBytes, ct);

            var parser = _parserFactory.CreateParser(logType);
            var entries = (await parser.ParseFileAsync(tempPath)).ToList();
            var columns = parser.GetColumns();
            var statistics = _analyzer.GetStatistics(entries, logType);

            var result = new ParseResultDto(
                entries.Select(DtoMapper.ToDto).ToList(),
                columns.Select(DtoMapper.ToDto).ToList(),
                DtoMapper.ToDto(statistics)
            );

            // Save to database
            var fileRecord = new FileRecord
            {
                FileName = fileName,
                FileSize = fileBytes.Length,
                LogType = logType.ToString(),
                Content = fileBytes,
                AnalysisResult = JsonSerializer.Serialize(statistics)
            };

            _dbContext.Files.Add(fileRecord);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInfo($"File saved to DB and processed: {fileName} (ID: {fileRecord.Id})");

            return result;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            }
        }
    }

    public async Task<IEnumerable<FileRecordDto>> GetRecentFilesAsync(int count = 10, CancellationToken ct = default)
    {
        return await _dbContext.Files
            .OrderByDescending(f => f.UploadedAt)
            .Take(count)
            .Select(f => DtoMapper.ToDto(f))
            .ToListAsync(ct);
    }

}
