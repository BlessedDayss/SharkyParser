using SharkyParser.Core;

if (args.Length == 0)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  sharkylog parse <path-to-log-file>");
    return;
}

var command = args[0];

if (command == "parse")
{
    if (args.Length < 2)
    {
        Console.WriteLine("Please specify log file path");
        return;
    }

    var path = args[1];

    if (!File.Exists(path))
    {
        Console.WriteLine($"File not found: {path}");
        return;
    }

    var parser = new LogParser();
    var logs = parser.ParseFile(path);

    foreach (var log in logs)
    {
        Console.WriteLine($"{log.Timestamp} | {log.Level} | {log.Source} | {log.Message}");
    }
}
else
{
    Console.WriteLine($"Unknown command: {command}");
}