using NUnit.Framework;
using Sched.Domain;

namespace Sched.Tests;

public class UnitTest1
{
    [Test]
    public void TimeRange_EndBeforeStart_ShouldFail()
    {
        Assert.Throws<System.ArgumentException>(() =>
            new TimeRange(new TimeOnly(10, 0), new TimeOnly(9, 0)));
    }
}
