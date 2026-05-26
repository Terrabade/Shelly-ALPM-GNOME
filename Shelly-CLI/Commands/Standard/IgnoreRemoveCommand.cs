using System.ComponentModel;
using PackageManager.Alpm;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Standard;

public class IgnoreRemoveSettings : CommandSettings
{
    [CommandArgument(0, "<PACKAGE>")]
    [Description("Package name to remove from IgnorePkg")]
    public string PackageName { get; set; } = string.Empty;
}

public class IgnoreRemoveCommand : Command<IgnoreRemoveSettings>
{
    public override int Execute(CommandContext context, IgnoreRemoveSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.PackageName))
        {
            if (Program.IsUiMode)
                Console.Error.WriteLine("Error: No package specified");
            else
                AnsiConsole.MarkupLine("[red]Error: No package specified[/]");

            return 1;
        }

        if (!Program.IsUiMode)
            RootElevator.EnsureRootExectuion();

        try
        {
            using var manager = new AlpmManager();
            manager.UnignorePackage(settings.PackageName);

            if (Program.IsUiMode)
                Console.Error.WriteLine($"{settings.PackageName} removed from IgnorePkg list.");
            else
                AnsiConsole.MarkupLine(
                    $"[green]{settings.PackageName.EscapeMarkup()}[/] removed from IgnorePkg list.");

            return 0;
        }
        catch (Exception e)
        {
            if (Program.IsUiMode)
                Console.Error.WriteLine($"Error: {e.Message}");
            else
                AnsiConsole.MarkupLine($"[red]Error: {e.Message.EscapeMarkup()}[/]");

            return 1;
        }
    }
}