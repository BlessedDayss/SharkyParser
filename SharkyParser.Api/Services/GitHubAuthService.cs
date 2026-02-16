using System.Text.Json;
using SharkyParser.Api.Interfaces;
using SharkyParser.Api.Models;

namespace SharkyParser.Api.Services;

/// <summary>
/// Manages GitHub OAuth Device Flow authentication.
/// Token is stored in memory for the lifetime of the application.
/// Implements IGitHubAuthService for proper dependency inversion.
/// </summary>
public sealed class GitHubAuthService : IGitHubAuthService
{
    private readonly ILogger<GitHubAuthService> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    private string? _accessToken;
    private string? _pendingDeviceCode;
    private int _pollInterval = 5;

    // GitHub CLI public OAuth App client ID (public, safe to embed)
    private const string DefaultClientId = "Iv1.b507a08c87ecfe98";

    private const string GitHubDeviceCodeUrl = "https://github.com/login/device/code";
    private const string GitHubTokenUrl = "https://github.com/login/oauth/access_token";

    public GitHubAuthService(ILogger<GitHubAuthService> logger, IConfiguration config, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _config = config;
        _http = httpFactory.CreateClient("GitHubAuth");
        _http.DefaultRequestHeaders.Add("Accept", "application/json");

        // Try to pick up token from config/env on startup
        var envToken = config["Copilot:GitHubToken"]
                       ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                       ?? Environment.GetEnvironmentVariable("GH_TOKEN")
                       ?? Environment.GetEnvironmentVariable("COPILOT_GITHUB_TOKEN");

        if (!string.IsNullOrEmpty(envToken))
        {
            _accessToken = envToken;
            _logger.LogInformation("GitHub token loaded from environment/config");
        }
    }

    /// <inheritdoc />
    public string? AccessToken => _accessToken;

    /// <inheritdoc />
    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

    /// <inheritdoc />
    public int PollInterval => _pollInterval;

    /// <inheritdoc />
    public async Task<DeviceCodeResponse> RequestDeviceCodeAsync(CancellationToken ct = default)
    {
        var clientId = _config["GitHub:ClientId"] ?? DefaultClientId;

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["scope"] = "read:user"
        });

        var response = await _http.PostAsync(GitHubDeviceCodeUrl, body, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("Device code response: {Json}", json);

        var result = JsonSerializer.Deserialize<DeviceCodeRaw>(json);

        if (result == null || string.IsNullOrEmpty(result.DeviceCode))
        {
            throw new Exception($"Failed to get device code from GitHub: {json}");
        }

        _pendingDeviceCode = result.DeviceCode;
        _pollInterval = result.Interval > 0 ? result.Interval : 5;

        return new DeviceCodeResponse(
            UserCode: result.UserCode!,
            VerificationUri: result.VerificationUri!,
            ExpiresIn: result.ExpiresIn
        );
    }

    /// <inheritdoc />
    public async Task<PollResult> PollForTokenAsync(CancellationToken ct = default)
    {
        if (IsAuthenticated)
        {
            return new PollResult("success", "Already authenticated.");
        }

        if (string.IsNullOrEmpty(_pendingDeviceCode))
        {
            return new PollResult("error", "No pending device code. Start the auth flow first.");
        }

        var clientId = _config["GitHub:ClientId"] ?? DefaultClientId;

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["device_code"] = _pendingDeviceCode,
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
        });

        var response = await _http.PostAsync(GitHubTokenUrl, body, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        _logger.LogInformation("GitHub OAuth poll raw response: {Json}", json);

        var result = JsonSerializer.Deserialize<TokenPollRaw>(json);

        if (!string.IsNullOrEmpty(result?.AccessToken))
        {
            _accessToken = result.AccessToken;
            _pendingDeviceCode = null;
            _logger.LogInformation("GitHub token obtained via Device Flow");
            return new PollResult("success", "Authenticated successfully!");
        }

        var error = result?.Error ?? "unknown";

        return error switch
        {
            "authorization_pending" => new PollResult("pending", "Waiting for you to enter the code on GitHub..."),
            "slow_down" => new PollResult("pending", "Waiting... (rate limited, please be patient)"),
            "expired_token" => new PollResult("expired", "The code has expired. Please start the flow again."),
            "access_denied" => new PollResult("denied", "Authorization was denied."),
            _ => new PollResult("error", $"Unexpected response: {error}")
        };
    }

    /// <inheritdoc />
    public void Logout()
    {
        _accessToken = null;
        _pendingDeviceCode = null;
        _logger.LogInformation("GitHub token cleared (logout)");
    }
}
