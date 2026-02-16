using SharkyParser.Core.Infrastructure;

namespace SharkyParser.Api.Infrastructure;

/// <summary>
/// API logger — delegates to the shared FileAppLogger from Core.
/// Writes to: {TempPath}/SharkyParser/Logs/ApiLog.txt
/// Kept as a thin subclass so API can customize behavior in the future if needed.
/// </summary>
public class ApiLogger : FileAppLogger
{
    public ApiLogger() : base("ApiLog.txt")
    {
    }
}
