using Microsoft.AspNetCore.Mvc;
using SharkyParser.Api.Interfaces;

namespace SharkyParser.Api.Controllers;

/// <summary>
/// Handles GitHub OAuth Device Flow authentication.
/// Single responsibility: auth endpoints only.
/// </summary>
[ApiController]
[Route("api/agent/auth")]
public class AuthController : ControllerBase
{
    private readonly IGitHubAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IGitHubAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("status")]
    public IActionResult AuthStatus()
    {
        var isAuthenticated = _authService.IsAuthenticated;
        var message = isAuthenticated
            ? "Authenticated via GitHub."
            : "Not authenticated. Sign in with GitHub to use the AI Agent.";

        return Ok(new { authenticated = isAuthenticated, message });
    }

    [HttpPost("device-code")]
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

    [HttpPost("poll")]
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

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _authService.Logout();
        return Ok(new { message = "Logged out successfully." });
    }
}
