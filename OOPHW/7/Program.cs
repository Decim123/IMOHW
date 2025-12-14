delegate void EffectHandler(CharacterContext ctx);

class CharacterContext
{
    public int Health { get; set; }
    public int Armor { get; set; }
    public bool Poisoned { get; set; }

    public override string ToString()
    {
        return $"HP={Health}, Armor={Armor}, Poisoned={Poisoned}";
    }
}

class EffectSystem
{
    public EffectHandler? Effects { get; set; }

    public void Run(CharacterContext ctx)
    {
        Effects?.Invoke(ctx);
    }
}

class Program
{
    static void Regenerate(CharacterContext ctx)
    {
        ctx.Health += 10;
    }

    static void ApplyPoison(CharacterContext ctx)
    {
        ctx.Health -= 5;
        ctx.Poisoned = true;
    }

    static void Main()
    {
        var character = new CharacterContext
        {
            Health = 50,
            Armor = 0,
            Poisoned = false
        };

        var system = new EffectSystem();

        EffectHandler shield = ctx => ctx.Armor += 3;
        Console.WriteLine("тик 1");        
        Console.WriteLine(character);
        system.Effects += Regenerate;
        Console.WriteLine("реген 10hp");
        system.Effects += ApplyPoison;
        Console.WriteLine("+яд");
        system.Effects += shield;
        Console.WriteLine("+3 армора");
        Console.WriteLine("После применения всех эффектов:");
        system.Run(character);
        Console.WriteLine(character);

        system.Effects -= ApplyPoison;
        Console.WriteLine("-яд");
        Console.WriteLine("тик 2");
        system.Run(character);
        Console.WriteLine(character);
    }
}
