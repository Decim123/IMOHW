using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Sched.App;
using Sched.Domain;

namespace Sched.Storage;

public sealed class CsvImportReport
{
    public int Added { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}

public static class CsvSessions
{
    public static CsvImportReport Import(string path, SchedApp app, bool replace)
    {
        var report = new CsvImportReport();
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0) return report;

        if (replace)
        {
            var db0 = app.Store.Load();
            db0.Sessions.Clear();
            app.Store.Save(db0);
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var cols = SplitCsvLine(line);
                var date = ParseDate(cols[0]);
                var start = ParseTime(cols[1]);
                var end = ParseTime(cols[2]);
                var courseId = int.Parse(cols[3]);
                var teacherId = int.Parse(cols[4]);
                var groupId = int.Parse(cols[5]);
                var roomId = int.Parse(cols[6]);
                var notes = cols.Length > 7 ? cols[7] : "";

                app.Sessions.Add(new SessionCreate(courseId, teacherId, groupId, roomId, date, start, end, notes), force: false);
                report.Added++;
            }
            catch (Exception ex)
            {
                report.Failed++;
                report.Errors.Add($"line {i + 1}: {ex.Message}");
            }
        }

        return report;
    }

    public static void Export(string outPath, SchedApp app, DateOnly? from, DateOnly? to)
    {
        var q = new Services.SessionQuery { From = from, To = to };
        var sessions = app.Sessions.List(q);
        using var sw = new StreamWriter(outPath);
        sw.WriteLine("date,start,end,courseId,teacherId,groupId,roomId,notes");
        foreach (var s in sessions.OrderBy(x => x.Date).ThenBy(x => x.Start))
        {
            sw.WriteLine($"{s.Date:yyyy-MM-dd},{s.Start:HH\\:mm},{s.End:HH\\:mm},{s.CourseId},{s.TeacherId},{s.GroupId},{s.RoomId},{Esc(s.Notes ?? "")}");
        }
    }

    static string Esc(string s)
    {
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    static DateOnly ParseDate(string s)
    {
        if (!DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            throw new ArgumentException("bad date");
        return d;
    }

    static TimeOnly ParseTime(string s)
    {
        if (!TimeOnly.TryParseExact(s, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            throw new ArgumentException("bad time");
        return t;
    }

    static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var cur = "";
        var inQ = false;

        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQ)
            {
                if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    cur += '"';
                    i++;
                    continue;
                }
                if (ch == '"')
                {
                    inQ = false;
                    continue;
                }
                cur += ch;
                continue;
            }

            if (ch == '"')
            {
                inQ = true;
                continue;
            }

            if (ch == ',')
            {
                result.Add(cur);
                cur = "";
                continue;
            }

            cur += ch;
        }

        result.Add(cur);
        return result.Select(x => x.Trim()).ToArray();
    }
}
