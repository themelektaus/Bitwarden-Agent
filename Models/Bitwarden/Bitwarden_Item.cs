using System;
using System.Collections.Generic;

using System.Runtime.Serialization;

namespace BitwardenAgent.Models;

public class Bitwarden_Item
{
    public enum Type
    {
        Unknown = 0,
        Login = 1,
        SecureNote = 2,
        Card = 3,
        Identity = 4
    }

    public string @object;
    public string id;
    public string organizationId;
    public object folderId;
    public Type type;
    public int reprompt;
    public string name;
    public string notes;
    public bool favorite;
    public Bitwarden_Login login;
    public List<string> collectionIds;
    public List<Bitwarden_Attachment> attachments;
    public DateTime revisionDate;
    public object deletedDate;

    [OnDeserialized]
    void OnDeserialize(StreamingContext _)
    {
        login ??= new();
    }
}
