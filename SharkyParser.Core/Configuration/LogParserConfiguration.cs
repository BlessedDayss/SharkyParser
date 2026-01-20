using SharkyParser.Core.Enums;

namespace SharkyParser.Core.Configuration;

public class LogParserConfiguration
{
    public LogType InstallationLogType { get; set; } = LogType.Installation;
    public Dictionary<LogType, ParserConfig> ParserConfigs { get; set; } = new();

    public class ParserConfig
    {
        public string AssemblyName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public Dictionary<string, object> Settings { get; set; } = new();
    }
}