using System.Text.Json;

namespace Sched.Domain;

public sealed class Teacher
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
}

public readonly record struct TeacherCreate(string Name, string Email);
public readonly record struct TeacherPatch(string? Name, string? Email);
