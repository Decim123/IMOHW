using System.Text.Json;

namespace Sched.Domain;

public sealed class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Code { get; set; } = "";
    public int DurationMinutes { get; set; } = 90;

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
}

public readonly record struct CourseCreate(string Title, string Code, int DurationMinutes);
public readonly record struct CoursePatch(string? Title, string? Code, int? DurationMinutes);
