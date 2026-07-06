using System.CommandLine;
using PackageManager.Flatpak;
using Shelly.Cli.Outputs;

namespace Shelly.Cli.Commands.Flatpak;

public class Purify : GlobalSettingsCommand
{
    public static Command Create()
    {
        var command = new Command("purify", "Cleans all unused dependencies");
        
        command.SetAction(async (parseResult, _) =>
        {
            var instance = new Purify();
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

        var manager = new FlatpakManager();
        await FlatpakSinglePaneOutput.Output(console, manager, x => x.UninstallUnused(), true);
    }

    public override async ValueTask ExecuteUiMode()
    {
        var manager = new FlatpakManager();
        await UiModeOutput.Run(manager, x => x.UninstallUnused());
    }
}