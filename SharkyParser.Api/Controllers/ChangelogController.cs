using Microsoft.AspNetCore.Mvc;
using SharkyParser.Api.Interfaces;

namespace SharkyParser.Api.Controllers;

/// <summary>
/// Returns the project changelog.
/// All file resolution logic is delegated to IChangelogService (SRP).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChangelogController : ControllerBase
{
    private readonly IChangelogService _changelogService;
    private readonly ILogger<ChangelogController> _logger;

    public ChangelogController(IChangelogService changelogService, ILogger<ChangelogController> logger)
    {
        _changelogService = changelogService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<string>> GetChangelog()
    {
        try
        {
            var content = await _changelogService.GetChangelogAsync();
            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read changelog");
            return StatusCode(500, "Failed to read changelog");
        }
    }
}
