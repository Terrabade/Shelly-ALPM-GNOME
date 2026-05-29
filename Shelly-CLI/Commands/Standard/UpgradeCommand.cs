using System.Diagnostics;
using PackageManager.Alpm;
using PackageManager.Flatpak;
using PackageManager.Utilities;
using PackageManager.Wire;
using Shelly_CLI.Commands.Aur;
using Shelly_CLI.Configuration;
using Shelly_CLI.ConsoleLayouts;
using Shelly_CLI.Utility;
using Shelly.Utilities.Eventing;
using Spectre.Console;
using Spectre.Console.Cli;
using static System.Enum;

namespace Shelly_CLI.Commands.Standard;

public class UpgradeCommand : AsyncCommand<UpgradeSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpgradeSettings settings)
    {
        if (Program.IsUiMode)
        {
            return await HandleUiModeUpgrade(context, settings);
        }

        RootElevator.EnsureRootExectuion();
        var archNews = new ArchNews();
        await archNews.ExecuteAsync(context, new ArchNewsSettings());

        AnsiConsole.MarkupLine("[yellow]Performing full system upgrade...[/]");

        var manager = new AlpmManager();

        AnsiConsole.MarkupLine("[yellow]Checking for system updates...[/]");
        AnsiConsole.MarkupLine("[yellow]Initializing and syncing repositories...[/]");
        manager.Initialize(true);
        manager.Sync();
        var packagesNeedingUpdate = manager.GetPackagesNeedingUpdate();
        if (packagesNeedingUpdate.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]Standard Packages are up to date![/]");
        }

        if (packagesNeedingUpdate.Count > 0)
        {
            var config = ConfigManager.ReadConfig();
            var parsed =
                (SizeDisplay)Parse(typeof(SizeDisplay),
                    config.FileSizeDisplay);


            var table = new Table().Border(TableBorder.None);


            table.AddColumn(new TableColumn($"[bold green]Package ({packagesNeedingUpdate.Count})[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold green]Old Version[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold green]New Version[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold green]Net Change[/]").RightAligned());
            table.AddColumn(new TableColumn("[bold green]Download Size[/]").RightAligned());

            long totalDownloadSize = 0;
            long totalNetChange = 0;
            long totalInstalledSize = 0;

            foreach (var pkg in packagesNeedingUpdate)
            {
                long netChangeBytes = pkg.SizeDifference;

                totalDownloadSize += pkg.DownloadSize;
                totalNetChange += netChangeBytes;

                table.AddRow(
                    $"[green]{Markup.Escape(pkg.Name)}[/]",
                    $"[green]{Markup.Escape(pkg.CurrentVersion)}[/]",
                    $"[green]{Markup.Escape(pkg.NewVersion)}[/]",
                    $"[green]{FormatSize(parsed, netChangeBytes)}[/]",
                    $"[green]{FormatSize(parsed, pkg.DownloadSize)}[/]"
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold green]Total Download Size:[/]  {FormatSize(parsed, totalDownloadSize),10}");
            AnsiConsole.MarkupLine($"[bold green]Net Upgrade Size:[/]     {FormatSize(parsed, totalNetChange),10}");
            AnsiConsole.WriteLine();


            string FormatSize(SizeDisplay size, double bytes)
            {
                return size switch
                {
                    SizeDisplay.Bytes => $"{bytes:0} B",
                    SizeDisplay.Megabytes => $"{(bytes / 1048576.0):F2} MiB",
                    SizeDisplay.Gigabytes => $"{(bytes / 1073741824.0):F2} GiB",
                    _ => $"{bytes:0} B"
                };
            }

            if (!settings.NoConfirm)
            {
                if (!AnsiConsole.Confirm("[bold green]:: Proceed with installation?[/]"))
                {
                    AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                    return 0;
                }
            }

            AnsiConsole.MarkupLine("[yellow] Starting System Upgrade...[/]");
            var upgradeResult = await StandardSinglePaneOutput.Output(manager, x => x.SyncSystemUpdate(), settings.NoConfirm);
            manager.Dispose();
            if (!upgradeResult)
            {
                AnsiConsole.MarkupLine("[red]System upgrade failed. See errors above.[/]");
                return 1;
            }

            AnsiConsole.MarkupLine("[green]System upgraded successfully![/]");
        }

        if ((settings.Aur || settings.All) && ConfigManager.ReadConfig().AurEnabled)
        {
            var aurCommand = new AurUpgradeCommand();
            var aurSettings = new AurUpgradeSettings()
            {
                NoConfirm = settings.NoConfirm
            };
            var aurResult = await aurCommand.ExecuteAsync(context, aurSettings);
            if (aurResult != 0)
            {
                AnsiConsole.MarkupLine("[red]AUR upgrade failed.[/]");
            }
        }

        if ((settings.Flatpak || settings.All) && ConfigManager.ReadConfig().FlatPackEnabled)
        {
            var flatpakResult = ExecuteFlatpakUpdate();
            AnsiConsole.MarkupLine($"[yellow]{flatpakResult.EscapeMarkup()}[/]");
        }


        var (needsReboot, services) = RestartManager.CheckForRequiredRestarts();
        if (needsReboot)
        {
            AnsiConsole.MarkupLine("[bold red]⚠ A full system reboot is required![/]");
        }
        else if (services.Count > 0)
        {
            AnsiConsole.MarkupLine("[yellow]Restarting services...[/]");
            var failures = await RestartManager.RestartServicesAsync(services);
            if (failures.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]All services restarted successfully.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[bold red] The following services failed to restart:[/]");
                foreach (var (svc, error) in failures)
                    AnsiConsole.MarkupLine($"[red]  • {Markup.Escape(svc)}: {Markup.Escape(error)}[/]");
            }
        }

        return 0;
    }

    private static async Task<int> HandleUiModeUpgrade(CommandContext context, UpgradeSettings settings)
    {
        JsonPackFrame.WriteToStdout<Event>(new AlpmInformationalEvent(
            AlpmEvents.InformationalOutput, "Performing full system upgrade..."));

        using var manager = new AlpmManager();
        manager.Question += (_, args) => QuestionHandler.HandleQuestion(args, true, settings.NoConfirm);

        JsonPackFrame.WriteToStdout<Event>(new AlpmInformationalEvent(
            AlpmEvents.InformationalOutput, "Initializing and syncing repositories..."));
        manager.IntializeWithSync();

        var packagesNeedingUpdate = manager.GetPackagesNeedingUpdate();
        if (packagesNeedingUpdate.Count == 0)
        {
            JsonPackFrame.WriteToStdout<Event>(new AlpmInformationalEvent(
                AlpmEvents.InformationalOutput, "Standard Packages are up to date!"));
        }
        else
        {
            JsonPackFrame.WriteToStdout<Event>(new AlpmInformationalEvent(
                AlpmEvents.TransactionStart,
                $"{packagesNeedingUpdate.Count} packages need updates"));

            var ok = await UiModeOutput.Run(manager, m => m.SyncSystemUpdate());

            JsonPackFrame.WriteToStdout<Event>(new AlpmInformationalEvent(
                ok ? AlpmEvents.TransactionDone : AlpmEvents.TransactionFailed,
                ok ? "System upgraded successfully!" : "System upgrade failed."));
            if (!ok) return 1;
        }

        if ((settings.Aur || settings.All) && ConfigManager.ReadConfig().AurEnabled)
        {
            var aurCommand = new AurUpgradeCommand();
            var aurSettings = new AurUpgradeSettings { NoConfirm = settings.NoConfirm };
            var aurResult = await aurCommand.ExecuteAsync(context, aurSettings);
            if (aurResult != 0)
                JsonPackFrame.WriteToStdout<Event>(new AlpmErrorEvent(EventLevel.Error, "AUR upgrade failed."));
        }

        if ((settings.Flatpak || settings.All) && ConfigManager.ReadConfig().FlatPackEnabled)
        {
            var flatpakResult = ExecuteFlatpakUpdate();
            if (!string.IsNullOrEmpty(flatpakResult))
                JsonPackFrame.WriteToStdout<Event>(new AlpmInformationalEvent(
                    AlpmEvents.InformationalOutput, flatpakResult));
        }

        var (needsReboot, services) = RestartManager.CheckForRequiredRestarts();
        if (needsReboot)
        {
            JsonPackFrame.WriteToStdout<Event>(new AlpmInformationalEvent(
                AlpmEvents.InformationalOutput, "[RESTART_REQUIRED]reboot"));
        }
        else if (services.Count > 0)
        {
            var failures = await RestartManager.RestartServicesAsync(services);
            foreach (var (svc, error) in failures)
                JsonPackFrame.WriteToStdout<Event>(new AlpmErrorEvent(
                    EventLevel.Error, $"[RESTART_FAILED]service:{svc}|{error}"));
        }

        return 0;
    }

    //Execture as non-root so flatpaks upgrade correctly.
    private static string ExecuteFlatpakUpdate()
    {
        var sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");

        var exe = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];

        var startInfo = new ProcessStartInfo
        {
            FileName = "sudo",
            Arguments = $"-u {sudoUser} {exe} flatpak upgrade",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) return "Failed to start non-root Flatpak update process.";

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return process.ExitCode != 0 ? $"Flatpak update failed (Exit {process.ExitCode}): {error}" : output;
    }
}