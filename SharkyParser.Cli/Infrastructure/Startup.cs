using SharkyParser.Cli.Commands;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.Infrastructure;

/// <summary>
/// Configures the Spectre.Console command tree.
/// Service registration lives in Program.cs (composition root).
/// </summary>
public static class Startup
{
    public static void ConfigureCommands(IConfigurator config)
    {
        config.SetApplicationName("sharky");

        config.AddCommand<ParseCommand>("parse")
            .WithDescription("Parse log file and display entries in table format")
            .WithExample(["parse", "path/to/log.log", "-t", "installation"])
            .WithExample(["parse", "path/to/log.log", "-t", "update", "-f", "error"]);

        config.AddCommand<AnalyzeCommand>("analyze")
            .WithDescription("Analyze log file and show statistics (errors, warnings, etc.)")
            .WithExample(["analyze", "path/to/log.log", "-t", "installation"])
            .WithExample(["analyze", "path/to/log.log", "-t", "iis"])
            .WithExample(["analyze", "path/to/log.log", "-t", "teamcity"]);
    }
}
