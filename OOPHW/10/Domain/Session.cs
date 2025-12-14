using System;
using System.Text.Json;

namespace Sched.Domain;

public sealed class Session
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int TeacherId { get; set; }
    public int GroupId { get; set; }
    public int RoomId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public string Notes { get; set; } = "";

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public TimeRange TimeRange() => new TimeRange(Start, End);
}

public readonly record struct SessionCreate(
    int CourseId,
    int TeacherId,
    int GroupId,
    int RoomId,
    DateOnly Date,
    TimeOnly Start,
    TimeOnly End,
    string Notes);

public readonly record struct SessionPatch(
    int? CourseId,
    int? TeacherId,
    int? GroupId,
    int? RoomId,
    DateOnly? Date,
    TimeOnly? Start,
    TimeOnly? End,
    string? Notes);
