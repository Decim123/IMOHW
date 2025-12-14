using System;
using System.Collections;
using System.Collections.Generic;

enum DangerLevel
{
    Low,
    Medium,
    High,
    Extreme,
    Abyssal
}

sealed class Objective
{
    private readonly string code;
    private readonly string description;
    private readonly int requiredCount;

    public string Code => code;
    public string Description => description;
    public int RequiredCount => requiredCount;

    public Objective(string code, string description, int requiredCount)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException(nameof(code));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(nameof(description));

        if (requiredCount < 1)
            throw new ArgumentOutOfRangeException(nameof(requiredCount));

        this.code = code;
        this.description = description;
        this.requiredCount = requiredCount;
    }

    public override string ToString()
    {
        return $"{Description} ×{RequiredCount}";
    }
}

sealed class Quest
{
    private readonly string id;
    private readonly string title;
    private readonly DangerLevel danger;
    private readonly List<Objective> objectives;

    public string Id => id;
    public string Title => title;
    public DangerLevel Danger => danger;
    public IReadOnlyList<Objective> Objectives => objectives;

    public Quest(string id, string title, DangerLevel danger)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException(nameof(id));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException(nameof(title));

        this.id = id;
        this.title = title;
        this.danger = danger;
        objectives = new List<Objective>();
    }

    public void AddObjective(Objective objective)
    {
        if (objective == null)
            throw new ArgumentNullException(nameof(objective));

        objectives.Add(objective);
    }

    public override string ToString()
    {
        return $"{Title} [{Danger}] — целей: {Objectives.Count}";
    }
}

sealed class UndeadJournal : IEnumerable<Quest>
{
    private readonly List<Quest> quests;
    private readonly Dictionary<string, Quest> byId;

    public int Count => quests.Count;

    public UndeadJournal()
    {
        quests = new List<Quest>();
        byId = new Dictionary<string, Quest>();
    }

    public Quest this[int index]
    {
        get
        {
            if (index < 0 || index >= quests.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return quests[index];
        }
    }

    public Quest this[string id]
    {
        get
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (!byId.TryGetValue(id, out var quest))
                throw new KeyNotFoundException(id);

            return quest;
        }
    }

    public void Add(Quest quest)
    {
        if (quest == null)
            throw new ArgumentNullException(nameof(quest));

        if (byId.ContainsKey(quest.Id))
            throw new ArgumentException("Quest already exists.");

        quests.Add(quest);
        byId.Add(quest.Id, quest);
    }

    public bool RemoveById(string id)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        if (!byId.TryGetValue(id, out var quest))
            return false;

        byId.Remove(id);
        return quests.Remove(quest);
    }

    public IEnumerable<Quest> ByDanger(DangerLevel min)
    {
        foreach (var q in quests)
        {
            if (q.Danger >= min)
                yield return q;
        }
    }

    public IEnumerator<Quest> GetEnumerator()
    {
        return quests.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

static class Program
{
    static void Main()
    {
        var journal = new UndeadJournal();

        var bells = new Quest(
            "bells",
            "Зажечь Колокола Пробуждения",
            DangerLevel.High);

        bells.AddObjective(new Objective(
            "gargoyles",
            "Победить Колокольных Гаргулий",
            1));

        var lords = new Quest(
            "lords",
            "Пепел Повелителей",
            DangerLevel.Abyssal);

        lords.AddObjective(new Objective(
            "nito",
            "Победить Нито",
            1));
        lords.AddObjective(new Objective(
            "seath",
            "Победить Нагого Сита",
            1));

        journal.Add(bells);
        journal.Add(lords);

        Console.WriteLine("📜 Журнал:");
        foreach (var q in journal)
            Console.WriteLine(q);

        Console.WriteLine();
        Console.WriteLine("🔥 Опасность >= High:");
        foreach (var q in journal.ByDanger(DangerLevel.High))
            Console.WriteLine(q);

        Console.WriteLine();
        Console.WriteLine("🗑 Удаляем квест 'bells'");
        journal.RemoveById("bells");

        Console.WriteLine();
        Console.WriteLine("📜 Осталось:");
        foreach (var q in journal)
            Console.WriteLine(q);
    }
}
