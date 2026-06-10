using PackageManager.AppImage;
using PackageManager.AppImage.AppImageV2;
using Shelly_CLI.Configuration;
using Shelly_CLI.Utility;
using Shelly.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.AppImage;

// No confirm intentionally not implemented here as user interaction is required.
public class AppImageConfigUpdates : AsyncCommand<AppImageConfigUpdatesSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AppImageConfigUpdatesSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]Error: AppImage name is required[/]");
            return 1;
        }

        if (string.IsNullOrEmpty(settings.UpdateUrl))
        {
            AnsiConsole.MarkupLine("[red]Error: Update URL is required[/]");
            return 1;
        }

        var installDir = ConfigManager.ReadConfig().AppImageInstallPath ?? XdgPaths.BinHome();

        if (!Directory.Exists(installDir))
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Info: {installDir} directory does not exist. No AppImages to set config on.[/]");
            return 0;
        }

        var appImages = Directory.GetFiles(installDir, "*.AppImage", SearchOption.TopDirectoryOnly);
        var matches = appImages
            .Where(f => Path.GetFileName(f).Contains(settings.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No AppImage matching \"{settings.Name}\" found in {installDir}[/]");
            return 0;
        }

        string targetAppImage;
        if (matches.Count == 1)
        {
            targetAppImage = matches[0];
        }
        else
        {
            targetAppImage = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Multiple AppImages matched. Which one do you want to [red]configure[/]?")
                    .AddChoices(matches.Select(Path.GetFileName).Cast<string>())
            );
            targetAppImage = matches.First(m => Path.GetFileName(m) == targetAppImage);
        }

        targetAppImage = Path.GetFileNameWithoutExtension(targetAppImage);

        var manager = new AppImageManagerV2(ConfigManager.ReadConfig().AppImageInstallPath ?? "");
        ;
        if (Program.IsUiMode)
        {
            manager.MessageEvent += (_, e) => UiFrames.Info(e.Message);
            manager.ErrorEvent += (_, e) => UiFrames.Error(e.Error);
        }
        else
        {
            manager.MessageEvent += (_, e) => AnsiConsole.MarkupLine($"[blue][[INFO]][/] {e.Message.EscapeMarkup()}");
            manager.ErrorEvent += (_, e) => AnsiConsole.MarkupLine($"[red][[ERROR]][/] {e.Error.EscapeMarkup()}");
        }

        var success = await manager.AppImageConfigureUpdates(settings.UpdateUrl, targetAppImage, settings.UpdateType,
            settings.AllowPrerelease);

        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Successfully configured updates for {targetAppImage}[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]Failed to configure updates for {targetAppImage}. Is it installed?[/]");
        return 1;
    }
}