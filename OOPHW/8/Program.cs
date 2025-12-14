using System;
using System.Collections.Generic;

class BatteryMonitor
{
    public delegate void BatteryEventHandler(
        BatteryMonitor sender,
        int level);

    public event BatteryEventHandler? LevelChanged;

    private EventHandler<int>? criticalHandlers;

    public event EventHandler<int> CriticalLowReached
    {
        add
        {
            criticalHandlers += value;
            Console.WriteLine(
                "CriticalLowReached: подписчик добавлен");
        }
        remove
        {
            criticalHandlers -= value;
            Console.WriteLine(
                "CriticalLowReached: подписчик удалён");
        }
    }

    private readonly Random random;
    private int level;

    public int Level => level;

    public BatteryMonitor(int startLevel, int seed)
    {
        if (startLevel < 5 || startLevel > 100)
            throw new ArgumentOutOfRangeException(
                nameof(startLevel));

        level = startLevel;
        random = new Random(seed);
    }

    public void Start(int steps)
    {
        if (steps < 1)
            throw new ArgumentOutOfRangeException(
                nameof(steps));

        for (int i = 0; i < steps; i++)
        {
            int step = random.Next(4, 13);
            int next = level - step;

            if (next < 5)
                next = 5;

            level = next;

            LevelChanged?.Invoke(this, level);

            if (level < 15)
                criticalHandlers?.Invoke(this, level);
        }
    }
}

class ConsoleHud
{
    public void OnLevelChanged(
        BatteryMonitor sender,
        int level)
    {
        Console.WriteLine($"Уровень: {level}%");
    }

    public void OnCriticalLow(
        object? sender,
        int level)
    {
        Console.WriteLine(
            $"Низкий заряд: {level}% — включите энергосбережение");
    }
}

class LowLevelStats
{
    private int below30Count;

    public void OnLevelChanged(
        BatteryMonitor sender,
        int level)
    {
        if (level < 30)
            below30Count++;
    }

    public void Report()
    {
        Console.WriteLine(
            $"Ниже 30% было {below30Count} раза");
    }
}

static class Program
{
    static void Main()
    {
        var monitor = new BatteryMonitor(
            startLevel: 100,
            seed: 7);

        var hud = new ConsoleHud();
        var stats = new LowLevelStats();

        monitor.LevelChanged += hud.OnLevelChanged;
        monitor.LevelChanged += stats.OnLevelChanged;

        monitor.CriticalLowReached += hud.OnCriticalLow;

        Console.WriteLine("Старт мониторинга батареи\n");

        monitor.Start(steps: 10);

        Console.WriteLine("\nОтписываем HUD от критического события\n");

        monitor.CriticalLowReached -= hud.OnCriticalLow;

        Console.WriteLine("\nОтчёт статистики\n");

        stats.Report();
    }
}
