using System;
using System.Collections.Generic;
using System.Linq;
using Sched.Domain;
using Sched.Storage;

namespace Sched.Services;

public sealed class RoomService
{
    private readonly DatabaseStore store;

    public RoomService(DatabaseStore store)
    {
        this.store = store;
    }

    public Room Add(RoomCreate create)
    {
        if (string.IsNullOrWhiteSpace(create.Code)) throw new ArgumentException(nameof(create.Code));
        if (create.Capacity <= 0) throw new ArgumentException(nameof(create.Capacity));

        var db = store.Load();
        if (db.Rooms.Any(r => r.Code == create.Code)) throw new InvalidOperationException("Room exists");

        var room = new Room
        {
            Id = db.NextRoomId++,
            Code = create.Code,
            Capacity = create.Capacity,
            Building = create.Building ?? "",
            AttrJson = create.AttrJson ?? "{}"
        };

        db.Rooms.Add(room);
        store.Save(db);
        return room;
    }

    public IReadOnlyList<Room> List()
    {
        return store.Load().Rooms;
    }

    public Room? FindByIdOrCode(string key)
    {
        var db = store.Load();
        if (int.TryParse(key, out var id)) return db.Rooms.FirstOrDefault(r => r.Id == id);
        return db.Rooms.FirstOrDefault(r => r.Code == key);
    }

    public bool Update(string key, RoomPatch patch, out Room? updated)
    {
        var db = store.Load();
        Room? room;
        if (int.TryParse(key, out var id)) room = db.Rooms.FirstOrDefault(r => r.Id == id);
        else room = db.Rooms.FirstOrDefault(r => r.Code == key);

        if (room == null)
        {
            updated = null;
            return false;
        }

        if (patch.Code != null)
        {
            if (string.IsNullOrWhiteSpace(patch.Code)) throw new ArgumentException("code");
            if (db.Rooms.Any(r => r.Id != room.Id && r.Code == patch.Code)) throw new InvalidOperationException("Room exists");
            room.Code = patch.Code;
        }

        if (patch.Capacity.HasValue)
        {
            if (patch.Capacity.Value <= 0) throw new ArgumentException("capacity");
            room.Capacity = patch.Capacity.Value;
        }

        if (patch.Building != null) room.Building = patch.Building;
        if (patch.AttrJson != null) room.AttrJson = patch.AttrJson;

        store.Save(db);
        updated = room;
        return true;
    }

    public bool Delete(string key)
    {
        var db = store.Load();
        Room? room;
        if (int.TryParse(key, out var id)) room = db.Rooms.FirstOrDefault(r => r.Id == id);
        else room = db.Rooms.FirstOrDefault(r => r.Code == key);

        if (room == null) return false;
        db.Rooms.Remove(room);
        store.Save(db);
        return true;
    }
}
