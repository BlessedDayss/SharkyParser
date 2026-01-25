using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Infrastructure;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;

namespace SharkyParser.Tests.Infrastructure;

public class LogParserFactoryTests
{
    [Fact]
    public void CreateParser_WhenLogTypeNotRegistered_ThrowsAndLogs()
    {
        var registry = new Mock<ILogParserRegistry>();
        registry.Setup(r => r.IsRegistered(LogType.Update)).Returns(false);

        var logger = new Mock<IAppLogger>();
        var provider = new ServiceCollection().BuildServiceProvider();

        var factory = new LogParserFactory(provider, registry.Object, logger.Object);

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
    public void CreateParser_CachesParserInstance()
    {
        var logger = new Mock<IAppLogger>();
        var provider = BuildProvider(logger.Object);

        var registry = new LogParserRegistry(logger.Object);
        var factory = new LogParserFactory(provider, registry, logger.Object);

        var first = factory.CreateParser(LogType.Installation);
        var second = factory.CreateParser(LogType.Installation);

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void CreateParser_WhenInstallationParser_SetsStackTraceMode()
    {
        var logger = new Mock<IAppLogger>();
        var provider = BuildProvider(logger.Object);

        var registry = new LogParserRegistry(logger.Object);
        var factory = new LogParserFactory(provider, registry, logger.Object);

        var parser = factory.CreateParser(LogType.Installation, StackTraceMode.NoStackTrace);

        parser.Should().BeOfType<InstallationLogParser>();
        ((InstallationLogParser)parser).StackTraceMode.Should().Be(StackTraceMode.NoStackTrace);
    }

    [Fact]
    public void CreateParser_WhenServiceProviderFails_LogsAndThrows()
    {
        var registry = new Mock<ILogParserRegistry>();
        registry.Setup(r => r.IsRegistered(LogType.Update)).Returns(true);
        registry.Setup(r => r.GetParserType(LogType.Update)).Returns(typeof(UpdateLogParser));

        var logger = new Mock<IAppLogger>();
        var provider = new ServiceCollection().BuildServiceProvider();

        var factory = new LogParserFactory(provider, registry.Object, logger.Object);

        Action action = () => factory.CreateParser(LogType.Update);

        action.Should().Throw<InvalidOperationException>();

        logger.Verify(
            l => l.LogError(
                It.Is<string>(msg => msg.Contains("Failed to create parser for type")),
                It.IsAny<Exception>()),
            Times.Once);
    }

    private static ServiceProvider BuildProvider(IAppLogger logger)
    {
        var services = new ServiceCollection();
        services.AddSingleton(logger);
        services.AddTransient<InstallationLogParser>();
        services.AddTransient<UpdateLogParser>();
        services.AddTransient<IISLogParser>();
        return services.BuildServiceProvider();
    }
}
