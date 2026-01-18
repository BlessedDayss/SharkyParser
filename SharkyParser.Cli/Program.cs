using SharkyParser.Cli.Infrastructure;
using SharkyParser.Cli.UI;
using Spectre.Console;
using Spectre.Console.Cli;

// Skip UI when --json or --embedded flag is present (for programmatic use)
var isQuietMode = args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase) || 
                                 a.Equals("--embedded", StringComparison.OrdinalIgnoreCase));

if (!isQuietMode)
{
    SpinnerLoader.ShowStartup();
    BannerRenderer.Show();
    TipsRenderer.Show();
}

var services = Startup.ConfigureServices();
var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);
app.Configure(Startup.ConfigureCommands);

if (args.Length > 0)
{
    return app.Run(args);
}

var history = new CommandHistory();

while (true)
{
    var input = InputReader.ReadLineWithHistory(history, "> ");

    if (string.IsNullOrWhiteSpace(input))
        continue;

    var command = input.Trim().ToLower();

    if (command is "exit" or "quit" or "q")
    {
        AnsiConsole.MarkupLine("[grey]Goodbye![/]");
        break;
    }

    if (command is "/help" or "help" or "?")
    {
        app.Run(new[] { "--help" });
        continue;
    }

    var commandArgs = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    app.Run(commandArgs);
}

return 0;