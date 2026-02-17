namespace SharkyParser.Core.Interfaces;

/// <summary>
/// Minimal logging contract for Core domain components.
/// All hosts (CLI, API, etc.) provide their own implementation.
/// </summary>
public interface ILogger
{
    void LogError(string message, Exception? ex = null);
    void LogInfo(string message);
}
