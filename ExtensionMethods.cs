using Microsoft.AspNetCore.Components;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Linq;
using System.Reflection;

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
}
