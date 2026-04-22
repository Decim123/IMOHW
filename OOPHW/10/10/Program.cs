using Sched.App;
using Sched.Cli;

try
{
    var app = AppBootstrap.Create();
    var code = Commands.Run(args, app);
    Environment.ExitCode = code;
}
catch (CliExitException ex)
{
    Environment.ExitCode = ex.ExitCode;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = ExitCodes.DbError;
}
