using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharkyParser.Cli.Infrastructure;
using SharkyParser.Core;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Interfaces;
using Spectre.Console.Cli;

namespace SharkyParser.Tests.Infrastructure;

[Collection("Console")]
public class StartupTests
{
    /// <summary>
    /// Verifies that Startup.ConfigureCommands wires up the command tree correctly
    /// and the app can respond to --help without throwing.
    /// </summary>
    [Fact]
    public void ConfigureCommands_AllowsHelpExecution()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogAnalyzer, LogAnalyzer>();
        services.AddSingleton<ILogParserFactory, FakeLogParserFactory>();

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);
        app.Configure(Startup.ConfigureCommands);

        var exitCode = app.Run(["--help"]);

        exitCode.Should().Be(0);
    }

    private sealed class FakeLogParserFactory : ILogParserFactory
    {
        public ILogParser CreateParser(LogType logType) => throw new NotImplementedException();
        public ILogParser CreateParser(LogType logType, StackTraceMode stackTraceMode) => throw new NotImplementedException();
        public IEnumerable<LogType> GetAvailableTypes() => Array.Empty<LogType>();
    }
}
