using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;

namespace SharkyParser.Core.Infrastructure;

public class LogParserRegistry : ILogParserRegistry
{
    private readonly Dictionary<LogType, Type> _registry = new();
    private readonly IAppLogger _logger;
    
    public LogParserRegistry(IAppLogger logger)
    {
        _logger = logger;
        RegisterDefaultParsers();
    }
    
    private void RegisterDefaultParsers()
    {
        Register(LogType.Installation, typeof(InstallationLogParser));
        Register(LogType.Update, typeof(UpdateLogParser));
        /*Register(LogType.RabbitMq, typeof(RabbitLogParser));*/
        Register(LogType.IIS, typeof(IISLogParser));
        
        _logger.LogInfo("Registered 4 default log parsers");
    }
    
    public void Register(LogType logType, Type parserType)
    {
        if (!typeof(ILogParser).IsAssignableFrom(parserType))
        {
            throw new ArgumentException($"Type {parserType} does not implement ILogParser");
        }

        _registry[logType] = parserType;
        _logger.LogInfo($"Registered parser {parserType.Name} for log type {logType}");
    }
    
    public bool IsRegistered(LogType logType) => _registry.ContainsKey(logType);
    
    public IEnumerable<LogType> GetRegisteredTypes() => _registry.Keys;
    
    public Type GetParserType(LogType logType)
    {
        if (!_registry.TryGetValue(logType, out var type))
        {
            throw new ArgumentException($"No parser registered for log type: {logType}");
        }
        return type;
    }
}