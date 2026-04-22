using System;
using System.Text.Json;

namespace Sched.Domain;

public sealed class Room
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int Capacity { get; set; }
    public string Building { get; set; } = "";
    public string AttrJson { get; set; } = "{}";

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
}

public readonly record struct RoomCreate(string Code, int Capacity, string Building, string AttrJson);
public readonly record struct RoomPatch(string? Code, int? Capacity, string? Building, string? AttrJson);
