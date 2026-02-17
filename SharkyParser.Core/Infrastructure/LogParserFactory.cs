using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Core.Infrastructure;

/// <summary>
/// Creates parsers via the registry's factory delegates and optionally configures them.
/// No IServiceProvider dependency — DIP-compliant.
/// Uses IConfigurableParser instead of concrete type checks — OCP/LSP-compliant.
/// </summary>
public class LogParserFactory : ILogParserFactory
{
    private readonly ILogParserRegistry _registry;
    private readonly ILogger _logger;

    public LogParserFactory(ILogParserRegistry registry, ILogger logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public ILogParser CreateParser(LogType logType)
        => CreateParser(logType, StackTraceMode.AllToStackTrace);

    public ILogParser CreateParser(LogType logType, StackTraceMode stackTraceMode)
    {
        if (!_registry.IsRegistered(logType))
        {
            _logger.LogError($"No parser registered for log type: {logType}");
            throw new ArgumentException($"No parser registered for log type: {logType}");
        }

        try
        {
            var parser = _registry.CreateParser(logType);

            if (parser is IConfigurableParser configurable)
                configurable.Configure(stackTraceMode);

            _logger.LogInfo($"Created parser '{parser.ParserName}' for type {logType}");
            return parser;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to create parser for type {logType}", ex);
            throw;
        }
    }

    public IEnumerable<LogType> GetAvailableTypes() => _registry.GetRegisteredTypes();
}
