using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

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
    public List<Bitwarden_Collection> collections;
    public List<Bitwarden_Attachment> attachments;
    public DateTime revisionDate;
    public object deletedDate;

    [OnDeserialized]
    void OnDeserialize(StreamingContext _)
    {
        login ??= new();
    }

    public void LoadCollections(IList<Bitwarden_Collection> collections)
    {
        this.collections ??= [];
        this.collections.Clear();

        foreach (var collectionId in collectionIds)
        {
            var collection = collections.FirstOrDefault(x => x.id == collectionId);
            if (collection is not null)
                this.collections.Add(collection);
        }
    }

    public string GetSimplifiedName()
    {
        var name = this.name;

        var repeat = true;
        while (repeat)
        {
            repeat = false;
            foreach (var collectionName in GetCollectionNames())
            {
                if (!name.StartsWith(collectionName))
                    continue;

                name = name[collectionName.Length..].Trim().Trim('-').Trim();
                repeat = true;
            }
        }

        return name;
    }

    List<string> collectionNamesCache;

    public List<string> GetCollectionNames()
    {
        if (collections is null)
        {
            collectionNamesCache = null;
            return [];
        }

        return collectionNamesCache ??= collections
            .Where(x => x is not null && !string.IsNullOrEmpty(x.name))
            .OrderBy(x => x.name)
            .SelectMany(x => x.name.Split('/'))
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => Regex.Replace(x, "\\(.*\\)", ""))
            .Select(x => x.Split(" - ").LastOrDefault())
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();
    }

    public string GetUrl()
    {
        return login.uris.FirstOrDefault()?.uri ?? string.Empty;
    }

    public string GetNotes()
    {
        return notes ?? string.Empty;
    }
}
