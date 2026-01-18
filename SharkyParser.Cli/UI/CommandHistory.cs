using System.Text;

namespace SharkyParser.Cli.UI;

public class CommandHistory
{
    private readonly List<string> _history = [];
    private int _currentIndex = -1;

    public void Add(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        if (_history.Count == 0 || _history[^1] != command)
        {
            _history.Add(command);
        }
        
        _currentIndex = _history.Count;
    }

    public string? GetPrevious()
    {
        if (_history.Count == 0)
            return null;

        if (_currentIndex > 0)
            _currentIndex--;

        return _history[_currentIndex];
    }

    public string? GetNext()
    {
        if (_history.Count == 0)
            return null;

        if (_currentIndex < _history.Count - 1)
        {
            _currentIndex++;
            return _history[_currentIndex];
        }

        _currentIndex = _history.Count;
        return string.Empty;
    }

    public void ResetNavigation()
    {
        _currentIndex = _history.Count;
    }
}
