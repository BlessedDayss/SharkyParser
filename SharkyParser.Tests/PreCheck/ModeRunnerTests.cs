using FluentAssertions;
using Moq;
using SharkyParser.Cli.PreCheck;
using SharkyParser.Core.Interfaces;
using Spectre.Console.Cli;

namespace SharkyParser.Tests.PreCheck;

[Collection("Console")]
public class ModeRunnerTests
{
    [Fact]
    public void CliModeRunner_InvokesCommandAndLogs()
    {
        var logger = new Mock<IAppLogger>();
        var app = BuildCommandApp();
        var runner = new CliModeRunner(app, logger.Object);

        var result = runner.Run(["noop"]);

        result.Should().Be(5);
        logger.Verify(l => l.LogInfo(It.Is<string>(msg => msg.Contains("CLI mode"))), Times.Once);
        logger.Verify(l => l.LogCommandExecution("noop", 5), Times.Once);
    }

    [Fact]
    public void EmbeddedModeRunner_InvokesCommandAndLogs()
    {
        var logger = new Mock<IAppLogger>();
        var app = BuildCommandApp();
        var runner = new EmbeddedModeRunner(app, logger.Object);

        var result = runner.Run(["noop"]);

        result.Should().Be(5);
        logger.Verify(l => l.LogInfo(It.Is<string>(msg => msg.Contains("Embedded mode"))), Times.Once);
        logger.Verify(l => l.LogCommandExecution("noop", 5), Times.Once);
    }

    private static CommandApp BuildCommandApp()
    {
        var app = new CommandApp();
        app.Configure(config => config.AddCommand<NoopCommand>("noop"));
        return app;
    }

    private sealed class NoopCommand : Command<NoopCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
        }

        protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
        {
            return 5;
        }
    }
}
