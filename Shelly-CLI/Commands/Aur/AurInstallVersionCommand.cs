using System.Diagnostics.CodeAnalysis;
using PackageManager.Alpm;
using PackageManager.Aur;
using Shelly_CLI.Utility;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Aur;

public class AurInstallVersionCommand : AsyncCommand<AurInstallVersionSettings>
{
    public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] AurInstallVersionSettings settings)
    {
        if (Program.IsUiMode)
        {
            return await HandleUiModeInstallVersion(settings);
        }

        RootElevator.EnsureRootExectuion();
        AurPackageManager? manager = null;
        if (string.IsNullOrWhiteSpace(settings.Package))
        {
            AnsiConsole.MarkupLine("[red]No package specified.[/]");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(settings.Commit))
        {
            AnsiConsole.MarkupLine("[red]No commit specified.[/]");
            return 1;
        }

        try
        {
            manager = new AurPackageManager();
            await manager.Initialize(root: true, noCheck: !settings.Check);

            manager.InformationalEvent += (_, args) =>
            {
                var statusColor = args.EventType switch
                {
                    AlpmEventType.AurDownloadStart   => "yellow",
                    AlpmEventType.AurBuildStart      => "blue",
                    AlpmEventType.AurInstallStart    => "cyan",
                    AlpmEventType.AurPackageCompleted => "green",
                    AlpmEventType.AurPackageFailed   => "red",
                    _ => null
                };
                if (statusColor == null) return;

                AnsiConsole.MarkupLine(
                    $"[{statusColor}][[{args.CurrentIndex}/{args.TotalCount}]] {(args.PackageName ?? "").EscapeMarkup()}: {args.EventType}[/]" +
                    (!string.IsNullOrEmpty(args.Message) ? $" - {args.Message.EscapeMarkup()}" : ""));
            };

            AnsiConsole.MarkupLine(
                $"[yellow]Installing AUR package {settings.Package.EscapeMarkup()} at commit {settings.Commit.EscapeMarkup()}[/]");
            await manager.InstallPackageVersion(settings.Package, settings.Commit);
            AnsiConsole.MarkupLine("[green]Installation complete.[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Installation failed:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }
        finally
        {
            manager?.Dispose();
        }
    }

    private static async Task<int> HandleUiModeInstallVersion(AurInstallVersionSettings settings)
    {
        AurPackageManager? manager = null;
        if (string.IsNullOrWhiteSpace(settings.Package))
        {
            Console.Error.WriteLine("Error: No package specified.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(settings.Commit))
        {
            Console.Error.WriteLine("Error: No commit specified.");
            return 1;
        }

        try
        {
            manager = new AurPackageManager();
            await manager.Initialize(root: true, noCheck: !settings.Check);

            manager.InformationalEvent += (_, args) =>
            {
                if (args.PackageName == null) return;
                Console.Error.WriteLine($"[{args.CurrentIndex}/{args.TotalCount}] {args.PackageName}: {args.EventType}" +
                    (!string.IsNullOrEmpty(args.Message) ? $" - {args.Message}" : ""));
            };

            Console.Error.WriteLine($"Installing AUR package {settings.Package} at commit {settings.Commit}");
            await manager.InstallPackageVersion(settings.Package, settings.Commit);
            Console.Error.WriteLine("Installation complete.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Installation failed: {ex.Message}");
            return 1;
        }
        finally
        {
            manager?.Dispose();
        }
    }
}
