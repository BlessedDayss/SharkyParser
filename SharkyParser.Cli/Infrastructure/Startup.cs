using SharkyParser.Cli.Commands;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.Infrastructure;

/// <summary>
/// Configures the CLI application with commands.
/// </summary>
public static class Startup
{
    public static void Configure(IConfigurator config)
    {
        config.SetApplicationName("sharky");
        
        config.AddCommand<ParseCommand>("parse")
            .WithDescription("Parse log files and display entries");
        
        config.AddCommand<AnalyzeCommand>("analyze")
            .WithDescription("Analyze log files for errors and health issues");
    }
}
