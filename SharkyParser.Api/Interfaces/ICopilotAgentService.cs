namespace SharkyParser.Api.Interfaces{

/// <summary>
/// Abstraction for the AI copilot agent service.
/// Provides chat capabilities and authentication status tracking.
/// </summary>
public interface ICopilotAgentService
{
    /// <summary>
    /// Returns the current authentication status.
    /// </summary>
    AuthStatus GetAuthStatus();

    /// <summary>
    /// Sends a message to the AI agent and returns the response.
    /// </summary>
    Task<string> ChatAsync(string message, string? logContext, CancellationToken ct = default);
}

/// <summary>
/// Represents the current authentication state.
/// </summary>
public record AuthStatus(bool IsAuthenticated, string Message);

}