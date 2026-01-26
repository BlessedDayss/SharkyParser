using FluentAssertions;
using SharkyParser.Core.Configuration;
using SharkyParser.Core.Enums;

namespace SharkyParser.Tests.Core;

public class LogParserConfigurationTests
{
    [Fact]
    public void Defaults_AreInitialized()
    {
        var config = new LogParserConfiguration();
        var parserConfig = new LogParserConfiguration.ParserConfig();

        config.InstallationLogType.Should().Be(LogType.Installation);
        config.ParserConfigs.Should().BeEmpty();

        parserConfig.AssemblyName.Should().BeEmpty();
        parserConfig.TypeName.Should().BeEmpty();
        parserConfig.Enabled.Should().BeTrue();
        parserConfig.Settings.Should().BeEmpty();
    }
}
