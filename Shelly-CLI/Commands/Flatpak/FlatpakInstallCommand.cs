using System.Diagnostics.CodeAnalysis;
using PackageManager.Flatpak;
using Shelly_CLI.Utility;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Flatpak;

public class FlatpakInstallCommand : Command<FlatpakPackageSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] FlatpakPackageSettings settings)
    {
        if (Program.IsUiMode)
        {
            return HandleUiModeInstall(settings);
        }

        AnsiConsole.MarkupLine("[yellow]Installing flatpak app...[/]");
        var manager = new FlatpakManager();
        manager.FlatpakEvent += (sender, args) =>
        {
            AnsiConsole.MarkupLine($"[yellow]{args.Message.EscapeMarkup()}[/]");
        };
        manager.InstallApp(settings.Packages, settings.Remote, settings.IsUser, settings.Branch ?? "stable",
            settings.IsRuntime);
        return 0;
    }

    private static int HandleUiModeInstall(FlatpakPackageSettings settings)
    {
        Console.Error.WriteLine("Installing flatpak app...");
        var manager = new FlatpakManager();
        manager.FlatpakEvent += (sender, args) => { Console.Error.WriteLine(args.Message); };
        manager.InstallApp(settings.Packages, settings.Remote, settings.IsUser);
        return 0;
    }
}