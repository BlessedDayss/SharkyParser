using Spectre.Console;

namespace SharkyParser.Cli.UI;

public static class BannerRenderer
{
    public static void Show()
    {
        AnsiConsole.Write(
            new FigletText("SHARKY PARSER")
                .Centered()
                .Color(Color.Aqua)
        );
    }
}
