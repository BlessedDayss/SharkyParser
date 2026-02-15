using SharkyParser.Core.Interfaces;

namespace SharkyParser.Api.Infrastructure;

public class ApiLogger : IAppLogger
{
    private readonly ILogger<ApiLogger> _logger;

    public ApiLogger(ILogger<ApiLogger> logger)
    {
        _logger = logger;
    }

    public void LogAppStart(string[] args) => _logger.LogInformation("Application started with args: {Args}", string.Join(" ", args));
    public void LogModeDetected(string mode) => _logger.LogInformation("Mode detected: {Mode}", mode);
    public void LogFileProcessed(string filePath) => _logger.LogInformation("File processed: {Path}", filePath);
    public void LogCommandExecution(string command, int exitCode) => _logger.LogInformation("Command executed: {Command} with exit code {Code}", command, exitCode);
    public void LogError(string message, Exception? ex = null) => _logger.LogError(ex, "{Message}", message);
    public void LogInfo(string message) => _logger.LogInformation("{Message}", message);
}
