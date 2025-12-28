using SharkyParser.Cli.Infrastructure;
using SharkyParser.Cli.UI;
using Spectre.Console;
using Spectre.Console.Cli;

SpinnerLoader.ShowStartup();
BannerRenderer.Show();
TipsRenderer.Show();

var services = Startup.ConfigureServices();
var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);
app.Configure(Startup.ConfigureCommands);

if (args.Length > 0)
{
    return app.Run(args);
}

while (true)
{
    var input = AnsiConsole.Prompt(
        new TextPrompt<string>("[aqua]>[/]")
            .AllowEmpty());

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