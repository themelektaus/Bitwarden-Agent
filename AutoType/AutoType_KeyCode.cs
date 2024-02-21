using System.Windows.Forms;

namespace BitwardenAgent;

public sealed class AutoType_KeyCode
{
    public readonly string code;
    public readonly int vKey;

    public AutoType_KeyCode(string code, Keys vKey) : this(code, (int) vKey)
    {

    }

    public AutoType_KeyCode(string code, int vKey)
    {
        this.code = string.IsNullOrEmpty(code) ? " " : code;
        this.vKey = vKey;
    }
}
