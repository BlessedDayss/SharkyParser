using FluentAssertions;
using Moq;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Infrastructure;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;
using Xunit;
using ILogger = SharkyParser.Core.Interfaces.ILogger;

namespace SharkyParser.Tests.Infrastructure;

public class LogParserFactoryTests
{
    [Fact]
    public void CreateParser_WhenLogTypeNotRegistered_ThrowsAndLogs()
    {
        var registry = new Mock<ILogParserRegistry>();
        registry.Setup(r => r.IsRegistered(LogType.Update)).Returns(false);

        var logger = new Mock<ILogger>();
        var factory = new LogParserFactory(registry.Object, logger.Object);

        Action action = () => factory.CreateParser(LogType.Update);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*No parser registered*");

        logger.Verify(
            l => l.LogError(
                It.Is<string>(msg => msg.Contains("No parser registered for log type")),
                It.IsAny<Exception>()),
            Times.Once);
    }

    [Fact]
    public void CreateParser_WhenInstallationParser_ConfiguresStackTraceMode()
    {
        var logger = new Mock<ILogger>();
        var registry = BuildRegistryWithParsers(logger.Object);
        var factory = new LogParserFactory(registry, logger.Object);

        var parser = factory.CreateParser(LogType.Installation, StackTraceMode.NoStackTrace);

        parser.Should().BeAssignableTo<IConfigurableParser>();
        ((IConfigurableParser)parser).GetConfigurationSummary()
            .Should().Contain(StackTraceMode.NoStackTrace.ToString());
    }

    [Fact]
    public void CreateParser_WhenNonConfigurableParser_ReturnsParserWithoutThrowing()
    {
        var logger = new Mock<ILogger>();
        var registry = BuildRegistryWithParsers(logger.Object);
        var factory = new LogParserFactory(registry, logger.Object);

        var parser = factory.CreateParser(LogType.Update, StackTraceMode.NoStackTrace);

        parser.Should().NotBeNull();
        parser.SupportedLogType.Should().Be(LogType.Update);
    }

    [Fact]
    public void CreateParser_WhenRegistryThrows_LogsAndRethrows()
    {
        var registry = new Mock<ILogParserRegistry>();
        registry.Setup(r => r.IsRegistered(LogType.Update)).Returns(true);
        registry.Setup(r => r.CreateParser(LogType.Update))
            .Throws(new InvalidOperationException("registry error"));

        var logger = new Mock<ILogger>();
        var factory = new LogParserFactory(registry.Object, logger.Object);

        Action action = () => factory.CreateParser(LogType.Update);

        action.Should().Throw<InvalidOperationException>();

        logger.Verify(
            l => l.LogError(
                It.Is<string>(msg => msg.Contains("Failed to create parser for type")),
                It.IsAny<Exception>()),
            Times.Once);
    }

    [Fact]
    public void GetAvailableTypes_DelegatesToRegistry()
    {
        var registry = new Mock<ILogParserRegistry>();
        registry.Setup(r => r.GetRegisteredTypes())
            .Returns(new[] { LogType.Installation, LogType.IIS });

        var logger = new Mock<ILogger>();
        var factory = new LogParserFactory(registry.Object, logger.Object);

        var types = factory.GetAvailableTypes().ToList();

        types.Should().BeEquivalentTo(new[] { LogType.Installation, LogType.IIS });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static LogParserRegistry BuildRegistryWithParsers(ILogger logger)
    {
        var registry = new LogParserRegistry();
        registry.Register(LogType.Installation, () => new InstallationLogParser(logger));
        registry.Register(LogType.Update,       () => new UpdateLogParser(logger));
        registry.Register(LogType.IIS,          () => new IISLogParser(logger));
        return registry;
    }
}
