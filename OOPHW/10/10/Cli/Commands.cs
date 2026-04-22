using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Sched.App;
using Sched.Domain;
using Sched.Reports;
using Sched.Storage;

namespace Sched.Cli;

public static class Commands
{
    public static int Run(string[] args, SchedApp app)
    {
        var p = new OptionParser(args);
        if (args.Length == 0) return ExitCodes.Ok;

        return p.Cmd(0) switch
        {
            "init" => InitCmd(p, app),
            "config" => ConfigCmd(p, app),
            "backup" => BackupCmd(p, app),
            "restore" => RestoreCmd(p, app),
            "room" => RoomCmd(p, app),
            "teacher" => TeacherCmd(p, app),
            "group" => GroupCmd(p, app),
            "course" => CourseCmd(p, app),
            "session" => SessionCmd(p, app),
            "import" => ImportCmd(p, app),
            "export" => ExportCmd(p, app),
            "report" => ReportCmd(p, app),
            _ => Fail(ExitCodes.ValidationError, "Unknown command")
        };
    }

    static int InitCmd(OptionParser p, SchedApp app)
    {
        var db = p.Get("--db") ?? app.Config.DbPath;
        var configPath = Path.Combine(Environment.CurrentDirectory, "sched.config.json");
        var cfg = new Config { DbPath = db };
        cfg.Save(configPath);

        var store = new DatabaseStore(db);
        store.Save(new Database());

        Console.WriteLine($"Initialized. db={db}");
        return ExitCodes.Ok;
    }

    static int ConfigCmd(OptionParser p, SchedApp app)
    {
        if (p.Cmd(1) != "show") return Fail(ExitCodes.ValidationError, "Usage: sched config show");
        Console.WriteLine($"db={app.Config.DbPath}");
        return ExitCodes.Ok;
    }

    static int BackupCmd(OptionParser p, SchedApp app)
    {
        p.Require("--out");
        var outPath = p.Get("--out")!;
        var db = app.Store.Load();
        File.WriteAllText(outPath, db.ToJson());
        Console.WriteLine($"Backup written: {outPath}");
        return ExitCodes.Ok;
    }

    static int RestoreCmd(OptionParser p, SchedApp app)
    {
        p.Require("--from");
        var from = p.Get("--from")!;
        if (!File.Exists(from)) return Fail(ExitCodes.NotFound, "Backup file not found");
        var json = File.ReadAllText(from);
        var db = Database.FromJson(json);
        app.Store.Save(db);
        Console.WriteLine("Restored.");
        return ExitCodes.Ok;
    }

    static int RoomCmd(OptionParser p, SchedApp app)
    {
        return p.Cmd(1) switch
        {
            "add" => RoomAdd(p, app),
            "list" => RoomList(p, app),
            "show" => RoomShow(p, app),
            "update" => RoomUpdate(p, app),
            "delete" => RoomDelete(p, app),
            _ => Fail(ExitCodes.ValidationError, "Bad room command")
        };
    }

    static int RoomAdd(OptionParser p, SchedApp app)
    {
        p.Require("--code", "--capacity");
        var code = p.Get("--code")!;
        var cap = p.GetInt("--capacity");
        if (cap is null or <= 0) return Fail(ExitCodes.ValidationError, "capacity must be > 0");
        var building = p.Get("--building") ?? "";
        var attr = p.Get("--attr") ?? "{}";

        var room = app.Rooms.Add(new RoomCreate(code, cap.Value, building, attr));
        Console.WriteLine($"Room {room.Code} (id={room.Id}) created.");
        return ExitCodes.Ok;
    }

    static int RoomList(OptionParser p, SchedApp app)
    {
        var items = app.Rooms.List();
        foreach (var r in items.OrderBy(x => x.Id))
            Console.WriteLine($"{r.Id}\t{r.Code}\t{r.Capacity}\t{r.Building}");
        return ExitCodes.Ok;
    }

    static int RoomShow(OptionParser p, SchedApp app)
    {
        var key = p.RequireOneOfPositional(2, "id|code");
        var room = app.Rooms.FindByIdOrCode(key);
        if (room == null) return Fail(ExitCodes.NotFound, "Room not found");
        Console.WriteLine(room.ToJson());
        return ExitCodes.Ok;
    }

