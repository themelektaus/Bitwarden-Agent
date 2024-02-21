using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BitwardenAgent.Models;

public class Bitwarden_Login
{
    public List<Bitwarden_Uri> uris;
    public string username;
    public string password;
    public object totp;
    public object passwordRevisionDate;

    [OnDeserialized]
    void OnDeserialize(StreamingContext _)
    {
        username ??= string.Empty;
        password ??= string.Empty;
    }
}
