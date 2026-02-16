using System.Text;

namespace SharkyParser.Cli.UI;

public static class InputReader
{
    public static string ReadLineWithHistory(CommandHistory history, string prompt)
    {
        Console.Write(prompt);
        var context = new InputReaderContext(history, prompt);

        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);
            var result = context.ProcessKey(keyInfo);
            if (result != null)
            {
                return result;
            }
        }
    }

    private sealed class InputReaderContext
    {
        private readonly CommandHistory _history;
        private readonly string _prompt;
        private readonly StringBuilder _input = new();
        private int _cursorPosition;

        public InputReaderContext(CommandHistory history, string prompt)
        {
            _history = history;
            _prompt = prompt;
        }

        public string? ProcessKey(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    return HandleEnter();
                case ConsoleKey.UpArrow:
                    HandleUpArrow();
                    break;
                case ConsoleKey.DownArrow:
                    HandleDownArrow();
                    break;
                case ConsoleKey.LeftArrow:
                    HandleLeftArrow();
                    break;
                case ConsoleKey.RightArrow:
                    HandleRightArrow();
                    break;
                case ConsoleKey.Home:
                    HandleHome();
                    break;
                case ConsoleKey.End:
                    HandleEnd();
                    break;
                case ConsoleKey.Backspace:
                    HandleBackspace();
                    break;
                case ConsoleKey.Delete:
                    HandleDelete();
                    break;
                case ConsoleKey.Escape:
                    HandleEscape();
                    break;
                case ConsoleKey.Tab:
                    break; 
                default:
                    HandleCharacter(keyInfo.KeyChar);
                    break;
            }

            return null;
        }

        private string HandleEnter()
        {
            Console.WriteLine();
            var result = _input.ToString();
            if (!string.IsNullOrWhiteSpace(result))
            {
                _history.Add(result);
            }
            _history.ResetNavigation();
            return result;
        }

        private void HandleUpArrow()
        {
            var previous = _history.GetPrevious();
            if (previous != null)
            {
                ReplaceInput(previous);
            }
        }

        private void HandleDownArrow()
        {
            var next = _history.GetNext();
            if (next != null)
            {
                ReplaceInput(next);
            }
        }

        private void HandleLeftArrow()
        {
            if (_cursorPosition > 0)
            {
                _cursorPosition--;
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
            }
        }

        private void HandleRightArrow()
        {
            if (_cursorPosition < _input.Length)
            {
                _cursorPosition++;
                Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
            }
        }

        private void HandleHome()
        {
            Console.SetCursorPosition(_prompt.Length, Console.CursorTop);
            _cursorPosition = 0;
        }

        private void HandleEnd()
        {
            Console.SetCursorPosition(_prompt.Length + _input.Length, Console.CursorTop);
            _cursorPosition = _input.Length;
        }

        private void HandleBackspace()
        {
            if (_cursorPosition > 0)
            {
                _input.Remove(_cursorPosition - 1, 1);
                _cursorPosition--;
                RedrawLine();
            }
        }

        private void HandleDelete()
        {
            if (_cursorPosition < _input.Length)
            {
                _input.Remove(_cursorPosition, 1);
                RedrawLine();
            }
        }

        private void HandleEscape()
        {
            ClearCurrentLine();
            _input.Clear();
            _cursorPosition = 0;
            Console.Write(_prompt);
        }

        private void HandleCharacter(char c)
        {
            if (char.IsControl(c)) return;

            _input.Insert(_cursorPosition, c);
            _cursorPosition++;

            if (_cursorPosition == _input.Length)
            {
                Console.Write(c);
            }
            else
            {
                RedrawLine();
            }
        }

        private void ReplaceInput(string newText)
        {
            ClearCurrentLine();
            _input.Clear();
            _input.Append(newText);
            _cursorPosition = _input.Length;
            Console.Write(_prompt + _input);
        }

        private void ClearCurrentLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', _prompt.Length + _input.Length + 1));
            Console.SetCursorPosition(0, Console.CursorTop);
        }

        private void RedrawLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(_prompt + _input + " ");
            Console.SetCursorPosition(_prompt.Length + _cursorPosition, Console.CursorTop);
        }
    }
}
