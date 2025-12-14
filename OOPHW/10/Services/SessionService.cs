using System;
using System.Collections.Generic;
using System.Linq;
using Sched.Domain;
using Sched.Storage;

namespace Sched.Services;

public sealed class SessionService
{
    private readonly DatabaseStore store;

    public SessionService(DatabaseStore store)
    {
        this.store = store;
    }

    public Session? Get(int id)
    {
        return store.Load().Sessions.FirstOrDefault(x => x.Id == id);
    }

    public IReadOnlyList<Session> List(SessionQuery q)
    {
        var db = store.Load();
        IEnumerable<Session> s = db.Sessions;

        if (q.GroupId.HasValue) s = s.Where(x => x.GroupId == q.GroupId.Value);
        if (q.TeacherId.HasValue) s = s.Where(x => x.TeacherId == q.TeacherId.Value);
        if (q.RoomId.HasValue) s = s.Where(x => x.RoomId == q.RoomId.Value);

        if (q.Date.HasValue) s = s.Where(x => x.Date == q.Date.Value);
        if (q.From.HasValue) s = s.Where(x => x.Date >= q.From.Value);
        if (q.To.HasValue) s = s.Where(x => x.Date <= q.To.Value);

        var list = s.OrderBy(x => x.Date).ThenBy(x => x.Start).ToList();

        if (!q.ConflictsOnly) return list;

        var conflicts = FindConflicts(q.From, q.To);
        var ids = conflicts.SelectMany(c => c.SessionIds).ToHashSet();
        return list.Where(x => ids.Contains(x.Id)).ToList();
    }

    public Session Add(SessionCreate create, bool force)
    {
        ValidateRefs(create);

        if (create.End <= create.Start) throw new ArgumentException("Invalid time range");

        var db = store.Load();
        var session = new Session
        {
            Id = db.NextSessionId++,
            CourseId = create.CourseId,
            TeacherId = create.TeacherId,
            GroupId = create.GroupId,
            RoomId = create.RoomId,
            Date = create.Date,
            Start = create.Start,
            End = create.End,
            Notes = create.Notes ?? ""
        };

        if (!force)
        {
            var conflict = FindFirstConflict(db, session);
            if (conflict != null) throw conflict;
        }

        db.Sessions.Add(session);
        store.Save(db);
        return session;
    }

    public List<Session> AddWeeklySeries(SessionCreate seed, DayOfWeek dow, DateOnly from, DateOnly to, bool force)
    {
        if (to < from) throw new ArgumentException("to must be >= from");
        var created = new List<Session>();

        var cur = from;
        while (cur.DayOfWeek != dow) cur = cur.AddDays(1);

        for (var d = cur; d <= to; d = d.AddDays(7))
        {
            var item = new SessionCreate(seed.CourseId, seed.TeacherId, seed.GroupId, seed.RoomId, d, seed.Start, seed.End, seed.Notes);
            created.Add(Add(item, force));
        }

        return created;
    }

    public bool Update(int id, SessionPatch patch, out Session? updated)
    {
        var db = store.Load();
        var s = db.Sessions.FirstOrDefault(x => x.Id == id);
        if (s == null)
        {
            updated = null;
            return false;
        }

        if (patch.CourseId.HasValue) s.CourseId = patch.CourseId.Value;
        if (patch.TeacherId.HasValue) s.TeacherId = patch.TeacherId.Value;
        if (patch.GroupId.HasValue) s.GroupId = patch.GroupId.Value;
        if (patch.RoomId.HasValue) s.RoomId = patch.RoomId.Value;
        if (patch.Date.HasValue) s.Date = patch.Date.Value;
        if (patch.Start.HasValue) s.Start = patch.Start.Value;
        if (patch.End.HasValue) s.End = patch.End.Value;
        if (patch.Notes != null) s.Notes = patch.Notes;

        ValidateRefs(new SessionCreate(s.CourseId, s.TeacherId, s.GroupId, s.RoomId, s.Date, s.Start, s.End, s.Notes));
        if (s.End <= s.Start) throw new ArgumentException("Invalid time range");

        var conflict = FindFirstConflict(db, s, excludeId: s.Id);
        if (conflict != null) throw conflict;

        store.Save(db);
        updated = s;
        return true;
    }

