namespace Sched.Cli;

public static class ExitCodes
{
    public const int Ok = 0;
    public const int ValidationError = 2;
    public const int DbError = 3;
    public const int Conflict = 4;
    public const int NotFound = 5;
}
