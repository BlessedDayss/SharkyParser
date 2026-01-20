using SharkyParser.Core.Enums;

namespace SharkyParser.Core.Interfaces;

public interface ILogParserFactory
{
    ILogParser CreateParser(LogType logType);
    ILogParser CreateParser(LogType logType, StackTraceMode stackTraceMode);
    IEnumerable<LogType> GetAvailableTypes();
    ILogParser GetParserForType(LogType logType);
}