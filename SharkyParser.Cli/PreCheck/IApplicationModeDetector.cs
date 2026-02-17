namespace SharkyParser.Cli.PreCheck;

public interface IApplicationModeDetector
{
    ApplicationMode DetermineMode(string[] args);
}
