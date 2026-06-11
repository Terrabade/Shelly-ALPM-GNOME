using PackageManager.AppImage;
using PackageManager.AppImage.AppImageV2;
using PackageManager.Wire;
using Shelly_CLI.Configuration;
using Shelly_CLI.Utility;
using Shelly.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.AppImage;

public class AppImageGetUpdates : AsyncCommand<AppImageDefaultSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AppImageDefaultSettings settings)
    {
        var installPath = ConfigManager.ReadConfig().AppImageInstallPath ?? XdgPaths.BinHome();
        var manager = new AppImageManagerV2(installPath);
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

        var result = await manager.CheckForAppImageUpdates();

        if (settings.Json)
        {
            if (Program.IsUiMode)
            {
                JsonPackFrame.WriteToStdout(result);
            }
            else
            {
                var json = System.Text.Json.JsonSerializer.Serialize(result,
                    ShellyCLIJsonContext.Default.ListAppImageUpdateDto);
                await using var stdout = Console.OpenStandardOutput();
                await using var writer = new StreamWriter(stdout, System.Text.Encoding.UTF8);
                await writer.WriteLineAsync(json);
                await writer.FlushAsync();
            }
        }
        else
        {
            foreach (var update in result)
            {
                AnsiConsole.MarkupLine($"[green]{update.Name} {update.Version} is available[/]");
            }

            if (result.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No updates available[/]");
            }
        }

        return 0;
    }
}