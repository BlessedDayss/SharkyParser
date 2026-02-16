using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SharkyParser.Api.Interfaces;
using SharkyParser.Api.Models;

namespace SharkyParser.Api.Services;

/// <summary>
/// Manages AI agent interactions via GitHub Models API.
/// Implements ICopilotAgentService for proper dependency inversion.
/// </summary>
public sealed class CopilotAgentService : ICopilotAgentService
{
    private readonly ILogger<CopilotAgentService> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _http;
    private readonly IGitHubAuthService _auth;

    private const string DefaultEndpoint = "https://models.inference.ai.azure.com";
    private const string DefaultModel = "gpt-4o";

    public CopilotAgentService(
        ILogger<CopilotAgentService> logger,
        IConfiguration config,
        IHttpClientFactory httpFactory,
        IGitHubAuthService auth)
    {
        _logger = logger;
        _config = config;
        _http = httpFactory.CreateClient("CopilotAgent");
        _auth = auth;
    }

    /// <inheritdoc />
    public AuthStatus GetAuthStatus()
    {
        return new AuthStatus(
            IsAuthenticated: _auth.IsAuthenticated,
            Message: _auth.IsAuthenticated
                ? "Authenticated via GitHub."
                : "Not authenticated. Sign in with GitHub to use the AI Agent."
        );
    }

    /// <inheritdoc />
    public async Task<string> ChatAsync(string message, string? logContext, CancellationToken ct = default)
    {
        var token = _auth.AccessToken;
        if (string.IsNullOrEmpty(token))
        {
            return "⚠ Not authenticated.\n\nPlease sign in with GitHub using the button above to start using the AI Agent.";
        }

        var endpoint = _config["Copilot:Endpoint"] ?? DefaultEndpoint;
        var model = _config["Copilot:Model"] ?? DefaultModel;

        var systemPrompt = BuildSystemPrompt(logContext);

        var body = new ChatCompletionRequest
        {
            Model = model,
            Messages =
            [
                new ChatMessage("system", systemPrompt),
                new ChatMessage("user", message)
            ],
            MaxTokens = 2048,
            Temperature = 0.3
        };

        var json = JsonSerializer.Serialize(body, ChatJsonContext.Default.ChatCompletionRequest);

        var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("GitHub Models API returned {Status}: {Body}", (int)response.StatusCode, errBody);

                if ((int)response.StatusCode is 401 or 403)
                {
                    return "⚠ Authentication failed. Your GitHub Token may be invalid or expired.\n\n" +
                           "Please generate a new token at https://github.com/settings/tokens and update your configuration.";
                }

                return $"The AI service returned an error (HTTP {(int)response.StatusCode}). Please try again later.";
            }

            var resultJson = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize(resultJson, ChatJsonContext.Default.ChatCompletionResponse);

            var content = result?.Choices?.FirstOrDefault()?.Message?.Content;
            return content ?? "No response from AI model.";
        }
        catch (TaskCanceledException)
        {
            return "The request timed out. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call GitHub Models API");
            return "An unexpected error occurred while contacting the AI service. Check the server logs for details.";
        }
    }

    private static string BuildSystemPrompt(string? logContext)
    {
        var prompt = """
            You are SharkyParser AI Agent — a log analysis expert embedded in a developer tool.
            Your job is to help developers understand, debug, and analyze application log files.

            When the user provides logs, you should:
            - Summarize errors and warnings concisely
            - Identify recurring patterns or spikes
            - Explain probable root causes
            - Suggest fixes or next debugging steps
            - Use clear formatting with bullet points and markdown

            If no log context is provided, let the user know they should load a log file first.
            Keep answers concise but thorough. Always respond in English.
            """;

        if (!string.IsNullOrWhiteSpace(logContext))
        {
            prompt += $"\n\n--- LOG DATA ---\n{logContext}\n--- END LOG DATA ---";
        }

        return prompt;
    }
}
