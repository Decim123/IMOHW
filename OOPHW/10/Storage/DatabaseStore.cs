using System;
using System.IO;
using System.Text.Json;

namespace Sched.Storage;

public sealed class DatabaseStore
{
    private readonly string path;

    public DatabaseStore(string path)
    {
        this.path = path;
    }

    public Database Load()
    {
        if (!File.Exists(path)) return new Database();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Database>(json) ?? new Database();
    }

    public void Save(Database db)
    {
        var json = JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
