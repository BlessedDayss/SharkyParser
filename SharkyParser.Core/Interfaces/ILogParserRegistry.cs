using SharkyParser.Core.Enums;

namespace SharkyParser.Core.Interfaces;

/// <summary>
/// Maps LogType to a parser factory delegate.
/// Parsers are registered from the composition root (Program.cs / Startup),
/// not from within the registry itself — keeping SRP clean.
/// </summary>
public interface ILogParserRegistry
{
    /// <summary>Registers a factory delegate for the given log type.</summary>
    void Register(LogType logType, Func<ILogParser> parserFactory);

    bool IsRegistered(LogType logType);
    IEnumerable<LogType> GetRegisteredTypes();

    /// <summary>Creates (or returns) a parser for the given type via the registered factory.</summary>
    ILogParser CreateParser(LogType logType);
}
