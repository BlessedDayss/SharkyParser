using Microsoft.AspNetCore.Mvc;

namespace SharkyParser.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChangelogController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ChangelogController> _logger;

    public ChangelogController(IWebHostEnvironment env, ILogger<ChangelogController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<string>> GetChangelog()
    {
        var path = Path.Combine(_env.ContentRootPath, "Changelog.md");
        if (!System.IO.File.Exists(path))
        {
            return Ok("# Changelog\n\nNo changelog available.");
        }
        try
        {
            var content = await System.IO.File.ReadAllTextAsync(path);
            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read changelog");
            return StatusCode(500, "Failed to read changelog");
        }
    }
}