    public bool Delete(int id)
    {
        var db = store.Load();
        var s = db.Sessions.FirstOrDefault(x => x.Id == id);
        if (s == null) return false;
        db.Sessions.Remove(s);
        store.Save(db);
        return true;
    }

    public List<ConflictInfo> FindConflicts(DateOnly? from, DateOnly? to)
    {
        var db = store.Load();
        IEnumerable<Session> sessions = db.Sessions;

        if (from.HasValue) sessions = sessions.Where(x => x.Date >= from.Value);
        if (to.HasValue) sessions = sessions.Where(x => x.Date <= to.Value);

        var list = sessions.OrderBy(x => x.Date).ThenBy(x => x.Start).ToList();
        var conflicts = new List<ConflictInfo>();

        foreach (var dayGroup in list.GroupBy(x => x.Date))
        {
            var daySessions = dayGroup.ToList();
            for (int i = 0; i < daySessions.Count; i++)
            {
                for (int j = i + 1; j < daySessions.Count; j++)
                {
                    var a = daySessions[i];
                    var b = daySessions[j];
                    if (!a.TimeRange().Intersects(b.TimeRange())) continue;

                    if (a.RoomId == b.RoomId || a.GroupId == b.GroupId || a.TeacherId == b.TeacherId)
                    {
                        conflicts.Add(ConflictInfo.From(a, b));
                    }
                }
            }
        }

        return conflicts;
    }

    private void ValidateRefs(SessionCreate c)
    {
        var db = store.Load();
        if (db.Courses.All(x => x.Id != c.CourseId)) throw new ArgumentException("course not found");
        if (db.Teachers.All(x => x.Id != c.TeacherId)) throw new ArgumentException("teacher not found");
        if (db.Groups.All(x => x.Id != c.GroupId)) throw new ArgumentException("group not found");
        if (db.Rooms.All(x => x.Id != c.RoomId)) throw new ArgumentException("room not found");
    }

    private ScheduleConflictException? FindFirstConflict(Database db, Session session, int? excludeId = null)
    {
        var range = session.TimeRange();

        foreach (var s in db.Sessions)
        {
            if (excludeId.HasValue && s.Id == excludeId.Value) continue;
            if (s.Date != session.Date) continue;
            if (!s.TimeRange().Intersects(range)) continue;

            if (s.RoomId == session.RoomId)
                return new ScheduleConflictException($"Conflict detected: Room {session.RoomId} is occupied {s.Start:hh\\:mm}-{s.End:hh\\:mm} (session id={s.Id}).");

            if (s.TeacherId == session.TeacherId)
                return new ScheduleConflictException($"Conflict detected: Teacher {session.TeacherId} is busy {s.Start:hh\\:mm}-{s.End:hh\\:mm} (session id={s.Id}).");

            if (s.GroupId == session.GroupId)
                return new ScheduleConflictException($"Conflict detected: Group {session.GroupId} is busy {s.Start:hh\\:mm}-{s.End:hh\\:mm} (session id={s.Id}).");
        }

        return null;
    }
}

public sealed class SessionQuery
{
    public int? GroupId { get; set; }
    public int? TeacherId { get; set; }
    public int? RoomId { get; set; }
    public DateOnly? Date { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public bool ConflictsOnly { get; set; }
}

public sealed class ConflictInfo
{
    public DateOnly Date { get; set; }
    public int[] SessionIds { get; set; } = Array.Empty<int>();
    public string Kind { get; set; } = "";
    public string Message { get; set; } = "";

    public override string ToString() => $"{Date:yyyy-MM-dd}\t{Kind}\t{Message}";

    public static ConflictInfo From(Session a, Session b)
    {
        var kind =
            a.RoomId == b.RoomId ? "room" :
            a.TeacherId == b.TeacherId ? "teacher" :
            "group";

        return new ConflictInfo
        {
            Date = a.Date,
            SessionIds = new[] { a.Id, b.Id },
            Kind = kind,
            Message = $"sessions {a.Id} and {b.Id} overlap"
        };
    }
}
