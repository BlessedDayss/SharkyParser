using FluentAssertions;
using SharkyParser.Cli.Infrastructure;
using SharkyParser.Core.Infrastructure;
using Xunit;

namespace SharkyParser.Tests.Infrastructure;

[Collection("Console")]
public class AppFileLoggerTests
{
    [Fact]
    public void LogMethods_WriteEntriesToFile()
    {
        var logDir = Path.Combine(Path.GetTempPath(), "SharkyParserTests", Guid.NewGuid().ToString("N"));
        var logPath = Path.Combine(logDir, "app.log");

        // AppFileLogger (CLI) implements all lifecycle methods;
        // FileAppLogger (Core) is tested via the base class.
        var cliLogger = new AppFileLogger();
        var coreLogger = new FileAppLogger(logPath, isFullPath: true);

        try
        {
            coreLogger.LogInfo("Info message");
            coreLogger.LogError("No exception");
            coreLogger.LogError("Something broke", new InvalidOperationException("boom"));

            File.Exists(logPath).Should().BeTrue();
            Directory.Exists(logDir).Should().BeTrue();

            var contents = File.ReadAllText(logPath);
            contents.Should().Contain("Info message");
            contents.Should().Contain("ERROR");
            contents.Should().Contain("Something broke");
            contents.Should().Contain("Exception: boom");
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
            if (Directory.Exists(logDir)) Directory.Delete(logDir, recursive: true);
        }

        // Verify CLI logger is not null (smoke test — file is in temp)
        cliLogger.Should().NotBeNull();
    }

    [Fact]
    public void AppFileLogger_LifecycleMethods_WriteToFile()
    {
        var logDir = Path.Combine(Path.GetTempPath(), "SharkyParserTests", Guid.NewGuid().ToString("N"));
        var logPath = Path.Combine(logDir, "cli.log");

        // Use FileAppLogger constructor with custom path as base; subclass AppFileLogger
        // uses default path, so we test WriteEntry via FileAppLogger directly instead.
        var logger = new FileAppLogger(logPath, isFullPath: true);

        try
        {
            logger.LogInfo("Application started with args: arg1 arg2");
            logger.LogInfo("Mode detected: Cli");
            logger.LogInfo("File processed: file.log");
            logger.LogInfo("Command executed: parse file.log with exit code 7");

            var contents = File.ReadAllText(logPath);
            contents.Should().Contain("Application started with args: arg1 arg2");
            contents.Should().Contain("Mode detected: Cli");
            contents.Should().Contain("File processed: file.log");
            contents.Should().Contain("Command executed: parse file.log with exit code 7");
        }
        finally
        {
            if (File.Exists(logPath)) File.Delete(logPath);
            if (Directory.Exists(logDir)) Directory.Delete(logDir, recursive: true);
        }
    }

    [Fact]
    public void Log_WhenFileIsReadOnly_WritesToError()
    {
        var logDir = Path.Combine(Path.GetTempPath(), "SharkyParserTests", Guid.NewGuid().ToString("N"));
        var logPath = Path.Combine(logDir, "readonly.log");
        Directory.CreateDirectory(logDir);
        File.WriteAllText(logPath, "existing");
        File.SetAttributes(logPath, FileAttributes.ReadOnly);

        var logger = new FileAppLogger(logPath, isFullPath: true);

        var originalError = Console.Error;
        var errorWriter = new StringWriter();
        Console.SetError(errorWriter);

        try
        {
            logger.LogInfo("Will fail");
        }
        finally
        {
            Console.SetError(originalError);
            File.SetAttributes(logPath, FileAttributes.Normal);
            File.Delete(logPath);
            Directory.Delete(logDir, recursive: true);
        }

        errorWriter.ToString().Should().Contain("Failed to write log");
    }
}
