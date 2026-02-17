namespace SharkyParser.Cli.PreCheck;

public class ApplicationModeDetector : IApplicationModeDetector
{
    public ApplicationMode DetermineMode(string[] args)
    {
        if (args.Any(arg => arg.Equals("--embedded", StringComparison.OrdinalIgnoreCase)))
            return ApplicationMode.Embedded;

        return args.Length > 0 ? ApplicationMode.Cli : ApplicationMode.Interactive;
    }
}
