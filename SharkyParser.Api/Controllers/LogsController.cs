using Microsoft.AspNetCore.Mvc;
using SharkyParser.Api.Interfaces;
using SharkyParser.Core.Enums;

namespace SharkyParser.Api.Controllers;

/// <summary>
/// API endpoints for log file parsing and analysis.
/// All business logic is delegated to ILogParsingService (SRP).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogParsingService _parsingService;
    private readonly ILogger<LogsController> _logger;

    private const int MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    public LogsController(
        ILogParsingService parsingService,
        ILogger<LogsController> logger)
    {
        _parsingService = parsingService;
        _logger = logger;
    }

    [HttpGet("types")]
    public IActionResult GetTypes()
    {
        var types = _parsingService.GetAvailableLogTypes();
        return Ok(types);
    }

    [HttpPost("parse")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
    public async Task<IActionResult> Parse(
        [FromForm] IFormFile file,
        [FromForm] string logType = "Installation",
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest($"File size exceeds limit of {MaxFileSizeBytes / (1024 * 1024)} MB");

        if (!Enum.TryParse<LogType>(logType, ignoreCase: true, out var type))
            return BadRequest($"Invalid log type. Available: {string.Join(", ", Enum.GetNames<LogType>())}");

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _parsingService.ParseFileAsync(stream, file.FileName, type, ct);
            return Ok(result);
        }

        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse log file");
            return StatusCode(500, "Failed to parse log file");
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        var history = await _parsingService.GetRecentFilesAsync(20, ct);
        return Ok(history);
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "Active", database = "Connected" });
}

