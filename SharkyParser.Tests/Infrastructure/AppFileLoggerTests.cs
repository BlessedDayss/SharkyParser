using FluentAssertions;
using SharkyParser.Cli.Infrastructure;

namespace SharkyParser.Tests.Infrastructure;

[Collection("Console")]
public class AppFileLoggerTests
{
    [Fact]
    public void LogMethods_WriteEntriesToFile()
    {
        var logDir = Path.Combine(Path.GetTempPath(), "SharkyParserTests", Guid.NewGuid().ToString("N"));
        var logPath = Path.Combine(logDir, "app.log");
        var logger = new AppFileLogger(logPath);

        try
        {
            logger.LogAppStart(["arg1", "arg2"]);
            logger.LogModeDetected("Cli");
            logger.LogFileProcessed("file.log");
            logger.LogCommandExecution("parse file.log", 7);
            logger.LogInfo("Info message");
            logger.LogError("No exception");
            logger.LogError("Something broke", new InvalidOperationException("boom"));

            File.Exists(logPath).Should().BeTrue();
            Directory.Exists(logDir).Should().BeTrue();

            var contents = File.ReadAllText(logPath);
            contents.Should().Contain("Application started with args: arg1 arg2");
            contents.Should().Contain("Mode detected: Cli");
            contents.Should().Contain("File processed: file.log");
            contents.Should().Contain("Command executed: parse file.log with exit code 7");
            contents.Should().Contain("Info message");
            contents.Should().Contain("ERROR");
            contents.Should().Contain("Something broke");
            contents.Should().Contain("Exception: boom");
        }
        finally
        {
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }

            if (Directory.Exists(logDir))
            {
                Directory.Delete(logDir, recursive: true);
            }
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

        var logger = new AppFileLogger(logPath);

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

        errorWriter.ToString().Should().Contain("Failed to log");
    }
}
