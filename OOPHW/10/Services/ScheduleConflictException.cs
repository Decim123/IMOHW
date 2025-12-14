using System;

namespace Sched.Services;

public sealed class ScheduleConflictException : InvalidOperationException
{
    public ScheduleConflictException(string message) : base(message) { }
}
