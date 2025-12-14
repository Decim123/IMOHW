using System;
using System.Collections.Generic;
using System.Linq;
using Sched.Domain;
using Sched.Storage;

namespace Sched.Reports;

public static class GroupReport
{
    public static IReadOnlyList<Session> ByGroup(Database db, int groupId, DateOnly? from = null, DateOnly? to = null)
    {
        IEnumerable<Session> q = db.Sessions;

        q = q.Where(s => s.GroupId == groupId);

        if (from.HasValue) q = q.Where(s => s.Date >= from.Value);
        if (to.HasValue) q = q.Where(s => s.Date <= to.Value);

        return q.OrderBy(s => s.Date).ThenBy(s => s.Start).ToList();
    }
}
