using System.Collections.Generic;

namespace BitwardenAgent.Models;

public class Bitwarden_SecureNote
{
    public class Type
    {
        public int type;
    }

    public List<string> collectionIds;
    public object organizationId;
    public object collectionId;
    public string folderId;
    public Bitwarden_Item.Type type;
    public string name;
    public string notes;
    public bool favorite;
    public List<object> fields;
    public Type secureNote = new();
    public int reprompt;
}
