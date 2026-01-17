using Serilog;
using Spectre.Console.Rendering;
using SoloForge.Console.Models;

namespace SoloForge.Console.Services;

public enum JournalMode
{
    Normal,
    Insert
}

public enum JournalFocus
{
    Left,
    Journal
}

public sealed class JournalService
{
    private readonly ILogger _log = AppLogger.ForContext<JournalService>();
    private readonly List<string> _lines = [];
    private string? _filePath;
    private bool _dirty;
    private bool _autoScroll = true;
    private char? _pendingCommand;
    private IRenderable? _lastLeftContent;
    private string? _lastTitle;
    private string? _lastFooter;
    private int _lastChaos;
    private int _lastCharacters;
    private int _lastThreads;
    private string? _lastCampaign;

    public JournalMode Mode { get; private set; } = JournalMode.Normal;
    public JournalFocus Focus { get; private set; } = JournalFocus.Left;
    public int CursorLine { get; private set; }
    public int CursorColumn { get; private set; }
    public int ScrollTop { get; private set; }
    public int ViewportHeight { get; private set; } = 12;
    public int ViewportWidth { get; private set; } = 40;
    public string? FilePath => _filePath;

    public void Load(string filePath)
    {
        _filePath = filePath;
        _lines.Clear();

        if (File.Exists(filePath))
        {
            var content = File.ReadAllText(filePath);
            var normalized = NormalizeLineEndings(content);
            _lines.AddRange(normalized.Split('\n'));
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, string.Empty);
            _lines.Add(string.Empty);
        }

        if (_lines.Count == 0)
            _lines.Add(string.Empty);

