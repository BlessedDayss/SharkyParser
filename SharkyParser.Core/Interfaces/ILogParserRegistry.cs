using SharkyParser.Core.Enums;

namespace SharkyParser.Core.Interfaces;

public interface ILogParserRegistry
{
    void Register(LogType logType, Type parserType);
    bool IsRegistered(LogType logType);
    IEnumerable<LogType> GetRegisteredTypes();
    Type GetParserType(LogType logType);
}