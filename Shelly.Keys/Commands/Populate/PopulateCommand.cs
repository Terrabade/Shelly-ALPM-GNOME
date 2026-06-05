using Spectre.Console.Cli;

namespace Shelly.Keys.Commands.Populate;

public class PopulateCommand : AsyncCommand<Settings>
{
    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        RootElevator.EnsureRootExectuion();
        throw new NotImplementedException();
    }
}