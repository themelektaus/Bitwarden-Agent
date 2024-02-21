namespace BitwardenAgent;

public sealed class AutoType_CharStream(string s)
{
    readonly string s = s;

    int position = 0;

    public char Read()
    {
        if (position >= s.Length)
            return char.MinValue;

        return s[position++];
    }

    public char Peek()
    {
        if (position >= s.Length)
            return char.MinValue;

        return s[position];
    }
}
