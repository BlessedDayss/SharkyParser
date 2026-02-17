using SharkyParser.Core.Infrastructure;

namespace SharkyParser.Api.Infrastructure;

/// <summary>
/// API logger — delegates to the shared FileAppLogger from Core.
/// Writes to: {TempPath}/SharkyParser/Logs/ApiLog.txt
/// The API host only needs ILogger (LogError / LogInfo) — no CLI lifecycle methods.
/// </summary>
public class ApiLogger : FileAppLogger
{
    public ApiLogger() : base("ApiLog.txt") { }
}
