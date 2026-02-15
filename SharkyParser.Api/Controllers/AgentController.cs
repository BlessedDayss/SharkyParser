using Microsoft.AspNetCore.Mvc;
using SharkyParser.Api.Services;

namespace SharkyParser.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly CopilotAgentService _agentService;
    private readonly ILogger<AgentController> _logger;

    public AgentController(CopilotAgentService agentService, ILogger<AgentController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<ActionResult> Chat([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required" });

        try
        {
            var response = await _agentService.ChatAsync(request.Message, request.LogContext, ct);
            return Ok(new { response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent chat failed");
            return StatusCode(500, new { error = "AI Agent is temporarily unavailable. Ensure the Copilot CLI is installed and a GitHub token is configured." });
        }
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "Active", sdk = "GitHub.Copilot.SDK" });
}

public record ChatRequest(string Message, string? LogContext = null);
