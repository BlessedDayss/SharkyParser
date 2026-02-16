namespace SharkyParser.Api.Interfaces;

/// <summary>
/// Abstraction for GitHub OAuth Device Flow authentication.
/// </summary>
public interface IGitHubAuthService
{
    /// <summary>
    /// The current access token, or null if not authenticated.
    /// </summary>
    string? AccessToken { get; }

    /// <summary>
    /// Whether the user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The poll interval recommended by GitHub (in seconds).
    /// </summary>
    int PollInterval { get; }

    /// <summary>
    /// Step 1: Request a device code from GitHub.
    /// Returns user_code and verification_uri for the user.
    /// </summary>
    Task<DeviceCodeResponse> RequestDeviceCodeAsync(CancellationToken ct = default);

    /// <summary>
    /// Step 2: Poll GitHub for the access token after user has entered the code.
    /// Returns the status of the authorization.
    /// </summary>
    Task<PollResult> PollForTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// Clear the stored token (logout).
    /// </summary>
    void Logout();
}

/// <summary>
/// Result of a device code request — data the user needs to complete authentication.
/// </summary>
public record DeviceCodeResponse(string UserCode, string VerificationUri, int ExpiresIn);

/// <summary>
/// Result of polling for an OAuth token.
/// </summary>
public record PollResult(string Status, string Message);
