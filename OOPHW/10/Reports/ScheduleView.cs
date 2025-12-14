using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sched.Domain;
using Sched.Storage;

namespace Sched.Reports;

public static class ScheduleView
{
    public static string Build(IReadOnlyList<Session> sessions, Database db)
    {
        var rows = sessions
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Start)
            .Select(s => new[]
            {
                s.Date.ToString("yyyy-MM-dd"),
                s.Start.ToString("HH:mm"),
                s.End.ToString("HH:mm"),
                ResolveCourse(db, s.CourseId),
                ResolveTeacher(db, s.TeacherId),
                ResolveGroup(db, s.GroupId),
                ResolveRoom(db, s.RoomId),
                s.Notes ?? ""
            })
            .ToList();

        var headers = new[] { "Date", "Start", "End", "Course", "Teacher", "Group", "Room", "Notes" };
        return AsciiTable.Render(headers, rows);
    }

    static string ResolveRoom(Database db, int id) => db.Rooms.FirstOrDefault(x => x.Id == id)?.Code ?? id.ToString();
    static string ResolveTeacher(Database db, int id) => db.Teachers.FirstOrDefault(x => x.Id == id)?.Name ?? id.ToString();
    static string ResolveGroup(Database db, int id) => db.Groups.FirstOrDefault(x => x.Id == id)?.Code ?? id.ToString();
    static string ResolveCourse(Database db, int id) => db.Courses.FirstOrDefault(x => x.Id == id)?.Title ?? id.ToString();
}

public static class AsciiTable
{
    public static string Render(string[] headers, List<string[]> rows)
    {
        var cols = headers.Length;
        var widths = new int[cols];

        for (int i = 0; i < cols; i++) widths[i] = headers[i].Length;
        foreach (var r in rows)
            for (int i = 0; i < cols; i++)
                widths[i] = Math.Max(widths[i], r[i].Length);

        var sb = new StringBuilder();
        sb.AppendLine(Line(widths));
        sb.AppendLine(Row(headers, widths));
        sb.AppendLine(Line(widths));
        foreach (var r in rows) sb.AppendLine(Row(r, widths));
        sb.AppendLine(Line(widths));
        return sb.ToString();
    }

    static string Line(int[] w)
    {
        var sb = new StringBuilder();
        sb.Append('+');
        foreach (var x in w)
        {
            sb.Append(new string('-', x + 2));
            sb.Append('+');
        }
        return sb.ToString();
    }

    static string Row(string[] v, int[] w)
    {
        var sb = new StringBuilder();
        sb.Append('|');
        for (int i = 0; i < w.Length; i++)
        {
            sb.Append(' ');
            sb.Append(v[i].PadRight(w[i]));
            sb.Append(' ');
            sb.Append('|');
        }
        return sb.ToString();
    }
}
