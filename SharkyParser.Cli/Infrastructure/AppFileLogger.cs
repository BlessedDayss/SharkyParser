using SharkyParser.Core.Infrastructure;

namespace SharkyParser.Cli.Infrastructure;

/// <summary>
/// CLI logger — delegates to the shared FileAppLogger from Core.
/// Writes to: {TempPath}/SharkyParser/Logs/AppLog.txt
/// Kept as a thin subclass so CLI can customize behavior in the future if needed.
/// </summary>
public class AppFileLogger : FileAppLogger
{
    public AppFileLogger() : base("AppLog.txt")
    {
    }
}