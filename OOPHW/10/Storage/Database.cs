using System.Collections.Generic;
using System.Text.Json;
using Sched.Domain;

namespace Sched.Storage;

public sealed class Database
{
    public int NextRoomId { get; set; } = 1;
    public int NextTeacherId { get; set; } = 1;
    public int NextGroupId { get; set; } = 1;
    public int NextCourseId { get; set; } = 1;
    public int NextSessionId { get; set; } = 1;

    public List<Room> Rooms { get; set; } = new();
    public List<Teacher> Teachers { get; set; } = new();
    public List<Group> Groups { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public List<Session> Sessions { get; set; } = new();

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static Database FromJson(string json)
    {
        return JsonSerializer.Deserialize<Database>(json) ?? new Database();
    }
}
