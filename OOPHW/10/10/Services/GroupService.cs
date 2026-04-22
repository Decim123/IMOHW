using System;
using System.Collections.Generic;
using System.Linq;
using Sched.Domain;
using Sched.Storage;

namespace Sched.Services;

public sealed class GroupService
{
    private readonly DatabaseStore store;

    public GroupService(DatabaseStore store)
    {
        this.store = store;
    }

    public Group Add(GroupCreate create)
    {
        if (string.IsNullOrWhiteSpace(create.Code)) throw new ArgumentException(nameof(create.Code));
        if (create.Size <= 0) throw new ArgumentException(nameof(create.Size));

        var db = store.Load();
        if (db.Groups.Any(g => g.Code == create.Code)) throw new InvalidOperationException("Group exists");

        var g = new Group
        {
            Id = db.NextGroupId++,
            Code = create.Code,
            Size = create.Size,
            Year = create.Year
        };

        db.Groups.Add(g);
        store.Save(db);
        return g;
    }

    public IReadOnlyList<Group> List() => store.Load().Groups;

    public Group? Get(int id) => store.Load().Groups.FirstOrDefault(x => x.Id == id);

    public bool Update(int id, GroupPatch patch, out Group? updated)
    {
        var db = store.Load();
        var g = db.Groups.FirstOrDefault(x => x.Id == id);
        if (g == null)
        {
            updated = null;
            return false;
        }

        if (patch.Code != null)
        {
            if (string.IsNullOrWhiteSpace(patch.Code)) throw new ArgumentException("code");
            if (db.Groups.Any(x => x.Id != g.Id && x.Code == patch.Code)) throw new InvalidOperationException("Group exists");
            g.Code = patch.Code;
        }

        if (patch.Size.HasValue)
        {
            if (patch.Size.Value <= 0) throw new ArgumentException("size");
            g.Size = patch.Size.Value;
        }

        if (patch.Year.HasValue) g.Year = patch.Year.Value;

        store.Save(db);
        updated = g;
        return true;
    }

    public bool Delete(int id)
    {
        var db = store.Load();
        var g = db.Groups.FirstOrDefault(x => x.Id == id);
        if (g == null) return false;
        db.Groups.Remove(g);
        store.Save(db);
        return true;
    }
}