        CursorLine = _lines.Count - 1;
        CursorColumn = _lines[^1].Length;
        ScrollTop = Math.Max(0, _lines.Count - ViewportHeight);
        _dirty = false;
        _autoScroll = true;
        _pendingCommand = null;
    }

    public void SetViewportSize(int width, int height)
    {
        ViewportWidth = Math.Max(width, 10);
        ViewportHeight = Math.Max(height, 6);
        EnsureCursorVisible();
    }

    public void SetRenderState(IRenderable leftContent, string title, int chaos, int characters, int threads, string? campaignName, string? footer)
    {
        _lastLeftContent = leftContent;
        _lastTitle = title;
        _lastChaos = chaos;
        _lastCharacters = characters;
        _lastThreads = threads;
        _lastCampaign = campaignName;
        _lastFooter = footer;
    }

    public bool TryGetRenderState(out IRenderable? leftContent, out string? title, out int chaos, out int characters, out int threads, out string? campaignName, out string? footer)
    {
        leftContent = _lastLeftContent;
        title = _lastTitle;
        chaos = _lastChaos;
        characters = _lastCharacters;
        threads = _lastThreads;
        campaignName = _lastCampaign;
        footer = _lastFooter;
        return leftContent != null && title != null;
    }

    public void SetFocus(JournalFocus focus)
    {
        Focus = focus;
        if (focus == JournalFocus.Left && Mode == JournalMode.Insert)
            Mode = JournalMode.Normal;
    }

    public IReadOnlyList<string> GetVisibleLines()
    {
        EnsureLines();

        if (_autoScroll)
        {
            ScrollTop = Math.Max(0, _lines.Count - ViewportHeight);
        }

        var visible = new List<string>();
        var contentWidth = Math.Max(ViewportWidth, 10);

        for (var index = ScrollTop; index < _lines.Count; index++)
        {
            var line = _lines[index] ?? string.Empty;
            line = line.Replace("\t", "    ");

            if (Focus == JournalFocus.Journal && index == CursorLine)
            {
                var marker = Mode == JournalMode.Insert ? "▋" : "▏";
                var column = Math.Clamp(CursorColumn, 0, line.Length);
                line = line.Insert(column, marker);
            }

            foreach (var wrapped in WrapLine(line, contentWidth))
            {
                visible.Add(wrapped);
                if (visible.Count >= ViewportHeight)
                    return visible;
            }
        }

        while (visible.Count < ViewportHeight)
            visible.Add(string.Empty);

        return visible;
    }

    public string GetHeaderLabel()
    {
        var modeLabel = Mode == JournalMode.Insert ? "INSERT" : "NORMAL";
        var focusLabel = Focus == JournalFocus.Journal ? "*" : "";
        var fileName = _filePath != null ? Path.GetFileName(_filePath) : "journal";
        return $"Journal{focusLabel} [{modeLabel}] {fileName}";
    }

    public bool HandleKey(ConsoleKeyInfo key)
    {
        if (Focus != JournalFocus.Journal)
            return false;

        if (Mode == JournalMode.Insert)
            return HandleInsertKey(key);

        return HandleNormalKey(key);
    }

    public void AppendEntry(LogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(_filePath))
            return;

        var markdown = TemplateService.Instance.ToMarkdown(entry);
        AppendMarkdown(markdown);
        Save();
    }

    public void AppendMarkdown(string markdown)
    {
        EnsureLines();

        if (_lines.Count > 0 && !string.IsNullOrWhiteSpace(_lines[^1]))
            _lines.Add(string.Empty);

        var normalized = NormalizeLineEndings(markdown);
        var parts = normalized.Split('\n');
        _lines.AddRange(parts);
        _lines.Add(string.Empty);

        CursorLine = _lines.Count - 1;
        CursorColumn = 0;
        _autoScroll = true;
        _dirty = true;
    }

    public void SaveIfDirty()
    {
        if (_dirty)
            Save();
    }

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
            return;

        try
        {
            var content = string.Join('\n', _lines);
            File.WriteAllText(_filePath, content);
            _dirty = false;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to save journal file: {Path}", _filePath);
        }
    }

    private bool HandleNormalKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.I)
        {
            Mode = JournalMode.Insert;
            _pendingCommand = null;
            return true;
        }

        if (key.KeyChar == 'g')
        {
            if (_pendingCommand == 'g')
            {
                JumpToStart();
                _pendingCommand = null;
            }
            else
            {
                _pendingCommand = 'g';
            }
            return true;
        }

        if (key.KeyChar == 'G')
        {
            JumpToEnd();
            _pendingCommand = null;
            return true;
        }

        _pendingCommand = null;

        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            switch (key.Key)
            {
                case ConsoleKey.F:
                    PageDown(ViewportHeight);
                    return true;
                case ConsoleKey.B:
                    PageUp(ViewportHeight);
                    return true;
                case ConsoleKey.D:
                    PageDown(Math.Max(1, ViewportHeight / 2));
                    return true;
                case ConsoleKey.U:
                    PageUp(Math.Max(1, ViewportHeight / 2));
                    return true;
            }
        }

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                MoveCursor(-1, 0);
                return true;
            case ConsoleKey.DownArrow:
                MoveCursor(1, 0);
                return true;
            case ConsoleKey.LeftArrow:
                MoveCursor(0, -1);
                return true;
            case ConsoleKey.RightArrow:
                MoveCursor(0, 1);
                return true;
            case ConsoleKey.PageDown:
                PageDown(ViewportHeight);
                return true;
            case ConsoleKey.PageUp:
                PageUp(ViewportHeight);
                return true;
            case ConsoleKey.Home:
                JumpToStart();
                return true;
            case ConsoleKey.End:
                JumpToEnd();
                return true;
        }

        return true;
    }

    private bool HandleInsertKey(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            Mode = JournalMode.Normal;
            SaveIfDirty();
            return true;
        }

        if (key.Key == ConsoleKey.Enter)
        {
            SplitLine();
            return true;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            Backspace();
            return true;
        }

        if (key.Key == ConsoleKey.LeftArrow)
        {
            MoveCursor(0, -1);
            return true;
        }

        if (key.Key == ConsoleKey.RightArrow)
        {
            MoveCursor(0, 1);
            return true;
        }

        if (key.Key == ConsoleKey.UpArrow)
        {
            MoveCursor(-1, 0);
            return true;
        }

        if (key.Key == ConsoleKey.DownArrow)
        {
            MoveCursor(1, 0);
            return true;
        }

        if (!char.IsControl(key.KeyChar))
        {
            InsertChar(key.KeyChar);
            return true;
        }

        return true;
    }

    private void MoveCursor(int lineDelta, int columnDelta)
    {
        EnsureLines();

        CursorLine = Math.Clamp(CursorLine + lineDelta, 0, _lines.Count - 1);
        var lineLength = _lines[CursorLine].Length;
        CursorColumn = Math.Clamp(CursorColumn + columnDelta, 0, lineLength);
        _autoScroll = false;
        EnsureCursorVisible();
    }

    private void PageDown(int amount)
    {
        EnsureLines();
        CursorLine = Math.Clamp(CursorLine + amount, 0, _lines.Count - 1);
        ScrollTop = Math.Clamp(ScrollTop + amount, 0, Math.Max(0, _lines.Count - ViewportHeight));
        CursorColumn = Math.Clamp(CursorColumn, 0, _lines[CursorLine].Length);
        _autoScroll = false;
    }

    private void PageUp(int amount)
    {
        EnsureLines();
        CursorLine = Math.Clamp(CursorLine - amount, 0, _lines.Count - 1);
        ScrollTop = Math.Clamp(ScrollTop - amount, 0, Math.Max(0, _lines.Count - ViewportHeight));
        CursorColumn = Math.Clamp(CursorColumn, 0, _lines[CursorLine].Length);
        _autoScroll = false;
    }

    private void JumpToStart()
    {
        CursorLine = 0;
        CursorColumn = 0;
        ScrollTop = 0;
        _autoScroll = false;
    }

    private void JumpToEnd()
    {
        EnsureLines();
        CursorLine = _lines.Count - 1;
        CursorColumn = _lines[CursorLine].Length;
        ScrollTop = Math.Max(0, _lines.Count - ViewportHeight);
        _autoScroll = false;
    }

    private void InsertChar(char value)
    {
        EnsureLines();
        var line = _lines[CursorLine];
        var column = Math.Clamp(CursorColumn, 0, line.Length);
        _lines[CursorLine] = line.Insert(column, value.ToString());
        CursorColumn = column + 1;
        _dirty = true;
        EnsureCursorVisible();
    }

    private void SplitLine()
    {
        EnsureLines();
        var line = _lines[CursorLine];
        var column = Math.Clamp(CursorColumn, 0, line.Length);
        var before = line[..column];
        var after = line[column..];
        _lines[CursorLine] = before;
        _lines.Insert(CursorLine + 1, after);
        CursorLine++;
        CursorColumn = 0;
        _dirty = true;
        EnsureCursorVisible();
    }

    private void Backspace()
    {
        EnsureLines();
        if (CursorColumn > 0)
        {
            var line = _lines[CursorLine];
            _lines[CursorLine] = line.Remove(CursorColumn - 1, 1);
            CursorColumn--;
        }
        else if (CursorLine > 0)
        {
            var previous = _lines[CursorLine - 1];
            var current = _lines[CursorLine];
            var newColumn = previous.Length;
            _lines[CursorLine - 1] = previous + current;
            _lines.RemoveAt(CursorLine);
            CursorLine--;
            CursorColumn = newColumn;
        }
        _dirty = true;
        EnsureCursorVisible();
    }

    private void EnsureCursorVisible()
    {
        var maxScroll = Math.Max(0, _lines.Count - ViewportHeight);
        if (CursorLine < ScrollTop)
            ScrollTop = CursorLine;
        if (CursorLine >= ScrollTop + ViewportHeight)
            ScrollTop = CursorLine - ViewportHeight + 1;
        ScrollTop = Math.Clamp(ScrollTop, 0, maxScroll);
    }

    private void EnsureLines()
    {
        if (_lines.Count == 0)
            _lines.Add(string.Empty);
    }

    private static IEnumerable<string> WrapLine(string line, int width)
    {
        if (width <= 0)
            return [string.Empty];

        var wrapped = new List<string>();
        var index = 0;

        while (index < line.Length)
        {
            var length = Math.Min(width, line.Length - index);
            wrapped.Add(line.Substring(index, length));
            index += length;
        }

        if (wrapped.Count == 0)
            wrapped.Add(string.Empty);

        return wrapped;
    }

    private static string NormalizeLineEndings(string input)
        => input.Replace("\r\n", "\n").Replace("\r", "\n");
}
