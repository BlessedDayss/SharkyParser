using Microsoft.AspNetCore.Mvc;
using SharkyParser.Api.Data;
using SharkyParser.Api.Interfaces;
using SharkyParser.Core.Enums;

namespace SharkyParser.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogParsingService _parsingService;
    private readonly AppDbContext _db;
    private readonly ILogger<LogsController> _logger;

    private const int MaxFileSizeBytes = 50 * 1024 * 1024;

    public LogsController(
        ILogParsingService parsingService,
        AppDbContext db,
        ILogger<LogsController> logger)
    {
        _parsingService = parsingService;
        _db             = db;
        _logger         = logger;
    }

    [HttpGet("types")]
    public IActionResult GetTypes()
        => Ok(_parsingService.GetAvailableLogTypes());

    [HttpPost("parse")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
    public async Task<IActionResult> Parse(
        [FromForm] IFormFile file,
        [FromForm] string logType = "Installation",
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest($"File size exceeds {MaxFileSizeBytes / (1024 * 1024)} MB.");

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
            return StatusCode(500, "Failed to parse log file.");
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
        => Ok(await _parsingService.GetRecentFilesAsync(20, ct));

    [HttpGet("{id:guid}/entries")]
    public async Task<IActionResult> GetEntries(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _parsingService.GetEntriesAsync(id, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"File record {id} not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load entries for record {Id}", id);
            return StatusCode(500, "Failed to load log entries.");
        }
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        var dbReachable = await _db.Database.CanConnectAsync(ct);
        var status = dbReachable ? "connected" : "unreachable";
        return dbReachable
            ? Ok(new { status = "active", database = status })
            : StatusCode(503, new { status = "degraded", database = status });
    }
}
