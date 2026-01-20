using Microsoft.Extensions.DependencyInjection;
using SharkyParser.Cli.Infrastructure;
using SharkyParser.Cli.PreCheck;
using SharkyParser.Core;
using SharkyParser.Core.Infrastructure;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;
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
            
            var runner = serviceProvider.GetRequiredService<ApplicationRunner>();
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
        
        services.AddTransient<InstallationLogParser>();
        services.AddSingleton<ILogParserRegistry, LogParserRegistry>();
        services.AddSingleton<ILogParserFactory, LogParserFactory>();
        services.AddTransient<UpdateLogParser>();
        /*services.AddTransient<RabbitLogParser>();*/
        services.AddTransient<IISLogParser>();
        services.AddSingleton<ILogAnalyzer, LogAnalyzer>();
        
        services.AddSingleton<IAppLogger, AppFileLogger>(); 
        services.AddSingleton<ApplicationModeDetector>();
        
        services.AddSingleton<ICliModeRunner, CliModeRunner>();
        services.AddSingleton<IInteractiveModeRunner, InteractiveModeRunner>();
        services.AddSingleton<IEmbeddedModeRunner, EmbeddedModeRunner>();
        
        services.AddSingleton<ApplicationRunner>();
        
        services.AddSingleton<CommandApp>(_ => 
        {
            var registrar = new TypeRegistrar(services);
            var app = new CommandApp(registrar);
            app.Configure(Startup.ConfigureCommands);
            return app;
        });
        
        return services;
    }
}