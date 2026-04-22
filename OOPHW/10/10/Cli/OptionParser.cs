using System;
using System.Collections.Generic;

namespace Sched.Cli;

public sealed class OptionParser
{
    private readonly List<string> args;

    public OptionParser(string[] args)
    {
        this.args = new List<string>(args);
    }

    public string Cmd(int index) => index >= 0 && index < args.Count ? args[index] : "";

    public bool Has(string name)
    {
        return args.Contains(name);
    }

    public string? Get(string name)
    {
        var i = args.IndexOf(name);
        if (i < 0) return null;
        if (i + 1 >= args.Count) return null;
        var v = args[i + 1];
        if (v.StartsWith("--")) return null;
        return v;
    }

    public int? GetInt(string name)
    {
        var s = Get(name);
        if (s == null) return null;
        if (int.TryParse(s, out var n)) return n;
        return null;
    }

    public void Require(params string[] names)
    {
        foreach (var n in names)
        {
            if (Get(n) == null)
                throw new CliExitException(ExitCodes.ValidationError, $"Missing option {n}");
        }
    }

    public string RequireOneOfPositional(int index, string label)
    {
        var v = Cmd(index);
        if (string.IsNullOrWhiteSpace(v))
            throw new CliExitException(ExitCodes.ValidationError, $"Missing {label}");
        return v;
    }
}
