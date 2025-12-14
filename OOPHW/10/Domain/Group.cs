using System.Text.Json;

namespace Sched.Domain;

public sealed class Group
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int Size { get; set; }
    public int? Year { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
}

public readonly record struct GroupCreate(string Code, int Size, int? Year);
public readonly record struct GroupPatch(string? Code, int? Size, int? Year);
