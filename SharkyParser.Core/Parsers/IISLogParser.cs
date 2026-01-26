using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;

namespace SharkyParser.Core.Parsers;

public class IISLogParser : BaseLogParser
{
    public override LogType SupportedLogType => LogType.IIS;
    public override string ParserName => "IIS Logs";
    public override string ParserDescription => "Parses IIS Logs";
    protected override LogEntry? ParseLineCore(string line)
    {
        throw new NotImplementedException();
    }

    public IISLogParser (IAppLogger logger) : base(logger) { }
    
}