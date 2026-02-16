using Microsoft.AspNetCore.Mvc;
using SharkyParser.Api.DTOs;
using SharkyParser.Api.Interfaces;

namespace SharkyParser.Api.Controllers;

/// <summary>
/// Handles AI agent chat interactions.
/// Single responsibility: chat endpoint only.
/// </summary>
[ApiController]
[Route("api/agent")]
public class ChatController : ControllerBase
{
    private readonly ICopilotAgentService _agentService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ICopilotAgentService agentService, ILogger<ChatController> logger)
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
            return StatusCode(500, new { error = "AI Agent is temporarily unavailable." });
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        var status = _agentService.GetAuthStatus();
        return Ok(new
        {
            status = "Active",
            authenticated = status.IsAuthenticated,
            message = status.Message
        });
    }
}
