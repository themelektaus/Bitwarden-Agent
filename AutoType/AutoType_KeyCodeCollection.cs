using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace BitwardenAgent;

public static class AutoType_KeyCodeCollection
{
    static List<AutoType_KeyCode> items = null;
    public static List<AutoType_KeyCode> Items
    {
        get
        {
            if (items is null)
            {
                List<AutoType_KeyCode> _items =
                [
                    new("BACKSPACE", Keys.Back),
                    new("BKSP", Keys.Back),
                    new("BS", Keys.Back),
                    new("BREAK", Keys.Cancel),
                    new("CAPSLOCK", Keys.CapsLock),
                    new("CLEAR", Keys.Clear),
                    new("DEL", Keys.Delete),
                    new("DELETE", Keys.Delete),
                    new("END", Keys.End),
                    new("ENTER", Keys.Enter),
                    new("ESC", Keys.Escape),
                    new("ESCAPE", Keys.Escape),
                    new("HELP", Keys.Help),
                    new("HOME", Keys.Home),
                    new("INS", Keys.Insert),
                    new("INSERT", Keys.Insert),
                    new("NUMLOCK", Keys.NumLock),
                    new("PGDN", Keys.PageDown),
                    new("PGUP", Keys.PageUp),
                    new("PRTSC", Keys.PrintScreen),
                    new("SCROLLLOCK", Keys.Scroll),
                    new("SPACE", Keys.Space),
                    new("TAB", Keys.Tab),
                    new("UP", Keys.Up),
                    new("DOWN", Keys.Down),
                    new("LEFT", Keys.Left),
                    new("RIGHT", Keys.Right),
                ];

                for (int i = 1; i <= 24; i++)
                    _items.Add(new AutoType_KeyCode($"F{i}", (int) Keys.F1 + i - 1));

                _items.Add(new("ADD", Keys.Add));
                _items.Add(new("SUBTRACT", Keys.Subtract));
                _items.Add(new("MULTIPLY", Keys.Multiply));
                _items.Add(new("DIVIDE", Keys.Divide));

                for (int i = 0; i < 10; i++)
                    _items.Add(new AutoType_KeyCode($"NUMPAD{i}", (int) Keys.NumPad0 + i));

                _items.Add(new("WIN", Keys.LWin));
                _items.Add(new("LWIN", Keys.LWin));
                _items.Add(new("RWIN", Keys.RWin));
                _items.Add(new("APPS", Keys.Apps));

                items = _items;
            }

            return items;
        }
    }

    static Dictionary<string, AutoType_KeyCode> codes = null;
    public static AutoType_KeyCode Get(string code)
    {
        codes ??= Items.ToDictionary(x => x.code, x => x);

        if (codes.TryGetValue(code.ToUpperInvariant(), out var si))
            return si;

        return null;
    }

    static Dictionary<char, int> charsToKeys;
    static Dictionary<char, int> charsToKeys_Always;
    static void EnsureCharsToKeys()
    {
        if (charsToKeys is not null)
            return;

        var d = new Dictionary<char, int>
        {
            ['\u0008'] = (int) Keys.Back,
            ['\t'] = (int) Keys.Tab,
            ['\n'] = (int) Keys.LineFeed,
            ['\r'] = (int) Keys.Return,
            ['\u001B'] = (int) Keys.Escape,
            [' '] = (int) Keys.Space,
            ['\u007F'] = (int) Keys.Delete
        };

        charsToKeys_Always = new Dictionary<char, int>(d);

        for (char c = '0'; c <= '9'; ++c)
            d[c] = c - '0' + (int) Keys.D0;

        for (char c = 'a'; c <= 'z'; ++c)
            d[c] = c - 'a' + (int) Keys.A;

        charsToKeys = d;
    }

    public static int CharToVKey(char c, bool force)
    {
        EnsureCharsToKeys();
        return (force ? charsToKeys_Always : charsToKeys)
            .TryGetValue(c, out var vKey) ? vKey : 0;
    }
}
