using System;
using System.Collections.Generic;
using System.Linq;
using Sched.Domain;
using Sched.Storage;

namespace Sched.Services;

public sealed class TeacherService
{
    private readonly DatabaseStore store;

    public TeacherService(DatabaseStore store)
    {
        this.store = store;
    }

    public Teacher Add(TeacherCreate create)
    {
        if (string.IsNullOrWhiteSpace(create.Name)) throw new ArgumentException(nameof(create.Name));

        var db = store.Load();
        var t = new Teacher
        {
            Id = db.NextTeacherId++,
            Name = create.Name,
            Email = create.Email ?? ""
        };

        db.Teachers.Add(t);
        store.Save(db);
        return t;
    }

    public IReadOnlyList<Teacher> List() => store.Load().Teachers;

    public Teacher? Get(int id) => store.Load().Teachers.FirstOrDefault(x => x.Id == id);

    public bool Update(int id, TeacherPatch patch, out Teacher? updated)
    {
        var db = store.Load();
        var t = db.Teachers.FirstOrDefault(x => x.Id == id);
        if (t == null)
        {
            updated = null;
            return false;
        }

        if (patch.Name != null)
        {
            if (string.IsNullOrWhiteSpace(patch.Name)) throw new ArgumentException("name");
            t.Name = patch.Name;
        }

        if (patch.Email != null) t.Email = patch.Email;

        store.Save(db);
        updated = t;
        return true;
    }

    public bool Delete(int id)
    {
        var db = store.Load();
        var t = db.Teachers.FirstOrDefault(x => x.Id == id);
        if (t == null) return false;
        db.Teachers.Remove(t);
        store.Save(db);
        return true;
    }
}
