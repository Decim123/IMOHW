using Sched.Storage;

namespace Sched.Tests;

public sealed class InMemoryStore : DatabaseStore
{
    private Database db = new();

    public InMemoryStore() : base("inmem") { }

    public new Database Load() => db;

    public new void Save(Database newDb) => db = newDb;
}
