class Exhibit
{
    public int Id { get; }
    public string Title { get; }
    public string Author { get; }

    public Exhibit(int id, string title, string author)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "Id должен быть положительным");
        Id = id;
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Author = author ?? throw new ArgumentNullException(nameof(author));
    }

    public override string ToString() => $"#{Id} {Title} - {Author}";
}

class Hall
{
    public string Name { get; }
    public double SizeSqM { get; }
    public IReadOnlyList<Exhibit> Exhibits => _exhibits.AsReadOnly();

    private readonly List<Exhibit> _exhibits = new();

    public Hall(string name, double sizeSqM)
    {
        if (sizeSqM <= 0) throw new ArgumentOutOfRangeException(nameof(sizeSqM), "Размер зала должен быть положительным");
        Name = name ?? throw new ArgumentNullException(nameof(name));
        SizeSqM = sizeSqM;
    }

    public void AddExhibit(Exhibit exhibit)
    {
        if (exhibit is null) throw new ArgumentNullException(nameof(exhibit));
        if (_exhibits.Any(e => e.Id == exhibit.Id))
            throw new InvalidOperationException($"Экспонат с Id={exhibit.Id} уже размещён в зале {Name}");
        _exhibits.Add(exhibit);
    }

    public override string ToString() => $"Зал {Name} ({SizeSqM:0.##} м2), экспонатов: {_exhibits.Count}";
}

class Museum
{
    public string Name { get; }
    public double TotalAreaSqM { get; }
    public IReadOnlyList<Hall> Halls => _halls.AsReadOnly();

    private readonly List<Hall> _halls = new();

    public Museum(string name, double totalAreaSqM)
    {
        if (totalAreaSqM <= 0) throw new ArgumentOutOfRangeException(nameof(totalAreaSqM), "Площадь музея должна быть положительной");
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TotalAreaSqM = totalAreaSqM;
    }

    public void AddHall(Hall hall)
    {
        if (hall is null) throw new ArgumentNullException(nameof(hall));
        if (_halls.Any(h => string.Equals(h.Name, hall.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Зал с названием {hall.Name} уже существует в музее");
        _halls.Add(hall);
    }

    public int ExhibitCount => _halls.Sum(h => h.Exhibits.Count);
    public double UsedHallAreaSqM => _halls.Sum(h => h.SizeSqM);

    public IEnumerable<Exhibit> AllExhibits() => _halls.SelectMany(h => h.Exhibits);

    public override string ToString()
        => $"Музей {Name}: площадь помещений {TotalAreaSqM:0.##} м2, залов {_halls.Count}, экспонатов {ExhibitCount}";
}

class Program
{
    static void Main()
    {
        var museum = new Museum("Городской музей", 1800);

        var firstHall = new Hall("1-й зал", 420);
        var secondHall = new Hall("2-й зал", 360);
        var thirdHall = new Hall("3-й зал", 280);

        museum.AddHall(firstHall);
        museum.AddHall(secondHall);
        museum.AddHall(thirdHall);

        firstHall.AddExhibit(new Exhibit(1, "Картина", "Писатель"));
        firstHall.AddExhibit(new Exhibit(2, "Рисунок", "Рисователь"));

        secondHall.AddExhibit(new Exhibit(10, "Впечатление. Восход солнца", "Клод Моне"));
        secondHall.AddExhibit(new Exhibit(11, "Звёздная ночь", "Винсент ван Гог"));

        thirdHall.AddExhibit(new Exhibit(20, "Чёрный квадрат", "Казимир Малевич"));
        thirdHall.AddExhibit(new Exhibit(21, "Постоянство памяти", "Сальвадор Дали"));

        Console.WriteLine(museum);
        Console.WriteLine($"Суммарная площадь залов: {museum.UsedHallAreaSqM:0.##} м2");
        Console.WriteLine();

        foreach (var hall in museum.Halls)
        {
            Console.WriteLine(hall);
            foreach (var ex in hall.Exhibits)
                Console.WriteLine($"  - {ex}");
            Console.WriteLine();
        }

        Console.WriteLine("Список всех экспонатов:");
        foreach (var ex in museum.AllExhibits().OrderBy(e => e.Id))
            Console.WriteLine($"• {ex}");
    }
}
