using Microsoft.AspNetCore.Mvc;
using SharkyParser.Api.DTOs;
using SharkyParser.Core;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogParserFactory _parserFactory;
    private readonly ILogAnalyzer _analyzer;
    private readonly ILogger<LogsController> _logger;

    private const int MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    public LogsController(
        ILogParserFactory parserFactory,
        ILogAnalyzer analyzer,
        ILogger<LogsController> logger)
    {
        _parserFactory = parserFactory;
        _analyzer = analyzer;
        _logger = logger;
    }

    [HttpGet("types")]
    public ActionResult<IEnumerable<LogTypeDto>> GetTypes()
    {
        var types = _parserFactory.GetAvailableTypes()
            .Select(t => new LogTypeDto(
                (int)t,
                t.ToString(),
                GetDescription(t)
            ))
            .ToList();
        return Ok(types);
    }

    [HttpPost("parse")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
    public async Task<ActionResult<ParseResultDto>> Parse([FromForm] IFormFile file, [FromForm] string logType = "Installation")
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest($"File size exceeds limit of {MaxFileSizeBytes / (1024 * 1024)} MB");

        if (!Enum.TryParse<LogType>(logType, ignoreCase: true, out var type))
            return BadRequest($"Invalid log type. Available: {string.Join(", ", Enum.GetNames<LogType>())}");

        if (!_parserFactory.GetAvailableTypes().Contains(type))
            return BadRequest($"Log type {logType} is not supported");

        string tempPath = null!;
        try
        {
            tempPath = Path.Combine(Path.GetTempPath(), $"sharky_{Guid.NewGuid():N}.log");
            await using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var parser = _parserFactory.GetParserForType(type);
            var entries = (await parser.ParseFileAsync(tempPath)).ToList();
            var statistics = _analyzer.GetStatistics(entries);

            var entriesDto = entries.Select(e => new LogEntryDto
            {
                Timestamp = e.Timestamp.ToString("O"),
                Level = e.Level,
                Message = e.Message,
                Source = e.Source,
                StackTrace = e.StackTrace,
                LineNumber = e.LineNumber,
                FilePath = e.FilePath,
                RawData = e.RawData
            }).ToList();

            var statsDto = new LogStatisticsDto(
                statistics.TotalCount,
                statistics.ErrorCount,
                statistics.WarningCount,
                statistics.InfoCount,
                statistics.DebugCount,
                statistics.IsHealthy,
                statistics.ExtendedData
            );

            return Ok(new ParseResultDto(entriesDto, statsDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse log file");
            return StatusCode(500, "Failed to parse log file");
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempPath) && System.IO.File.Exists(tempPath))
            {
                try { System.IO.File.Delete(tempPath); } catch { /* ignore */ }
            }
        }
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "Active" });

    private static string GetDescription(LogType type)
    {
        var field = typeof(LogType).GetField(type.ToString());
        if (field == null) return type.ToString();
        var attr = field.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .OfType<System.ComponentModel.DescriptionAttribute>()
            .FirstOrDefault();
        return attr?.Description ?? type.ToString();
    }
}
