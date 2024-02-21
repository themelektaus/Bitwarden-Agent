namespace BitwardenAgent.Models;

public class Bitwarden_Field
{
    public enum Type
    {
        Text = 0,
        Hidden = 1,
        Boolean = 2
    }

    public string name;
    public string value;
    public Type type;
}
