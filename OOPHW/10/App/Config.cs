using System.IO;
using System.Text.Json;

namespace Sched.App;

public sealed class Config
{
    public string DbPath { get; set; } = "sched.db.json";

    public static Config? TryLoad(string path)
    {
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Config>(json);
    }

    public void Save(string path)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
