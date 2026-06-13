using PackageManager.AppImage;
using PackageManager.AppImage.AppImageV2;
using Shelly_CLI.Configuration;
using Shelly_CLI.Utility;
using Shelly.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.AppImage;

public class AppImageSyncMeta : AsyncCommand<AppImageSyncMetaSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AppImageSyncMetaSettings settings)
    {
        var installDir = ConfigManager.ReadConfig().AppImageInstallPath ?? XdgPaths.BinHome();
        if (!Directory.Exists(installDir))
        {
            AnsiConsole.MarkupLine($"[yellow]Info: {installDir} directory does not exist. No AppImages to sync.[/]");
            return 0;
        }

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

        if (!string.IsNullOrEmpty(settings.Query))
        {
            var appImages = Directory.GetFiles(installDir, "*.AppImage", SearchOption.TopDirectoryOnly);
            var matches = appImages
                .Where(f => Path.GetFileName(f).Contains(settings.Query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No AppImage matching \"{settings.Query}\" found in {installDir}[/]");
                return 0;
            }

            string targetAppImage;
            if (matches.Count == 1)
            {
                targetAppImage = matches[0];
            }
            else
            {
                if (settings.NoConfirm)
                {
                    targetAppImage = matches[0];
                    AnsiConsole.MarkupLine(
                        $"[yellow]Multiple matches found, picking first one due to --no-confirm: {Path.GetFileName(targetAppImage)}[/]");
                }
                else
                {
                    targetAppImage = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Multiple AppImages matched. Which one do you want to [green]sync[/]?")
                            .AddChoices(matches.Select(Path.GetFileName).Cast<string>())
                    );
                    targetAppImage = matches.First(m => Path.GetFileName(m) == targetAppImage);
                }
            }

            var package = new List<string> { Path.GetFileNameWithoutExtension(targetAppImage) };
            await manager.SyncAppImageMeta(package);
        }
        else
        {
            var appImages = Directory.GetFiles(installDir, "*.AppImage", SearchOption.TopDirectoryOnly);
            var appImageNames = appImages.Select(Path.GetFileNameWithoutExtension).Cast<string>().ToList();
            await manager.SyncAppImageMeta(appImageNames);
        }

        return 0;
    }
}