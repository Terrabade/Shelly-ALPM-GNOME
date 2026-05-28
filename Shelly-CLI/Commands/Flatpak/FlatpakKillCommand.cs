using System.Diagnostics.CodeAnalysis;
using PackageManager.Flatpak;
using Shelly_CLI.Utility;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Flatpak;

public class FlatpakKillCommand : Command<FlatpakPackageSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] FlatpakPackageSettings settings)
    {
        if (Program.IsUiMode)
        {
            return HandleUiModeKill(settings);
        }

        AnsiConsole.MarkupLine("[yellow]Killing selected flatpak app...[/]");
        var flatpakManager = new FlatpakManager();
        flatpakManager.FlatpakEvent += (sender, args) =>
        {
            AnsiConsole.MarkupLine($"[yellow]{args.Message.EscapeMarkup()}[/]");
        };
        flatpakManager.KillApp(settings.Packages);

        return 0;
    }

    private static int HandleUiModeKill(FlatpakPackageSettings settings)
    {
        Console.Error.WriteLine("Killing selected flatpak app...");
        var manager = new FlatpakManager();
        manager.FlatpakEvent += (sender, args) => { Console.Error.WriteLine(args.Message); };
        manager.KillApp(settings.Packages);
        return 0;
    }
}