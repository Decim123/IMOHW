using System;

namespace Sched.Cli;

public sealed class CliExitException : Exception
{
    public int ExitCode { get; }

    public CliExitException(int exitCode, string message) : base(message)
    {
        ExitCode = exitCode;
    }
}
