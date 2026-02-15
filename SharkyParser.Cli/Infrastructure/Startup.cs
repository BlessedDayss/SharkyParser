using Microsoft.Extensions.DependencyInjection;
using SharkyParser.Cli.Commands;
using SharkyParser.Core;
using SharkyParser.Core.Interfaces;
using Spectre.Console.Cli;

namespace SharkyParser.Cli.Infrastructure;

public static class Startup
{
    public static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogAnalyzer, LogAnalyzer>();
        return services;
    }

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
            .WithExample(["analyze", "path/to/log.log", "-t", "iis"]);
    }
}
