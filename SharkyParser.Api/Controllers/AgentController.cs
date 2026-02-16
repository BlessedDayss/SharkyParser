using Microsoft.AspNetCore.Mvc;
using SharkyParser.Api.DTOs;
using SharkyParser.Api.Interfaces;

namespace SharkyParser.Api.Controllers;

/// <summary>
/// Handles AI agent chat and GitHub OAuth Device Flow authentication.
/// Depends on abstractions (interfaces) rather than concrete services.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly ICopilotAgentService _agentService;
    private readonly IGitHubAuthService _authService;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        ICopilotAgentService agentService,
        IGitHubAuthService authService,
        ILogger<AgentController> logger)
    {
        _agentService = agentService;
        _authService = authService;
        _logger = logger;
    }

    // ── Chat ───────────────────────────────────────────────

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

    // ── Auth: Device Flow ──────────────────────────────────

    [HttpGet("auth/status")]
    public IActionResult AuthStatus()
    {
        var status = _agentService.GetAuthStatus();
        return Ok(new { authenticated = status.IsAuthenticated, message = status.Message });
    }

    [HttpPost("auth/device-code")]
    public async Task<ActionResult> StartDeviceFlow(CancellationToken ct)
    {
        try
        {
            var result = await _authService.RequestDeviceCodeAsync(ct);
            return Ok(new
            {
                userCode = result.UserCode,
                verificationUri = result.VerificationUri,
                expiresIn = result.ExpiresIn,
                interval = _authService.PollInterval
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start device flow");
            return StatusCode(500, new { error = "Failed to start GitHub authentication." });
        }
    }

    [HttpPost("auth/poll")]
    public async Task<ActionResult> PollForToken(CancellationToken ct)
    {
        try
        {
            var result = await _authService.PollForTokenAsync(ct);
            return Ok(new { status = result.Status, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to poll for token");
            return StatusCode(500, new { error = "Failed to check authentication status." });
        }
    }

    [HttpPost("auth/logout")]
    public IActionResult Logout()
    {
        _authService.Logout();
        return Ok(new { message = "Logged out successfully." });
    }

    // ── Health ─────────────────────────────────────────────

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
