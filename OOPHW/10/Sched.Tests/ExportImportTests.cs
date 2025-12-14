using System.IO;
using NUnit.Framework;
using Sched.App;
using Sched.Domain;
using Sched.Services;
using Sched.Storage;

namespace Sched.Tests;

public class ExportImportTests
{
    [Test]
    public void ExportCsv_HasHeaderAndRow()
    {
        var store = new InMemoryStore();
        Seed(store);

        var app = new SchedApp
        {
            Config = new Config { DbPath = "x" },
            Store = store,
            Rooms = new RoomService(store),
            Teachers = new TeacherService(store),
            Groups = new GroupService(store),
            Courses = new CourseService(store),
            Sessions = new SessionService(store)
        };

        var tmp = Path.GetTempFileName();
        try
        {
            CsvSessions.Export(tmp, app, null, null);
            var text = File.ReadAllText(tmp);
            Assert.That(text, Does.Contain("date,start,end,courseId,teacherId,groupId,roomId,notes"));
            Assert.That(text, Does.Contain("2025-11-26"));
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    [Test]
    public void ImportCsv_MixedRows_ShouldReportErrors()
    {
        var store = new InMemoryStore();
        Seed(store);

        var app = new SchedApp
        {
            Config = new Config { DbPath = "x" },
            Store = store,
            Rooms = new RoomService(store),
            Teachers = new TeacherService(store),
            Groups = new GroupService(store),
            Courses = new CourseService(store),
            Sessions = new SessionService(store)
        };

        var tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp,
                "date,start,end,courseId,teacherId,groupId,roomId,notes\n" +
                "2025-11-26,10:00,11:30,1,1,1,1,ok\n" +
                "bad,10:00,11:30,1,1,1,1,fail\n");

            var rep = CsvSessions.Import(tmp, app, replace: false);
            Assert.That(rep.Added, Is.EqualTo(1));
            Assert.That(rep.Failed, Is.EqualTo(1));
            Assert.That(rep.Errors.Count, Is.EqualTo(1));
        }
        finally
        {
            File.Delete(tmp);
        }
    }

    static void Seed(InMemoryStore store)
    {
        var db = store.Load();
        db.Rooms.Add(new Room { Id = 1, Code = "A-101", Capacity = 40, Building = "Main", AttrJson = "{}" });
        db.Teachers.Add(new Teacher { Id = 1, Name = "Ivanov", Email = "" });
        db.Groups.Add(new Group { Id = 1, Code = "CS-2025", Size = 30, Year = 2025 });
        db.Courses.Add(new Course { Id = 1, Title = "Algo", Code = "A", DurationMinutes = 90 });
        db.Sessions.Add(new Session
        {
            Id = 1,
            CourseId = 1,
            TeacherId = 1,
            GroupId = 1,
            RoomId = 1,
            Date = new DateOnly(2025, 11, 26),
            Start = new TimeOnly(10, 0),
            End = new TimeOnly(11, 30),
            Notes = ""
        });
        store.Save(db);
    }
}