    static int RoomUpdate(OptionParser p, SchedApp app)
    {
        var key = p.RequireOneOfPositional(2, "id|code");
        var patch = new RoomPatch(
            p.Get("--code"),
            p.GetInt("--capacity"),
            p.Get("--building"),
            p.Get("--attr")
        );
        var ok = app.Rooms.Update(key, patch, out var updated);
        if (!ok) return Fail(ExitCodes.NotFound, "Room not found");
        Console.WriteLine($"Room updated: id={updated!.Id}");
        return ExitCodes.Ok;
    }

    static int RoomDelete(OptionParser p, SchedApp app)
    {
        var key = p.RequireOneOfPositional(2, "id|code");
        var ok = app.Rooms.Delete(key);
        if (!ok) return Fail(ExitCodes.NotFound, "Room not found");
        Console.WriteLine("Room deleted.");
        return ExitCodes.Ok;
    }

    static int TeacherCmd(OptionParser p, SchedApp app)
    {
        return p.Cmd(1) switch
        {
            "add" => TeacherAdd(p, app),
            "list" => TeacherList(p, app),
            "show" => TeacherShow(p, app),
            "update" => TeacherUpdate(p, app),
            "delete" => TeacherDelete(p, app),
            _ => Fail(ExitCodes.ValidationError, "Bad teacher command")
        };
    }

    static int TeacherAdd(OptionParser p, SchedApp app)
    {
        p.Require("--name");
        var t = app.Teachers.Add(new TeacherCreate(p.Get("--name")!, p.Get("--email") ?? ""));
        Console.WriteLine($"Teacher {t.Name} (id={t.Id}) created.");
        return ExitCodes.Ok;
    }

    static int TeacherList(OptionParser p, SchedApp app)
    {
        foreach (var t in app.Teachers.List().OrderBy(x => x.Id))
            Console.WriteLine($"{t.Id}\t{t.Name}\t{t.Email}");
        return ExitCodes.Ok;
    }

    static int TeacherShow(OptionParser p, SchedApp app)
    {
        var id = p.RequireOneOfPositional(2, "id");
        if (!int.TryParse(id, out var n)) return Fail(ExitCodes.ValidationError, "id must be int");
        var t = app.Teachers.Get(n);
        if (t == null) return Fail(ExitCodes.NotFound, "Teacher not found");
        Console.WriteLine(t.ToJson());
        return ExitCodes.Ok;
    }

    static int TeacherUpdate(OptionParser p, SchedApp app)
    {
        var id = p.RequireOneOfPositional(2, "id");
        if (!int.TryParse(id, out var n)) return Fail(ExitCodes.ValidationError, "id must be int");
        var patch = new TeacherPatch(p.Get("--name"), p.Get("--email"));
        var ok = app.Teachers.Update(n, patch, out var updated);
        if (!ok) return Fail(ExitCodes.NotFound, "Teacher not found");
        Console.WriteLine($"Teacher updated: id={updated!.Id}");
        return ExitCodes.Ok;
    }

    static int TeacherDelete(OptionParser p, SchedApp app)
    {
        var id = p.RequireOneOfPositional(2, "id");
        if (!int.TryParse(id, out var n)) return Fail(ExitCodes.ValidationError, "id must be int");
        var ok = app.Teachers.Delete(n);
        if (!ok) return Fail(ExitCodes.NotFound, "Teacher not found");
        Console.WriteLine("Teacher deleted.");
        return ExitCodes.Ok;
    }

    static int GroupCmd(OptionParser p, SchedApp app)
    {
        return p.Cmd(1) switch
        {
            "add" => GroupAdd(p, app),
            "list" => GroupList(p, app),
            "show" => GroupShow(p, app),
            "update" => GroupUpdate(p, app),
            "delete" => GroupDelete(p, app),
            _ => Fail(ExitCodes.ValidationError, "Bad group command")
        };
    }

    static int GroupAdd(OptionParser p, SchedApp app)
    {
        p.Require("--code", "--size");
        var code = p.Get("--code")!;
        var size = p.GetInt("--size");
        if (size is null or <= 0) return Fail(ExitCodes.ValidationError, "size must be > 0");
        var year = p.GetInt("--year");
        var g = app.Groups.Add(new GroupCreate(code, size.Value, year));
        Console.WriteLine($"Group {g.Code} (id={g.Id}) created.");
        return ExitCodes.Ok;
    }

