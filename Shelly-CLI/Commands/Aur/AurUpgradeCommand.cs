using System.Diagnostics.CodeAnalysis;
using PackageManager.Alpm;
using PackageManager.Aur;
using PackageManager.Wire;
using Shelly_CLI.Configuration;
using Shelly_CLI.ConsoleLayouts;
using Shelly_CLI.Utility;
using Shelly.Utilities.Eventing;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Aur;

public class AurUpgradeCommand : AsyncCommand<AurUpgradeSettings>
{
    public override async Task<int> ExecuteAsync([NotNull] CommandContext context,
        [NotNull] AurUpgradeSettings settings)
    {
        if (Program.IsUiMode)
        {
            return await HandleUiModeUpgrade(settings);
        }

        AurPackageManager? manager = null;
        try
        {
            RootElevator.EnsureRootExectuion();
            manager = new AurPackageManager();
            await manager.Initialize(root: true, noCheck: !settings.Check);

            var updates = await manager.GetPackagesNeedingUpdate();

            if (updates.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]All AUR packages are up to date.[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[yellow]{updates.Count} AUR packages need updates:[/]");
            foreach (var pkg in updates)
            {
                AnsiConsole.MarkupLine(
                    $"  {pkg.Name.EscapeMarkup()}: {pkg.Version.EscapeMarkup()} -> {pkg.NewVersion.EscapeMarkup()}");
            }

            if (!settings.NoConfirm)
            {
                if (!AnsiConsole.Confirm("[yellow]Proceed with upgrade?[/]", defaultValue: true))
                {
                    AnsiConsole.MarkupLine("[yellow]Upgrade cancelled.[/]");
                    return 0;
                }
            }

            var cfg = ConfigManager.ReadConfig();
            var useSinglePane = settings.SinglePane
                                || string.Equals(cfg.OutputMode, "singlepane", StringComparison.OrdinalIgnoreCase)
                                || Console.IsOutputRedirected;

            var packageNames = updates.Select(u => u.Name).ToList();
            var result = await AurSinglePaneOutput.Output(manager, m => m.UpdatePackages(packageNames), settings.NoConfirm);
            if (!result)
            {
                AnsiConsole.MarkupLine("[red]Upgrade failed. See errors above.[/]");
                return 1;
            }

            AnsiConsole.MarkupLine("[green]Upgrade complete.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Upgrade failed:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }
        finally
        {
            manager?.Dispose();
        }

        return 0;
    }

    private static async Task<int> HandleUiModeUpgrade(AurUpgradeSettings settings)
    {
        AurPackageManager? manager = null;
        try
        {
            manager = new AurPackageManager();
            await manager.Initialize(root: true, noCheck: !settings.Check);

            var updates = await manager.GetPackagesNeedingUpdate();

            if (updates.Count == 0)
            {
                JsonPackFrame.WriteToStdout<Event>(new AlpmInformationalEvent(
                    AlpmEvents.InformationalOutput, "All AUR packages are up to date."));
                return 0;
            }

            manager.Question += (_, args) => QuestionHandler.HandleQuestion(args, true, settings.NoConfirm);
            manager.PkgbuildDiffRequest += (_, args) =>
                QuestionHandler.HandleQuestion(args, Program.IsUiMode, settings.NoConfirm);

            JsonPackFrame.WriteToStdout<Event>(new AlpmInformationalEvent(
                AlpmEvents.AurDownloadStart,
                $"{updates.Count} AUR packages need updates"));

            var packageNames = updates.Select(u => u.Name).ToList();
            var ok = await UiModeOutput.Run(manager, m => m.UpdatePackages(packageNames));

            JsonPackFrame.WriteToStdout<Event>(new AlpmInformationalEvent(
                ok ? AlpmEvents.AurPackageCompleted : AlpmEvents.AurPackageFailed,
                ok ? "Upgrade complete." : "Upgrade failed."));
            return ok ? 0 : 1;
        }
        finally
        {
            manager?.Dispose();
        }
    }
    
}