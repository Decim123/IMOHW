using System;

namespace Sched.Domain;

public sealed class TimeRange
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    public TimeRange(TimeOnly start, TimeOnly end)
    {
        if (end <= start) throw new ArgumentException("Invalid time range");
        Start = start;
        End = end;
    }

    public bool Intersects(TimeRange other)
    {
        return Start < other.End && End > other.Start;
    }
}
