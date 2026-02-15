using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharkyParser.Api.Services;

/// <summary>
/// Manages GitHub OAuth Device Flow authentication.
/// Token is stored in memory for the lifetime of the application.
/// </summary>
public sealed class GitHubAuthService
{
    private readonly ILogger<GitHubAuthService> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;

    private string? _accessToken;
    private string? _pendingDeviceCode;
    private int _pollInterval = 5;

    // GitHub CLI public OAuth App client ID (public, safe to embed)
    private const string DefaultClientId = "Iv1.b507a08c87ecfe98";

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

    public string? AccessToken => _accessToken;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

    /// <summary>
    /// Step 1: Request a device code from GitHub.
    /// Returns user_code and verification_uri for the user.
    /// </summary>
    public async Task<DeviceCodeResponse> RequestDeviceCodeAsync(CancellationToken ct = default)
    {
        var clientId = _config["GitHub:ClientId"] ?? DefaultClientId;

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["scope"] = "read:user"
        });

        var response = await _http.PostAsync("https://github.com/login/device/code", body, ct);
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

    /// <summary>
    /// Step 2: Poll GitHub for the access token after user has entered the code.
    /// Returns the status of the authorization.
    /// </summary>
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

        var response = await _http.PostAsync("https://github.com/login/oauth/access_token", body, ct);
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

    /// <summary>
    /// Logout — clear the stored token.
    /// </summary>
    public void Logout()
    {
        _accessToken = null;
        _pendingDeviceCode = null;
        _logger.LogInformation("GitHub token cleared (logout)");
    }

    public int PollInterval => _pollInterval;
}

// ── DTOs ───────────────────────────────────────────────────

public record DeviceCodeResponse(string UserCode, string VerificationUri, int ExpiresIn);
public record PollResult(string Status, string Message);

internal class DeviceCodeRaw
{
    [JsonPropertyName("device_code")] public string? DeviceCode { get; set; }
    [JsonPropertyName("user_code")] public string? UserCode { get; set; }
    [JsonPropertyName("verification_uri")] public string? VerificationUri { get; set; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("interval")] public int Interval { get; set; }
}

internal class TokenPollRaw
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("token_type")] public string? TokenType { get; set; }
    [JsonPropertyName("scope")] public string? Scope { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
}
