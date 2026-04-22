using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Sched.Domain;
using Sched.Storage;

namespace Sched.Reports;

public static class ReportFormats
{
    public static string ToJson(IReadOnlyList<Session> sessions, Database db)
    {
        var items = sessions.Select(s => new
        {
            s.Id,
            Date = s.Date.ToString("yyyy-MM-dd"),
            Start = s.Start.ToString("HH:mm"),
            End = s.End.ToString("HH:mm"),
            Course = db.Courses.FirstOrDefault(x => x.Id == s.CourseId)?.Title ?? s.CourseId.ToString(),
            Teacher = db.Teachers.FirstOrDefault(x => x.Id == s.TeacherId)?.Name ?? s.TeacherId.ToString(),
            Group = db.Groups.FirstOrDefault(x => x.Id == s.GroupId)?.Code ?? s.GroupId.ToString(),
            Room = db.Rooms.FirstOrDefault(x => x.Id == s.RoomId)?.Code ?? s.RoomId.ToString(),
            Notes = s.Notes ?? ""
        }).ToList();

        return JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
    }

    public static string ToCsv(IReadOnlyList<Session> sessions, Database db)
    {
        var sb = new StringBuilder();
        sb.AppendLine("id,date,start,end,course,teacher,group,room,notes");

        foreach (var s in sessions.OrderBy(x => x.Date).ThenBy(x => x.Start))
        {
            var course = db.Courses.FirstOrDefault(x => x.Id == s.CourseId)?.Title ?? s.CourseId.ToString();
            var teacher = db.Teachers.FirstOrDefault(x => x.Id == s.TeacherId)?.Name ?? s.TeacherId.ToString();
            var group = db.Groups.FirstOrDefault(x => x.Id == s.GroupId)?.Code ?? s.GroupId.ToString();
            var room = db.Rooms.FirstOrDefault(x => x.Id == s.RoomId)?.Code ?? s.RoomId.ToString();
            sb.AppendLine($"{s.Id},{s.Date:yyyy-MM-dd},{s.Start:HH\\:mm},{s.End:HH\\:mm},{Esc(course)},{Esc(teacher)},{Esc(group)},{Esc(room)},{Esc(s.Notes ?? "")}");
        }

        return sb.ToString();
    }

    static string Esc(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
