using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharkyParser.Cli;
using SharkyParser.Cli.PreCheck;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;

namespace SharkyParser.Tests;

[Collection("Console")]
public class ProgramTests
{
    [Fact]
    public void Main_WithHelp_ReturnsSuccess()
    {
        var exitCode = Program.Main(["--help"]);

        exitCode.Should().Be(0);
    }

    [Fact]
    public void Main_WithInvalidCommand_ReturnsError()
    {
        var exitCode = Program.Main(["invalid-command-that-does-not-exist"]);

        exitCode.Should().NotBe(0);
    }

    [Fact]
    public void ConfigureServices_RegistersAllRequiredServices()
    {
        // Use reflection to call private ConfigureServices method
        var method = typeof(Program).GetMethod("ConfigureServices",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.Should().NotBeNull();

        var services = method!.Invoke(null, null) as IServiceCollection;

        services.Should().NotBeNull();
        services!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ConfigureServices_AllParserTypesCanBeResolved()
    {
        // Use reflection to call private ConfigureServices method
        var method = typeof(Program).GetMethod("ConfigureServices",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var services = method!.Invoke(null, null) as IServiceCollection;
        using var provider = services!.BuildServiceProvider();

        // Verify all parsers can be resolved
        var installationParser = provider.GetService<InstallationLogParser>();
        var updateParser = provider.GetService<UpdateLogParser>();
        var iisParser = provider.GetService<IISLogParser>();

        installationParser.Should().NotBeNull();
        updateParser.Should().NotBeNull();
        iisParser.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_AllCoreServicesCanBeResolved()
    {
        // Use reflection to call private ConfigureServices method
        var method = typeof(Program).GetMethod("ConfigureServices",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var services = method!.Invoke(null, null) as IServiceCollection;
        using var provider = services!.BuildServiceProvider();

        // Verify all core services can be resolved
        var logParserRegistry = provider.GetService<ILogParserRegistry>();
        var logParserFactory = provider.GetService<ILogParserFactory>();
        var logAnalyzer = provider.GetService<ILogAnalyzer>();
        var appLogger = provider.GetService<IAppLogger>();

        logParserRegistry.Should().NotBeNull();
        logParserFactory.Should().NotBeNull();
        logAnalyzer.Should().NotBeNull();
        appLogger.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_AllModeRunnersCanBeResolved()
    {
        // Use reflection to call private ConfigureServices method
        var method = typeof(Program).GetMethod("ConfigureServices",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var services = method!.Invoke(null, null) as IServiceCollection;
        using var provider = services!.BuildServiceProvider();

        // Verify all mode runners can be resolved
        var cliRunner = provider.GetService<ICliModeRunner>();
        var interactiveRunner = provider.GetService<IInteractiveModeRunner>();
        var embeddedRunner = provider.GetService<IEmbeddedModeRunner>();

        cliRunner.Should().NotBeNull();
        interactiveRunner.Should().NotBeNull();
        embeddedRunner.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ApplicationRunnerCanBeResolved()
    {
        // Use reflection to call private ConfigureServices method
        var method = typeof(Program).GetMethod("ConfigureServices",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var services = method!.Invoke(null, null) as IServiceCollection;
        using var provider = services!.BuildServiceProvider();

        var applicationRunner = provider.GetService<ApplicationRunner>();

        applicationRunner.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ApplicationModeDetectorCanBeResolved()
    {
        // Use reflection to call private ConfigureServices method
        var method = typeof(Program).GetMethod("ConfigureServices",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var services = method!.Invoke(null, null) as IServiceCollection;
        using var provider = services!.BuildServiceProvider();

        var modeDetector = provider.GetService<ApplicationModeDetector>();

        modeDetector.Should().NotBeNull();
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("parse", "--help")]
    [InlineData("analyze", "--help")]
    public void Main_WithDifferentCommands_ExecutesWithoutException(params string[] args)
    {
        var action = () => Program.Main(args);

        action.Should().NotThrow();
    }

    [Fact]
    public void Main_WithMissingRequiredArguments_ReturnsError()
    {
        // Parse command without required arguments should return error
        var exitCode = Program.Main(["parse"]);

        exitCode.Should().NotBe(0);
    }

    [Fact]
    public void Main_Integration_ParseCommand_WithNonExistentFile_ReturnsError()
    {
        var exitCode = Program.Main(["parse", "non-existent-file.log", "--type", "installation"]);

        exitCode.Should().NotBe(0);
    }

    [Fact]
    public void Main_Integration_AnalyzeCommand_WithNonExistentFile_ReturnsError()
    {
        var exitCode = Program.Main(["analyze", "non-existent-file.log", "--type", "installation"]);

        exitCode.Should().NotBe(0);
    }
}
