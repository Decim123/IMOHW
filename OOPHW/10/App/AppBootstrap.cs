using System;
using System.IO;
using Sched.Services;
using Sched.Storage;

namespace Sched.App;

public sealed class SchedApp
{
    public required Config Config { get; init; }
    public required DatabaseStore Store { get; init; }
    public required RoomService Rooms { get; init; }
    public required TeacherService Teachers { get; init; }
    public required GroupService Groups { get; init; }
    public required CourseService Courses { get; init; }
    public required SessionService Sessions { get; init; }
}

public static class AppBootstrap
{
    public static SchedApp Create()
    {
        var configPath = Path.Combine(Environment.CurrentDirectory, "sched.config.json");
        var config = Config.TryLoad(configPath) ?? new Config { DbPath = "sched.db.json" };

        var store = new DatabaseStore(config.DbPath);
        var rooms = new RoomService(store);
        var teachers = new TeacherService(store);
        var groups = new GroupService(store);
        var courses = new CourseService(store);
        var sessions = new SessionService(store);

        return new SchedApp
        {
            Config = config,
            Store = store,
            Rooms = rooms,
            Teachers = teachers,
            Groups = groups,
            Courses = courses,
            Sessions = sessions
        };
    }
}
