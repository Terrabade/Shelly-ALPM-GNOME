using System.Diagnostics.CodeAnalysis;
using PackageManager.Flatpak;
using PackageManager.Ostree;
using Shelly_CLI.Utility;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Flatpak;

public class FlatpakRepair : Command<FlatpakRepairSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] FlatpakRepairSettings settings)
    {
        var manager = new FlatpakManager();
        var status = AnsiConsole.Status();
        
        // Step 1 - Scan all locally available refs, removing any that don't correspond to a deployed ref.

        // Step 2 - Verify each commit they point to, removing any invalid objects and noting any missing objects.

        // Step 3 - Remove any refs that had an invalid object, and any non-partial refs that had missing objects.

        // Step 4 - Prune all objects not referenced by a ref, which gets rid of any possibly invalid non-scanned objects.

        // Step 5 - Enumerate all deployed refs and re-install any that are not in the repo (or are partial for a non-subdir deploy).
        
        return 0;
    }
}