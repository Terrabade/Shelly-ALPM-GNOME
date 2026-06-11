using Shelly_CLI.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.AppImage;

public class AppImageSetInstallPath : AsyncCommand<AppImageInstallPathSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AppImageInstallPathSettings settings)
    {
        if (Directory.Exists(settings.Path))
        {
            var config = ConfigManager.ReadConfig();
            config.AppImageInstallPath = settings.Path;
            ConfigManager.SaveConfig(config);
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Error: Specified path does not exist.[/]");
        return 0;
    }
}