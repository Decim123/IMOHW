class GameEntity
{
    private string name;
    private int health;

    public string Name
    {
        get => name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("не может быть пустой строкой", nameof(value));
            name = value;
        }
    }

    public int Health
    {
        get => health;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "должно быть >= 0");
            health = value;
        }
    }

    public GameEntity(string name, int health)
    {
        Name = name;
        Health = health;
    }

    public void TakeDamage(int dmg)
    {
        if (dmg < 0)
            throw new ArgumentOutOfRangeException(nameof(dmg), "должно быть >= 0");

        int newHealth = Health - dmg;
        Health = newHealth < 0 ? 0 : newHealth;
    }

    public bool IsAlive()
    {
        return Health > 0;
    }

    public virtual string Act()
    {
        return "GameEntity idles";
    }

    public override string ToString()
    {
        return $"{GetType().Name}(Имя={Name}, HP={Health})";
    }
}

class Warrior : GameEntity
{
    private int strength;

    public int Strength
    {
        get => strength;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "должно быть >= 0");
            strength = value;
        }
    }

    public Warrior(string name, int health, int strength) : base(name, health)
    {
        Strength = strength;
    }

    public string PowerStrike()
    {
        return $"Warrior использует PowerStrike наносит {Strength} урона!";
    }

    public override string Act()
    {
        return "Warrior использует sword.";
    }
}

class Villager : GameEntity
{
    private string occupation;

    public string Occupation
    {
        get => occupation;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("не может быть пустой строкой.", nameof(value));
            occupation = value;
        }
    }

    public Villager(string name, int health, string occupation) : base(name, health)
    {
        Occupation = occupation;
    }

    public string Work()
    {
        return $"Villager професия: {Occupation}.";
    }
}

class Merchant : Villager
{
    private int gold;

    public int Gold
    {
        get => gold;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "должно быть >= 0");
            gold = value;
        }
    }

    public Merchant(string name, int health, string occupation, int gold) : base(name, health, occupation)
    {
        Gold = gold;
    }

    public string Trade(int amount)
    {
        int result = Gold + amount;
        Gold = result < 0 ? 0 : result;
        return $" Трейд совершен. Gold: {Gold}";
    }

    public override string Act()
    {
        return "Merchant торгует с путешественниками";
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("Демонстрация иерархии игровых сущностей\n");

        var entity = new GameEntity("Entity", 10);
        var warrior = new Warrior("Conan", 30, 12);
        var villager = new Villager("Bob", 15, "farmer");
        var merchant = new Merchant("Marco", 20, "trader", 50);

        GameEntity[] party = { entity, warrior, villager, merchant };

        Console.WriteLine("Состояние объектов:");
        foreach (var p in party)
            Console.WriteLine(p);

        Console.WriteLine("\nПроверка виртуального метода Act():");
        foreach (var p in party)
            Console.WriteLine($"{p.Name}: {p.Act()}");

        Console.WriteLine("\nНевиртуальные действия потомков:");
        Console.WriteLine(warrior.PowerStrike());
        Console.WriteLine(villager.Work());
        Console.WriteLine(merchant.Work());

        Console.WriteLine("\nТорговля (Trade) и защита от ухода в минус:");
        Console.WriteLine(merchant.Trade(+25));
        Console.WriteLine(merchant.Trade(-200));

        Console.WriteLine("\nПолучение урона (TakeDamage) и проверка IsAlive():");
        warrior.TakeDamage(10);
        Console.WriteLine($"{warrior.Name} После получения урона: Health={warrior.Health}, Alive={warrior.IsAlive()}");
        warrior.TakeDamage(1000);
        Console.WriteLine($"{warrior.Name} После получения большего урона: Health={warrior.Health}, Alive={warrior.IsAlive()}");

        Console.WriteLine("\nГотово.");
    }
}
