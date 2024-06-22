using BitwardenAgent.Web.Components;

using System.Collections.Generic;

namespace BitwardenAgent;

public class Dialog : DialogWithUserData<object>
{
    public static readonly Stack<IDialog> all = new();
    public static IDialog active => all.TryPeek(out var dialog) ? dialog : null;
}
