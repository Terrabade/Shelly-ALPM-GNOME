using System.Diagnostics.CodeAnalysis;
using PackageManager.Flatpak;
using PackageManager.Wire;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Flatpak;

public class FlathubGetRemote : Command<FlatpakListRemoteAppStreamSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] FlatpakListRemoteAppStreamSettings settings)
    {
        var result = settings.AppStreamName == "all"
            ? new FlatpakManager().GetAvailableAppsFromAppstreamJson("all", getAll: true)
            : new FlatpakManager().GetAvailableAppsFromAppstreamJson(settings.AppStreamName);

        MemPackFrame.WriteToStdout(result);
        return 0;
    }
}