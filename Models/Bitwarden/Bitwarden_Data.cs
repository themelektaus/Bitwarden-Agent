using System.Collections.Generic;

using System.Runtime.Serialization;

namespace BitwardenAgent.Models;

public class Bitwarden_Data
{
    public List<Bitwarden_Collection> collections = new();
    public List<Bitwarden_Item> items = new();

    public bool IsEmpty()
        => collections.Count == 0
        && items.Count == 0;

    [OnDeserialized]
    void OnDeserialize(StreamingContext _)
    {
        collections ??= new();
        items ??= new();
    }
}