    static int GroupList(OptionParser p, SchedApp app)
    {
        foreach (var g in app.Groups.List().OrderBy(x => x.Id))
            Console.WriteLine($"{g.Id}\t{g.Code}\t{g.Size}\t{g.Year}");
        return ExitCodes.Ok;
    }

    static int GroupShow(OptionParser p, SchedApp app)
    {
        var id = p.RequireOneOfPositional(2, "id");
        if (!int.TryParse(id, out var n)) return Fail(ExitCodes.ValidationError, "id must be int");
        var g = app.Groups.Get(n);
        if (g == null) return Fail(ExitCodes.NotFound, "Group not found");
        Console.WriteLine(g.ToJson());
        return ExitCodes.Ok;
    }

    static int GroupUpdate(OptionParser p, SchedApp app)
    {
        var id = p.RequireOneOfPositional(2, "id");
        if (!int.TryParse(id, out var n)) return Fail(ExitCodes.ValidationError, "id must be int");
        var patch = new GroupPatch(p.Get("--code"), p.GetInt("--size"), p.GetInt("--year"));
        var ok = app.Groups.Update(n, patch, out var updated);
        if (!ok) return Fail(ExitCodes.NotFound, "Group not found");
        Console.WriteLine($"Group updated: id={updated!.Id}");
        return ExitCodes.Ok;
    }

    static int GroupDelete(OptionParser p, SchedApp app)
    {
        var id = p.RequireOneOfPositional(2, "id");
        if (!int.TryParse(id, out var n)) return Fail(ExitCodes.ValidationError, "id must be int");
        var ok = app.Groups.Delete(n);
        if (!ok) return Fail(ExitCodes.NotFound, "Group not found");
        Console.WriteLine("Group deleted.");
        return ExitCodes.Ok;
    }

    static int CourseCmd(OptionParser p, SchedApp app)
    {
        return p.Cmd(1) switch
        {
            "add" => CourseAdd(p, app),
            "list" => CourseList(p, app),
            "show" => CourseShow(p, app),
            "update" => CourseUpdate(p, app),
            "delete" => CourseDelete(p, app),
            _ => Fail(ExitCodes.ValidationError, "Bad course command")
        };
    }

    static int CourseAdd(OptionParser p, SchedApp app)
    {
        p.Require("--title");
        var title = p.Get("--title")!;
        var code = p.Get("--code") ?? "";
        var dur = p.GetInt("--duration") ?? 90;
        if (dur <= 0) return Fail(ExitCodes.ValidationError, "duration must be > 0");
        var c = app.Courses.Add(new CourseCreate(title, code, dur));
        Console.WriteLine($"Course {c.Title} (id={c.Id}) created.");
        return ExitCodes.Ok;
    }

    static int CourseList(OptionParser p, SchedApp app)
    {
        foreach (var c in app.Courses.List().OrderBy(x => x.Id))
            Console.WriteLine($"{c.Id}\t{c.Code}\t{c.Title}\t{c.DurationMinutes}");
        return ExitCodes.Ok;
    }

    static int CourseShow(OptionParser p, SchedApp app)
    {
        var id = p.RequireOneOfPositional(2, "id");
        if (!int.TryParse(id, out var n)) return Fail(ExitCodes.ValidationError, "id must be int");
        var c = app.Courses.Get(n);
        if (c == null) return Fail(ExitCodes.NotFound, "Course not found");
        Console.WriteLine(c.ToJson());
        return ExitCodes.Ok;
    }

    static int CourseUpdate(OptionParser p, SchedApp app)
    {
        var id = p.RequireOneOfPositional(2, "id");
        if (!int.TryParse(id, out var n)) return Fail(ExitCodes.ValidationError, "id must be int");
        var patch = new CoursePatch(p.Get("--title"), p.Get("--code"), p.GetInt("--duration"));
        var ok = app.Courses.Update(n, patch, out var updated);
        if (!ok) return Fail(ExitCodes.NotFound, "Course not found");
        Console.WriteLine($"Course updated: id={updated!.Id}");
        return ExitCodes.Ok;
    }

