using Microsoft.Extensions.DependencyInjection;
using SharkyParser.Cli.Infrastructure;
using SharkyParser.Cli.Interfaces;
using SharkyParser.Cli.PreCheck;
using SharkyParser.Core;
using SharkyParser.Core.Enums;
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
            using var provider = ConfigureServices().BuildServiceProvider();
            var runner = provider.GetRequiredService<ApplicationRunner>();
            return runner.Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
    }

    private static ServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // ── Logger ────────────────────────────────────────────────────────────
        // A single AppFileLogger instance satisfies both ILogger (Core) and IAppLogger (CLI).
        services.AddSingleton<AppFileLogger>();
        services.AddSingleton<ILogger>(sp  => sp.GetRequiredService<AppFileLogger>());
        services.AddSingleton<IAppLogger>(sp => sp.GetRequiredService<AppFileLogger>());

        // ── Parsers ────────────────────────────────────────────────────────────
        // InstallationLogParser is Transient: each CreateParser call returns a fresh
        // instance so StackTraceMode configured via IConfigurableParser is isolated
        // per-command invocation.
        // UpdateLogParser / IISLogParser are Singleton: stateless (Update) or
        // per-file state is reset at ParseFile start (IIS).
        services.AddTransient<InstallationLogParser>();
        services.AddSingleton<UpdateLogParser>();
        services.AddSingleton<IISLogParser>();

        // ── Core services ──────────────────────────────────────────────────────
        // ILogParserRegistry is registered as a factory so that EVERY ServiceProvider
        // that is built from this collection (the main one AND Spectre's internal
        // TypeRegistrar provider) gets its own properly populated registry.
        // Capturing 'sp' here refers to the specific provider that resolves this
        // singleton — so both containers work correctly.
        services.AddSingleton<ILogParserRegistry>(sp =>
        {
            var registry = new LogParserRegistry();
            registry.Register(LogType.Installation, () => sp.GetRequiredService<InstallationLogParser>());
            registry.Register(LogType.Update,        () => sp.GetRequiredService<UpdateLogParser>());
            registry.Register(LogType.IIS,           () => sp.GetRequiredService<IISLogParser>());
            return registry;
        });

        services.AddSingleton<ILogParserFactory, LogParserFactory>();
        services.AddSingleton<ILogAnalyzer, LogAnalyzer>();

        // ── CLI infrastructure ─────────────────────────────────────────────────
        services.AddSingleton<IApplicationModeDetector, ApplicationModeDetector>();
        services.AddSingleton<ICliModeRunner, CliModeRunner>();
        services.AddSingleton<IInteractiveModeRunner, InteractiveModeRunner>();
        services.AddSingleton<IEmbeddedModeRunner, EmbeddedModeRunner>();
        services.AddSingleton<ApplicationRunner>();

        // CommandApp is built last so TypeRegistrar captures the fully configured
        // services collection (all registrations above are already in place).
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
