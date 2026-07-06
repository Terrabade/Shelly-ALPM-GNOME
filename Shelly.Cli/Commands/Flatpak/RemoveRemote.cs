using System.CommandLine;
using PackageManager.Flatpak;
using PackageManager.Flatpak.Enums;
using static Shelly.Cli.Interactions.AnsiUtilities;

namespace Shelly.Cli.Commands.Flatpak;

public class RemoveRemote : GlobalSettingsCommand
{
    private string RemoteName { get; set; } = string.Empty;
    private bool SystemWide { get; set; }

    public static Command Create()
    {
        var remoteName = new Argument<string>("remote") { Description = "Flatpak remote name ID (e.g., flathub)" };
        var system = new Option<bool>("--system", "-s") { Description = "Remove the remote system-wide", DefaultValueFactory = _ => true };

        var command = new Command("remove-remotes", "Removes a flatpak remote")
        {
            remoteName, system
        };

        command.SetAction(async (parseResult, _) =>
        {
            var instance = new RemoveRemote
            {
                RemoteName = parseResult.GetValue(remoteName) ?? string.Empty,
                SystemWide = parseResult.GetValue(system)
            };
            GlobalOptions.Apply(instance, parseResult);
            await instance.ExecuteAsync(ShellyConsoleFactory.Create());
            return 0;
        });

        return command;
    }

    public override ValueTask ExecuteAsync(IShellyConsole console)
    {
        console.WriteLine(Colorize($"Removing remote {RemoteName}", ConsoleColor.Red));
        var result = FlatpakManager.RemoveRemote(RemoteName, SystemWide ? InstallLevel.System : InstallLevel.User);
        console.WriteLine(result);
        return ValueTask.CompletedTask;
    }

    public override ValueTask ExecuteUiMode() => ValueTask.CompletedTask;
}
