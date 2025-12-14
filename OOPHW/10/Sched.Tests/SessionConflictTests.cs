using NUnit.Framework;
using Sched.Domain;
using Sched.Services;

namespace Sched.Tests;

[TestFixture]
public class SessionConflictTests
{
    [Test]
    public void Conflict_SameRoom_SameDate_ShouldFail()
    {
        var store = new InMemoryStore();
        SeedRefs(store);
        var svc = new SessionService(store);

        svc.Add(new SessionCreate(1, 1, 1, 1, new DateOnly(2025, 11, 26), new TimeOnly(10, 0), new TimeOnly(11, 30), ""), false);

        Assert.Throws<ScheduleConflictException>(() =>
            svc.Add(new SessionCreate(1, 2, 2, 1, new DateOnly(2025, 11, 26), new TimeOnly(11, 0), new TimeOnly(12, 0), ""), false));
    }

    [Test]
    public void NoConflict_DifferentDate_ShouldPass()
    {
        var store = new InMemoryStore();
        SeedRefs(store);
        var svc = new SessionService(store);

        svc.Add(new SessionCreate(1, 1, 1, 1, new DateOnly(2025, 11, 26), new TimeOnly(10, 0), new TimeOnly(11, 30), ""), false);

        Assert.DoesNotThrow(() =>
            svc.Add(new SessionCreate(1, 1, 1, 1, new DateOnly(2025, 11, 27), new TimeOnly(10, 0), new TimeOnly(11, 30), ""), false));
    }

    [Test]
    public void Conflict_SameTeacher_ShouldFail()
    {
        var store = new InMemoryStore();
        SeedRefs(store);
        var svc = new SessionService(store);

        svc.Add(new SessionCreate(1, 1, 1, 1, new DateOnly(2025, 11, 26), new TimeOnly(10, 0), new TimeOnly(11, 0), ""), false);

        Assert.Throws<ScheduleConflictException>(() =>
            svc.Add(new SessionCreate(1, 1, 2, 2, new DateOnly(2025, 11, 26), new TimeOnly(10, 30), new TimeOnly(11, 30), ""), false));
    }

    [Test]
    public void Conflict_SameGroup_ShouldFail()
    {
        var store = new InMemoryStore();
        SeedRefs(store);
        var svc = new SessionService(store);

        svc.Add(new SessionCreate(1, 2, 1, 1, new DateOnly(2025, 11, 26), new TimeOnly(10, 0), new TimeOnly(11, 0), ""), false);

        Assert.Throws<ScheduleConflictException>(() =>
            svc.Add(new SessionCreate(1, 1, 1, 2, new DateOnly(2025, 11, 26), new TimeOnly(10, 30), new TimeOnly(11, 30), ""), false));
    }

    static void SeedRefs(InMemoryStore store)
    {
        var db = store.Load();
        db.Rooms.Add(new Room { Id = 1, Code = "A-101", Capacity = 40, Building = "Main", AttrJson = "{}" });
        db.Rooms.Add(new Room { Id = 2, Code = "B-201", Capacity = 30, Building = "Main", AttrJson = "{}" });
        db.Teachers.Add(new Teacher { Id = 1, Name = "Ivanov", Email = "" });
        db.Teachers.Add(new Teacher { Id = 2, Name = "Petrov", Email = "" });
        db.Groups.Add(new Group { Id = 1, Code = "CS-2025", Size = 30, Year = 2025 });
        db.Groups.Add(new Group { Id = 2, Code = "CS-2026", Size = 28, Year = 2026 });
        db.Courses.Add(new Course { Id = 1, Title = "Algo", Code = "A", DurationMinutes = 90 });
        db.NextRoomId = 3;
        db.NextTeacherId = 3;
        db.NextGroupId = 3;
        db.NextCourseId = 2;
        store.Save(db);
    }
}
