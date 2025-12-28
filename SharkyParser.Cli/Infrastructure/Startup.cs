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
        services.AddSingleton<ILogParser, LogParser>();
        services.AddSingleton<ILogAnalyzer, LogAnalyzer>();
        return services;
    }

    public static void ConfigureCommands(IConfigurator config)
    {
        config.SetApplicationName("sharky");
        config.AddCommand<ParseCommand>("parse");
        config.AddCommand<AnalyzeCommand>("analyze");
    }
}