    static int CourseDelete(OptionParser p, SchedApp app)
    {
        var id = p.RequireOneOfPositional(2, "id");
        if (!int.TryParse(id, out var n)) return Fail(ExitCodes.ValidationError, "id must be int");
        var ok = app.Courses.Delete(n);
        if (!ok) return Fail(ExitCodes.NotFound, "Course not found");
        Console.WriteLine("Course deleted.");
        return ExitCodes.Ok;
    }

    static int SessionCmd(OptionParser p, SchedApp app)
    {
        return p.Cmd(1) switch
        {
            "add" => SessionAdd(p, app),
            "list" => SessionList(p, app),
            "show" => SessionShow(p, app),
            "update" => SessionUpdate(p, app),
            "delete" => SessionDelete(p, app),
            "find-conflicts" => SessionFindConflicts(p, app),
            _ => Fail(ExitCodes.ValidationError, "Bad session command")
        };
    }

    static int SessionAdd(OptionParser p, SchedApp app)
    {
        p.Require("--course", "--teacher", "--group", "--room", "--date", "--start", "--end");
        var courseId = ParseId(p.Get("--course")!, "--course");
        var teacherId = ParseId(p.Get("--teacher")!, "--teacher");
        var groupId = ParseId(p.Get("--group")!, "--group");
        var roomId = ParseIdOrRoomCode(p.Get("--room")!, app);
        var date = ParseDate(p.Get("--date")!);
        var start = ParseTime(p.Get("--start")!);
        var end = ParseTime(p.Get("--end")!);
        var notes = p.Get("--notes") ?? "";
        var force = p.Has("--force");

        var dow = p.Get("--dow");
        var rec = p.Get("--recurrence");
        var from = p.Get("--from");
        var to = p.Get("--to");

        if (!string.IsNullOrWhiteSpace(rec))
        {
            if (rec != "weekly") return Fail(ExitCodes.ValidationError, "Only weekly recurrence is supported");
            if (string.IsNullOrWhiteSpace(dow) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                return Fail(ExitCodes.ValidationError, "Recurrence requires --dow, --from, --to");

            var dayOfWeek = ParseDow(dow);
            var startDate = ParseDate(from);
            var endDate = ParseDate(to);

            var created = app.Sessions.AddWeeklySeries(
                new SessionCreate(courseId, teacherId, groupId, roomId, date, start, end, notes),
                dayOfWeek,
                startDate,
                endDate,
                force);

            Console.WriteLine($"Sessions created: {created.Count}");
            return ExitCodes.Ok;
        }

        try
        {
            var s = app.Sessions.Add(new SessionCreate(courseId, teacherId, groupId, roomId, date, start, end, notes), force);
            Console.WriteLine($"Session created: id={s.Id}, {s.Date:yyyy-MM-dd} {s.Start:hh\\:mm}-{s.End:hh\\:mm}");
            return ExitCodes.Ok;
        }
        catch (Services.ScheduleConflictException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ExitCodes.Conflict;
        }
    }

    static int SessionList(OptionParser p, SchedApp app)
    {
        var filter = new Services.SessionQuery
        {
            GroupId = TryParseId(p.Get("--group")),
            TeacherId = TryParseId(p.Get("--teacher")),
            RoomId = TryParseId(p.Get("--room")),
            Date = TryParseDate(p.Get("--date")),
            From = TryParseDate(p.Get("--from")),
            To = TryParseDate(p.Get("--to")),
            ConflictsOnly = p.Has("--conflicts-only")
        };

        var rows = app.Sessions.List(filter);
        var view = ScheduleView.Build(rows, app.Store.Load());
        Console.WriteLine(view);
        return ExitCodes.Ok;
    }

    static int SessionShow(OptionParser p, SchedApp app)
    {
        var id = ParseId(p.RequireOneOfPositional(2, "session_id"), "session_id");
        var s = app.Sessions.Get(id);
        if (s == null) return Fail(ExitCodes.NotFound, "Session not found");
        Console.WriteLine(s.ToJson());
        return ExitCodes.Ok;
    }

    static int SessionUpdate(OptionParser p, SchedApp app)
    {
        var id = ParseId(p.RequireOneOfPositional(2, "session_id"), "session_id");
        var patch = new SessionPatch(
            TryParseId(p.Get("--course")),
            TryParseId(p.Get("--teacher")),
            TryParseId(p.Get("--group")),
            TryParseId(p.Get("--room")),
            TryParseDate(p.Get("--date")),
            TryParseTime(p.Get("--start")),
            TryParseTime(p.Get("--end")),
            p.Get("--notes")
        );

        try
        {
            var ok = app.Sessions.Update(id, patch, out var updated);
            if (!ok) return Fail(ExitCodes.NotFound, "Session not found");
            Console.WriteLine($"Session updated: id={updated!.Id}");
            return ExitCodes.Ok;
        }
        catch (Services.ScheduleConflictException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ExitCodes.Conflict;
        }
    }

    static int SessionDelete(OptionParser p, SchedApp app)
    {
        var id = ParseId(p.RequireOneOfPositional(2, "session_id"), "session_id");
        var ok = app.Sessions.Delete(id);
        if (!ok) return Fail(ExitCodes.NotFound, "Session not found");
        Console.WriteLine("Session deleted.");
        return ExitCodes.Ok;
    }

    static int SessionFindConflicts(OptionParser p, SchedApp app)
    {
        var from = TryParseDate(p.Get("--from"));
        var to = TryParseDate(p.Get("--to"));
        var conflicts = app.Sessions.FindConflicts(from, to);

        if (conflicts.Count == 0)
        {
            Console.WriteLine("No conflicts.");
            return ExitCodes.Ok;
        }

        foreach (var c in conflicts)
            Console.WriteLine(c);

        return ExitCodes.Conflict;
    }

    static int ImportCmd(OptionParser p, SchedApp app)
    {
        var kind = p.Cmd(1);
        if (kind is not ("csv" or "json")) return Fail(ExitCodes.ValidationError, "Usage: sched import csv|json ...");

        if (kind == "json")
        {
            p.Require("--file");
            var path = p.Get("--file")!;
            if (!File.Exists(path)) return Fail(ExitCodes.NotFound, "File not found");
            var db = Database.FromJson(File.ReadAllText(path));
            app.Store.Save(db);
            Console.WriteLine("Imported JSON.");
            return ExitCodes.Ok;
        }

        p.Require("--entity", "--file");
        var entity = p.Get("--entity")!;
        var file = p.Get("--file")!;
        var mode = p.Get("--mode") ?? "append";
        if (!File.Exists(file)) return Fail(ExitCodes.NotFound, "CSV file not found");

        if (entity != "sessions") return Fail(ExitCodes.ValidationError, "Only sessions CSV import supported");

        var report = CsvSessions.Import(file, app, mode == "replace");
        Console.WriteLine($"Imported: added={report.Added}, failed={report.Failed}");
        foreach (var e in report.Errors) Console.WriteLine(e);
        return report.Failed > 0 ? ExitCodes.ValidationError : ExitCodes.Ok;
    }

    static int ExportCmd(OptionParser p, SchedApp app)
    {
        var kind = p.Cmd(1);
        if (kind is not ("csv" or "json")) return Fail(ExitCodes.ValidationError, "Usage: sched export csv|json ...");

        if (kind == "json")
        {
            p.Require("--out");
            var outPath = p.Get("--out")!;
            File.WriteAllText(outPath, app.Store.Load().ToJson());
            Console.WriteLine($"Exported JSON: {outPath}");
            return ExitCodes.Ok;
        }

        p.Require("--entity", "--out");
        var entity = p.Get("--entity")!;
        var outFile = p.Get("--out")!;
        if (entity != "sessions") return Fail(ExitCodes.ValidationError, "Only sessions CSV export supported");

        var from = TryParseDate(p.Get("--from"));
        var to = TryParseDate(p.Get("--to"));
        CsvSessions.Export(outFile, app, from, to);
        Console.WriteLine($"Exported CSV: {outFile}");
        return ExitCodes.Ok;
    }

    static int ReportCmd(OptionParser p, SchedApp app)
    {
        var t = p.Cmd(1);
        return t switch
        {
            "group" => ReportGroup(p, app),
            "teacher" => ReportTeacher(p, app),
            "room" => ReportRoom(p, app),
            "day" => ReportDay(p, app),
            _ => Fail(ExitCodes.ValidationError, "Bad report command")
        };
    }

    static int ReportGroup(OptionParser p, SchedApp app)
    {
        p.Require("--group", "--from", "--to");
        var id = ParseId(p.Get("--group")!, "--group");
        var from = ParseDate(p.Get("--from")!);
        var to = ParseDate(p.Get("--to")!);
        var format = p.Get("--format") ?? "text";

        var sessions = app.Sessions.List(new Services.SessionQuery { GroupId = id, From = from, To = to });
        return PrintReport(sessions, app, format);
    }

    static int ReportTeacher(OptionParser p, SchedApp app)
    {
        p.Require("--teacher", "--from", "--to");
        var id = ParseId(p.Get("--teacher")!, "--teacher");
        var from = ParseDate(p.Get("--from")!);
        var to = ParseDate(p.Get("--to")!);
        var format = p.Get("--format") ?? "text";

        var sessions = app.Sessions.List(new Services.SessionQuery { TeacherId = id, From = from, To = to });
        return PrintReport(sessions, app, format);
    }

    static int ReportRoom(OptionParser p, SchedApp app)
    {
        p.Require("--room", "--from", "--to");
        var id = ParseIdOrRoomCode(p.Get("--room")!, app);
        var from = ParseDate(p.Get("--from")!);
        var to = ParseDate(p.Get("--to")!);
        var format = p.Get("--format") ?? "text";

        var sessions = app.Sessions.List(new Services.SessionQuery { RoomId = id, From = from, To = to });
        return PrintReport(sessions, app, format);
    }

    static int ReportDay(OptionParser p, SchedApp app)
    {
        p.Require("--date");
        var date = ParseDate(p.Get("--date")!);
        var format = p.Get("--format") ?? "text";

        var sessions = app.Sessions.List(new Services.SessionQuery { Date = date });
        return PrintReport(sessions, app, format);
    }

    static int PrintReport(System.Collections.Generic.IReadOnlyList<Session> sessions, SchedApp app, string format)
    {
        var db = app.Store.Load();

        if (format == "text")
        {
            Console.WriteLine(ScheduleView.Build(sessions, db));
            return ExitCodes.Ok;
        }

        if (format == "json")
        {
            Console.WriteLine(ReportFormats.ToJson(sessions, db));
            return ExitCodes.Ok;
        }

        if (format == "csv")
        {
            Console.WriteLine(ReportFormats.ToCsv(sessions, db));
            return ExitCodes.Ok;
        }

        return Fail(ExitCodes.ValidationError, "format must be text|csv|json");
    }

    static int Fail(int code, string message)
    {
        Console.Error.WriteLine(message);
        return code;
    }

    static int ParseId(string s, string label)
    {
        if (!int.TryParse(s, out var n)) throw new CliExitException(ExitCodes.ValidationError, $"{label} must be int");
        return n;
    }

    static int ParseIdOrRoomCode(string s, SchedApp app)
    {
        if (int.TryParse(s, out var n)) return n;
        var room = app.Rooms.FindByIdOrCode(s);
        if (room == null) throw new CliExitException(ExitCodes.NotFound, "Room not found");
        return room.Id;
    }

    static int? TryParseId(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (int.TryParse(s, out var n)) return n;
        return null;
    }

    static DateOnly ParseDate(string s)
    {
        if (!DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            throw new CliExitException(ExitCodes.ValidationError, "date must be YYYY-MM-DD");
        return d;
    }

    static DateOnly? TryParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        return null;
    }

    static TimeOnly ParseTime(string s)
    {
        if (!TimeOnly.TryParseExact(s, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            throw new CliExitException(ExitCodes.ValidationError, "time must be HH:MM");
        return t;
    }

    static TimeOnly? TryParseTime(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (TimeOnly.TryParseExact(s, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            return t;
        return null;
    }

    static DayOfWeek ParseDow(string s)
    {
        return s.ToUpperInvariant() switch
        {
            "MON" => DayOfWeek.Monday,
            "TUE" => DayOfWeek.Tuesday,
            "WED" => DayOfWeek.Wednesday,
            "THU" => DayOfWeek.Thursday,
            "FRI" => DayOfWeek.Friday,
            "SAT" => DayOfWeek.Saturday,
            "SUN" => DayOfWeek.Sunday,
            _ => throw new CliExitException(ExitCodes.ValidationError, "dow must be MON..SUN")
        };
    }
}
