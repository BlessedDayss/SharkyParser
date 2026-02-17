using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Core.Infrastructure;

/// <summary>
/// Maps LogType → parser factory delegate.
/// Does NOT self-register parsers (that is a configuration responsibility
/// belonging to the composition root — Program.cs / Startup).
/// </summary>
public class LogParserRegistry : ILogParserRegistry
{
    private readonly Dictionary<LogType, Func<ILogParser>> _registry = new();

    public void Register(LogType logType, Func<ILogParser> parserFactory)
    {
        if (parserFactory is null)
            throw new ArgumentNullException(nameof(parserFactory));

        _registry[logType] = parserFactory;
    }

    public bool IsRegistered(LogType logType) => _registry.ContainsKey(logType);

    public IEnumerable<LogType> GetRegisteredTypes() => _registry.Keys;

    public ILogParser CreateParser(LogType logType)
    {
        if (!_registry.TryGetValue(logType, out var factory))
            throw new ArgumentException($"No parser registered for log type: {logType}");

        return factory();
    }
}
