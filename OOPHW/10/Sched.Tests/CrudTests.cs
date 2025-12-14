using NUnit.Framework;
using Sched.Services;
using Sched.Domain;

namespace Sched.Tests;

public class CrudTests
{
    [Test]
    public void Room_CRUD_Works()
    {
        var store = new InMemoryStore();
        var rooms = new RoomService(store);

        var r = rooms.Add(new RoomCreate("A-101", 40, "Main", "{}"));
        Assert.That(r.Id, Is.GreaterThan(0));

        var got = rooms.FindByIdOrCode("A-101");
        Assert.That(got, Is.Not.Null);

        var ok = rooms.Update("A-101", new RoomPatch("A-102", 50, null, null), out var upd);
        Assert.That(ok, Is.True);
        Assert.That(upd!.Code, Is.EqualTo("A-102"));

        var del = rooms.Delete("A-102");
        Assert.That(del, Is.True);
    }

    [Test]
    public void Teacher_CRUD_Works()
    {
        var store = new InMemoryStore();
        var svc = new TeacherService(store);

        var t = svc.Add(new TeacherCreate("Ivanov I.", "a@b.com"));
        Assert.That(t.Id, Is.GreaterThan(0));

        var ok = svc.Update(t.Id, new TeacherPatch("Ivanov I.I.", null), out var upd);
        Assert.That(ok, Is.True);
        Assert.That(upd!.Name, Is.EqualTo("Ivanov I.I."));

        Assert.That(svc.Delete(t.Id), Is.True);
    }

    [Test]
    public void Group_CRUD_Works()
    {
        var store = new InMemoryStore();
        var svc = new GroupService(store);

        var g = svc.Add(new GroupCreate("CS-2025", 30, 2025));
        Assert.That(g.Id, Is.GreaterThan(0));

        var ok = svc.Update(g.Id, new GroupPatch(null, 31, null), out var upd);
        Assert.That(ok, Is.True);
        Assert.That(upd!.Size, Is.EqualTo(31));

        Assert.That(svc.Delete(g.Id), Is.True);
    }

    [Test]
    public void Course_CRUD_Works()
    {
        var store = new InMemoryStore();
        var svc = new CourseService(store);

        var c = svc.Add(new CourseCreate("Algorithms", "ALGO101", 90));
        Assert.That(c.Id, Is.GreaterThan(0));

        var ok = svc.Update(c.Id, new CoursePatch(null, "ALGO102", 120), out var upd);
        Assert.That(ok, Is.True);
        Assert.That(upd!.DurationMinutes, Is.EqualTo(120));

        Assert.That(svc.Delete(c.Id), Is.True);
    }
}
