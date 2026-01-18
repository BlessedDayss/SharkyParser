using Microsoft.Extensions.DependencyInjection;
using SharkyParser.Cli.Infrastructure;
using SharkyParser.Cli.PreCheck;
using SharkyParser.Core;
using SharkyParser.Core.Interfaces;
using Spectre.Console.Cli;

namespace SharkyParser.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var services = ConfigureServices();
            using var serviceProvider = services.BuildServiceProvider();
            var registrar = new TypeRegistrar(services);
            var commandApp = new CommandApp(registrar);
            commandApp.Configure(Startup.ConfigureCommands);
            var logger = serviceProvider.GetRequiredService<IAppLogger>();
            var cliRunner = new CliModeRunner(commandApp, logger);
            var interactiveRunner = new InteractiveModeRunner(commandApp, logger);
            var embeddedRunner = new EmbeddedModeRunner(commandApp, logger);
            var modeDetector = serviceProvider.GetRequiredService<ApplicationModeDetector>();
            var runner = new ApplicationRunner(modeDetector, cliRunner, interactiveRunner, embeddedRunner, logger);
            return runner.Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
    }
    
    private static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogParser, LogParser>();
        services.AddSingleton<ILogAnalyzer, LogAnalyzer>();
        services.AddSingleton<ApplicationModeDetector>();
        services.AddSingleton<IAppLogger, AppFileLogger>(); 
        services.AddSingleton<ApplicationRunner>();
        return services;
    }
}