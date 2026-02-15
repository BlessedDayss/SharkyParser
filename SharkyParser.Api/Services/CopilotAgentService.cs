using GitHub.Copilot.SDK;

namespace SharkyParser.Api.Services;

public sealed class CopilotAgentService : IAsyncDisposable
{
    private readonly ILogger<CopilotAgentService> _logger;
    private readonly IConfiguration _config;
    private CopilotClient? _client;
    private bool _started;

    public CopilotAgentService(ILogger<CopilotAgentService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<string> ChatAsync(string message, string? logContext, CancellationToken ct = default)
    {
        await EnsureStartedAsync(ct);

        var systemPrompt = BuildSystemPrompt(logContext);

        await using var session = await _client!.CreateSessionAsync(new SessionConfig
        {
            Model = _config["Copilot:Model"] ?? "gpt-4o",
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = systemPrompt
            }
        });

        var response = new TaskCompletionSource<string>();
        var content = string.Empty;

        session.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageEvent msg:
                    content = msg.Data.Content;
                    break;
                case SessionIdleEvent:
                    response.TrySetResult(content);
                    break;
                case SessionErrorEvent err:
                    response.TrySetException(new Exception(err.Data.Message));
                    break;
            }
        });

        await session.SendAsync(new MessageOptions { Prompt = message });

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(60));

        try
        {
            return await response.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return "The request timed out. Please try again.";
        }
    }

    private async Task EnsureStartedAsync(CancellationToken ct)
    {
        if (_started) return;

        var token = _config["Copilot:GitHubToken"]
                    ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                    ?? Environment.GetEnvironmentVariable("GH_TOKEN")
                    ?? Environment.GetEnvironmentVariable("COPILOT_GITHUB_TOKEN");

        var options = new CopilotClientOptions
        {
            AutoStart = true,
            UseStdio = true,
        };

        if (!string.IsNullOrEmpty(token))
        {
            options.GithubToken = token;
        }

        var cliUrl = _config["Copilot:CliUrl"];
        if (!string.IsNullOrEmpty(cliUrl))
        {
            options.CliUrl = cliUrl;
        }

        _client = new CopilotClient(options);
        await _client.StartAsync();
        _started = true;
        _logger.LogInformation("Copilot SDK client started successfully");
    }

    private static string BuildSystemPrompt(string? logContext)
    {
        var prompt = @"
You are SharkyParser AI Agent — a log analysis expert.
Your job is to help developers understand, debug, and analyze application log files.

When the user provides logs, you should:
- Summarize errors and warnings concisely
- Identify recurring patterns or spikes
- Explain probable root causes
- Suggest fixes or next debugging steps
- Use clear formatting with bullet points

If no log context is provided, let the user know they need to load a log file first.
Keep answers concise but thorough.";

        if (!string.IsNullOrWhiteSpace(logContext))
        {
            prompt += $"\n\n--- LOG DATA ---\n{logContext}\n--- END LOG DATA ---";
        }

        return prompt;
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            try
            {
                await _client.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping Copilot client");
            }
        }
    }
}
