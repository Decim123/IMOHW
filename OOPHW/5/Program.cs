using System;
using System.Collections.Generic;

interface ITrack
{
    string Title { get; }
    int DurationSec { get; }
    int PositionSec { get; set; }

    void Seek(int seconds)
    {
        if (seconds <= 0)
            return;

        int next = PositionSec + seconds;
        PositionSec = Math.Min(next, DurationSec);
    }
}

interface IPlaylist
{
    int Count { get; }
    void Add(ITrack track);
}

sealed class BasicTrack : ITrack
{
    private readonly string title;
    private readonly int durationSec;
    private int positionSec;

    public string Title => title;
    public int DurationSec => durationSec;

    public int PositionSec
    {
        get => positionSec;
        set
        {
            if (value < 0)
                positionSec = 0;
            else if (value > DurationSec)
                positionSec = DurationSec;
            else
                positionSec = value;
        }
    }

    public BasicTrack(string title, int durationSec)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(nameof(title));

        if (durationSec <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationSec));

        this.title = title;
        this.durationSec = durationSec;
        positionSec = 0;
    }

    public override string ToString()
    {
        return $"Трек: {Title}, {PositionSec}/{DurationSec} сек";
    }
}

sealed class ManagedPlaylist : ITrack, IPlaylist
{
    private readonly List<ITrack> tracks;
    private int positionSec;

    public string Title { get; }
    public int CrossfadeSec { get; }

    public int Count => tracks.Count;

    public int DurationSec
    {
        get
        {
            if (tracks.Count == 0)
                return 0;

            int sum = 0;
            foreach (var t in tracks)
                sum += t.DurationSec;

            int fades = CrossfadeSec * (tracks.Count - 1);
            return sum + fades;
        }
    }

    public int PositionSec
    {
        get => positionSec;
        set
        {
            if (value < 0)
                positionSec = 0;
            else if (value > DurationSec)
                positionSec = DurationSec;
            else
                positionSec = value;
        }
    }

    public ManagedPlaylist(string title, int crossfadeSec)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(nameof(title));

        if (crossfadeSec < 0)
            throw new ArgumentOutOfRangeException(nameof(crossfadeSec));

        Title = title;
        CrossfadeSec = crossfadeSec;
        tracks = new List<ITrack>();
        positionSec = 0;
    }

    public void Add(ITrack track)
    {
        if (track == null)
            throw new ArgumentNullException(nameof(track));

        tracks.Add(track);
        PositionSec = PositionSec;
    }

    public void Seek(int seconds)
    {
        if (seconds <= 0 || tracks.Count == 0)
            return;

        int total = DurationSec;
        int target = PositionSec + seconds;

        if (target >= total)
        {
            PositionSec = total;
            return;
        }

        int timeline = 0;
        int index = 0;

        while (index < tracks.Count)
        {
            int endTrack = timeline + tracks[index].DurationSec;

            if (target < endTrack)
            {
                PositionSec = target;
                return;
            }

            timeline = endTrack;

            if (index < tracks.Count - 1)
                timeline += CrossfadeSec;

            if (target < timeline)
            {
                PositionSec = target;
                return;
            }

            index++;
        }

        PositionSec = total;
    }

    public override string ToString()
    {
        return $"Плейлист: {Title}, {PositionSec}/{DurationSec} сек";
    }
}

class Program
{
    static void Main()
    {
        ITrack t1 = new BasicTrack("Intro", 30);
        ITrack t2 = new BasicTrack("Main", 120);
        ITrack t3 = new BasicTrack("Outro", 45);

        var pl = new ManagedPlaylist("My Playlist", 5);

        pl.Add(t1);
        pl.Add(t2);
        pl.Add(t3);

        ITrack playlistAsTrack = pl;

        Console.WriteLine(pl);
        Console.WriteLine($"Треков в плейлисте: {pl.Count}");
        Console.WriteLine();

        Console.WriteLine("BasicTrack: seek +20 (из интерфейса)");
        t1.Seek(20);
        Console.WriteLine(t1);
        Console.WriteLine();

        Console.WriteLine("Playlist: seek +20 (внутри трека)");
        playlistAsTrack.Seek(20);
        Console.WriteLine(pl);

        Console.WriteLine("Playlist: seek +20 (переход + кроссфейд)");
        playlistAsTrack.Seek(20);
        Console.WriteLine(pl);

        Console.WriteLine("Playlist: seek +999 (упор в конец)");
        playlistAsTrack.Seek(999);
        Console.WriteLine(pl);
    }
}
