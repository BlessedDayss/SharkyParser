using System.Collections.Concurrent;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SharkyParser.Core.Parsers;

namespace SharkyParser.Core.Infrastructure;

public class LogParserFactory : ILogParserFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogParserRegistry _registry;
    private readonly IAppLogger _logger;
    private readonly ConcurrentDictionary<LogType, ILogParser> _parserCache = new();

    public LogParserFactory(
        IServiceProvider serviceProvider,
        ILogParserRegistry registry,
        IAppLogger logger)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
        _logger = logger;
    }

    public ILogParser CreateParser(LogType logType)
    {
        return CreateParser(logType, StackTraceMode.AllToStackTrace);
    }

    public ILogParser CreateParser(LogType logType, StackTraceMode stackTraceMode)
    {
        if (_parserCache.TryGetValue(logType, out var cachedParser))
        {
            return cachedParser;
        }

        if (!_registry.IsRegistered(logType))
        {
            _logger.LogError($"No parser registered for log type: {logType}");
            throw new ArgumentException($"No parser registered for log type: {logType}");
        }

        var parserType = _registry.GetParserType(logType);
        
        try
        {
            var parser = (ILogParser)_serviceProvider.GetRequiredService(parserType);
            
            if (parser is InstallationLogParser installationParser)
            {
                installationParser.StackTraceMode = stackTraceMode;
            }
            _parserCache[logType] = parser;
            _logger.LogInfo($"Created parser {parser.ParserName} for type {logType}");
            
            return parser;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to create parser for type {logType}", ex);
            throw;
        }
    }

    public IEnumerable<LogType> GetAvailableTypes() => _registry.GetRegisteredTypes();
    
    public ILogParser GetParserForType(LogType logType) => CreateParser(logType);
}