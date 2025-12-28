using SharkyParser.Cli.Infrastructure;
using SharkyParser.Cli.UI;
using Spectre.Console.Cli;

// Show startup animation
SpinnerLoader.ShowStartup();

// Show banner and tips
BannerRenderer.Show();
TipsRenderer.Show();

// Run CLI application
var app = new CommandApp();
app.Configure(Startup.Configure);
return app.Run(args);