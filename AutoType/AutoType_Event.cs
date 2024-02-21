using System.Windows.Forms;

namespace BitwardenAgent;

public sealed class AutoType_Event
{
    public enum Type { None, Key, KeyModifier, Char }
    public Type type;
    public int vKey;
    public bool? isExtendedKey;
    public Keys keyModifier;
    public char @char = char.MinValue;
    public bool? down;
    public string text;
}
