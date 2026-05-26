using PackageManager.Alpm;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Standard;

public class IgnoreListSettings : CommandSettings;

public class IgnoreListCommand : Command<IgnoreListSettings>
{
    public override int Execute(CommandContext context, IgnoreListSettings settings)
    {
        using var manager = new AlpmManager();
        var ignoredPackages = manager.GetIgnoredPackages();
        if (ignoredPackages.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]IgnorePkg list is empty.[/]");
            return 0;
        }

        foreach (var package in ignoredPackages)
            AnsiConsole.WriteLine(package);

        return 0;
    }
}