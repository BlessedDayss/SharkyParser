using System.Text;

namespace SharkyParser.Cli.UI;

public static class InputReader
{
    public static string ReadLineWithHistory(CommandHistory history, string prompt)
    {
        Console.Write(prompt);
        
        var input = new StringBuilder();
        var cursorPosition = 0;

        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    var result = input.ToString();
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        history.Add(result);
                    }
                    history.ResetNavigation();
                    return result;

                case ConsoleKey.UpArrow:
                    var previous = history.GetPrevious();
                    if (previous != null)
                    {
                        ClearCurrentLine(prompt, input.Length);
                        input.Clear();
                        input.Append(previous);
                        cursorPosition = input.Length;
                        Console.Write(prompt + input);
                    }
                    break;

                case ConsoleKey.DownArrow:
                    var next = history.GetNext();
                    if (next != null)
                    {
                        ClearCurrentLine(prompt, input.Length);
                        input.Clear();
                        input.Append(next);
                        cursorPosition = input.Length;
                        Console.Write(prompt + input);
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (cursorPosition > 0)
                    {
                        cursorPosition--;
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (cursorPosition < input.Length)
                    {
                        cursorPosition++;
                        Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                    }
                    break;

                case ConsoleKey.Home:
                    Console.SetCursorPosition(prompt.Length, Console.CursorTop);
                    cursorPosition = 0;
                    break;

                case ConsoleKey.End:
                    Console.SetCursorPosition(prompt.Length + input.Length, Console.CursorTop);
                    cursorPosition = input.Length;
                    break;

                case ConsoleKey.Backspace:
                    if (cursorPosition > 0)
                    {
                        input.Remove(cursorPosition - 1, 1);
                        cursorPosition--;
                        RedrawLine(prompt, input.ToString(), cursorPosition);
                    }
                    break;

                case ConsoleKey.Delete:
                    if (cursorPosition < input.Length)
                    {
                        input.Remove(cursorPosition, 1);
                        RedrawLine(prompt, input.ToString(), cursorPosition);
                    }
                    break;

                case ConsoleKey.Escape:
                    ClearCurrentLine(prompt, input.Length);
                    input.Clear();
                    cursorPosition = 0;
                    Console.Write(prompt);
                    break;

                case ConsoleKey.Tab:
                    break;

                default:
                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        input.Insert(cursorPosition, keyInfo.KeyChar);
                        cursorPosition++;
                        
                        if (cursorPosition == input.Length)
                        {
                            Console.Write(keyInfo.KeyChar);
                        }
                        else
                        {
                            RedrawLine(prompt, input.ToString(), cursorPosition);
                        }
                    }
                    break;
            }
        }
    }

    private static void ClearCurrentLine(string prompt, int inputLength)
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', prompt.Length + inputLength + 1));
        Console.SetCursorPosition(0, Console.CursorTop);
    }

    private static void RedrawLine(string prompt, string input, int cursorPosition)
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(prompt + input + " ");
        Console.SetCursorPosition(prompt.Length + cursorPosition, Console.CursorTop);
    }
}
