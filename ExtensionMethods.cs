using Microsoft.AspNetCore.Components;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Flags = System.Reflection.BindingFlags;

namespace BitwardenAgent;

public static class ExtensionMethods
{
    const Flags PRIVATE_FLAGS = Flags.Instance | Flags.NonPublic;

    public static void RenderLater(this ComponentBase @this)
    {
        var method = @this.GetPrivateMethod("StateHasChanged");
        var action = new Action(() => method.Invoke(@this, null));
        var invokeMethod = @this.GetPrivateMethod("InvokeAsync", typeof(Action));
        invokeMethod.Invoke(@this, new object[] { action });
    }

    public static MethodInfo GetPrivateMethod(this object @this, string name, params Type[] argTypes)
    {
        return @this.AsType().GetMethod(name, PRIVATE_FLAGS, argTypes);
    }

    static Type AsType(this object @object)
    {
        return @object is Type type ? type : @object.GetType();
    }

    static readonly JsonSerializerSettings jsonSerializerSettings = new()
    {
        Formatting = Formatting.Indented
    };

    public static string ToJson<T>(this T @this)
    {
        return JsonConvert.SerializeObject(@this, jsonSerializerSettings);
    }

    public static T FromJson<T>(this string @this)
    {
        return JsonConvert.DeserializeObject<T>(@this, jsonSerializerSettings);
    }

    public static bool ValidateJson(this string @this)
    {
        try
        {
            JToken.Parse(@this);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string GetHostname(this string @this)
    {
        return @this
            .Split("://").LastOrDefault()
            .Split('/').FirstOrDefault()
        ?? string.Empty;
    }

    public static string Split(this string @this, int blockSize, string separator)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < @this.Length; i++)
        {
            if (i > 0 && i % blockSize == 0)
                builder.Append(separator);
            builder.Append(@this[i]);
        }
        return builder.ToString();
    }

    public static async Task<T> InvokeAsync<T>(this Control @this, Func<T> method)
    {
        var result = @this.BeginInvoke(method);
        await Task.Run(result.AsyncWaitHandle.WaitOne);
        return (T) @this.EndInvoke(result);
    }

    public static async Task<DialogResult> ShowDialogAsync(this CommonDialog @this)
    {
        var result = DialogResult.None;
        var thread = new Thread(() => result = @this.ShowDialog());
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await Task.Run(thread.Join);
        return result;
    }

    public static async Task<DialogResult> ShowDialogAsync(this CommonDialog @this, Control threadHandle)
    {
        return await threadHandle.InvokeAsync(@this.ShowDialog);
    }

    public static MarkupString ToHtmlString(this string @this)
    {
        return new(@this.ReplaceLineEndings("<br>"));
    }
}
