using System;
using System.Collections.Generic;
using System.Linq;
using Sched.Domain;
using Sched.Storage;

namespace Sched.Services;

public sealed class CourseService
{
    private readonly DatabaseStore store;

    public CourseService(DatabaseStore store)
    {
        this.store = store;
    }

    public Course Add(CourseCreate create)
    {
        if (string.IsNullOrWhiteSpace(create.Title)) throw new ArgumentException(nameof(create.Title));
        if (create.DurationMinutes <= 0) throw new ArgumentException(nameof(create.DurationMinutes));

        var db = store.Load();
        var c = new Course
        {
            Id = db.NextCourseId++,
            Title = create.Title,
            Code = create.Code ?? "",
            DurationMinutes = create.DurationMinutes
        };

        db.Courses.Add(c);
        store.Save(db);
        return c;
    }

    public IReadOnlyList<Course> List() => store.Load().Courses;

    public Course? Get(int id) => store.Load().Courses.FirstOrDefault(x => x.Id == id);

    public bool Update(int id, CoursePatch patch, out Course? updated)
    {
        var db = store.Load();
        var c = db.Courses.FirstOrDefault(x => x.Id == id);
        if (c == null)
        {
            updated = null;
            return false;
        }

        if (patch.Title != null)
        {
            if (string.IsNullOrWhiteSpace(patch.Title)) throw new ArgumentException("title");
            c.Title = patch.Title;
        }

        if (patch.Code != null) c.Code = patch.Code;

        if (patch.DurationMinutes.HasValue)
        {
            if (patch.DurationMinutes.Value <= 0) throw new ArgumentException("duration");
            c.DurationMinutes = patch.DurationMinutes.Value;
        }

        store.Save(db);
        updated = c;
        return true;
    }

    public bool Delete(int id)
    {
        var db = store.Load();
        var c = db.Courses.FirstOrDefault(x => x.Id == id);
        if (c == null) return false;
        db.Courses.Remove(c);
        store.Save(db);
        return true;
    }
}
