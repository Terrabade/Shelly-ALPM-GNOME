using System.CommandLine;
using PackageManager.AppImage.AppImageV2;
using Pastel;
using Shelly.Cli.Interactions;
using Shelly.Cli.Outputs;
using Shelly.Utilities;
using Shelly.Utilities.Eventing;

namespace Shelly.Cli.Commands.AppImage;

public partial class AppImageSyncMeta : GlobalSettingsCommand
{
    private string? Package { get; set; }

    public static Command Create()
    {
        var package = new Argument<string?>("package")
            { Description = "The search query for the AppImage", Arity = ArgumentArity.ZeroOrOne };

        var command = new Command("sync-meta", "Syncs meta data for an AppImage") { package };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var instance = new AppImageSyncMeta
            {
                Package = parseResult.GetValue(package)
            };
            GlobalOptions.Apply(instance, parseResult);
            await instance.ExecuteAsync(ShellyConsoleFactory.Create());
            return 0;
        });

        return command;
    }

    public override async ValueTask ExecuteAsync(IShellyConsole console)
    {
        if (UiMode)
        {
            await ExecuteUiMode();
            return;
        }

        var installDir = string.IsNullOrEmpty(ConfigManager.ReadConfig().AppImageInstallPath)
            ? XdgPaths.BinHome() 
            : ConfigManager.ReadConfig().AppImageInstallPath;
        if (!Directory.Exists(installDir))
        {
            console.WriteLine(AnsiUtilities.Colorize(
                $"Info: {installDir} directory does not exist. No AppImages to sync.", ConsoleColor.Yellow));
            return;
        }

        var manager = new AppImageManagerV2(ConfigManager.ReadConfig().AppImageInstallPath ?? "");
        var appImages = await manager.GetAppImagesFromLocalDb();

        if (!string.IsNullOrEmpty(Package))
        {
            var matches = appImages
                .Where(f => f.Name.Contains(Package, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                console.WriteLine(AnsiUtilities.Colorize($"No AppImage matching \"{Package}\" found in database",
                    ConsoleColor.Yellow));
                return;
            }

            await AppImageSinglePaneOutput.Output(console, manager, x => x.SyncAppImageMeta([
                matches.First().Name
            ]));
        }
        else
        {
            await AppImageSinglePaneOutput.Output(console, manager,
                x => x.SyncAppImageMeta(appImages.Select(a => a.Name).ToList()));
        }
    }

    public override async ValueTask ExecuteUiMode()
    {
        var installDir = string.IsNullOrEmpty(ConfigManager.ReadConfig().AppImageInstallPath)
            ? XdgPaths.BinHome() 
            : ConfigManager.ReadConfig().AppImageInstallPath;
        if (!Directory.Exists(installDir))
        {
            UiFrames.Error($"Info: {installDir} directory does not exist. No AppImages to sync.");
            return;
        }

        var manager = new AppImageManagerV2(ConfigManager.ReadConfig().AppImageInstallPath ?? "");
        var appImages = await manager.GetAppImagesFromLocalDb();

        if (!string.IsNullOrEmpty(Package))
        {
            var matches = appImages
                .Where(f => f.Name.Contains(Package, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                UiFrames.Info($"No AppImage matching \"{Package}\" found in database");
                return;
            }

            await UiModeOutput.Run(manager,
                x => x.SyncAppImageMeta([matches.First().Name]));
        }
        else
        {
            await UiModeOutput.Run(manager, x => x.SyncAppImageMeta(appImages.Select(a => a.Name).ToList()));
        }
    }
}