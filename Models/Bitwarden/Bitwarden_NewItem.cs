using System.Collections.Generic;

namespace BitwardenAgent.Models;

public class Bitwarden_NewItem
{
    public List<string> collectionIds;
    public string organizationId;
    public string collectionId;
    public object folderId;
    public Bitwarden_Item.Type type;
    public string name;
    public object notes;
    public bool favorite;
    public List<Bitwarden_Field> fields;
    public Bitwarden_Login login;
    public int reprompt;
}
