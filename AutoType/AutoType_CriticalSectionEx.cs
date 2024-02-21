using System.Threading;

namespace BitwardenAgent;

public sealed class AutoType_CriticalSectionEx
{
    int location;

    public bool Enter()
    {
        if (IsLocked())
            return false;

        Interlocked.Increment(ref location);
        return true;
    }

    public bool Exit()
    {
        if (!IsLocked())
            return false;

        Interlocked.Decrement(ref location);
        return true;
    }

    bool IsLocked()
    {
        return Interlocked.Exchange(ref location, 1) != 0;
    }
}
