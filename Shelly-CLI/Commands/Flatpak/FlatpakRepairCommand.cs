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
        var flatpakManager = new FlatpakManager();
        var ostreeManager = new OstreeManager();
        var status = AnsiConsole.Status();
        
        // Step 1 - Scan all locally available refs, removing any that don't correspond to a deployed ref.

        var repositories = flatpakManager.GetRepositoryPaths();
        
        // Testing mechanism to see if it works.
        foreach (var repo in repositories)
        {
            var refs = ostreeManager.ListRefs(repo);

            var tree = new Tree(
                $"[yellow]{repo}[/]");

            foreach (var reference in refs)
            {
                tree.AddNode(
                    $"[green]{reference.Remote}[/]: {reference.Ref}");
            }

            AnsiConsole.Write(tree);
            
        }

        var installed = flatpakManager.SearchInstalled();
        var installedRefs =
            installed
                .Select(x => x.FullRef)
                .ToHashSet();

        foreach (var repo in repositories)
        {
            var refs =
                ostreeManager.ListRefs(repo);

            foreach (var reference in refs)
            {
                if (!installedRefs.Contains(
                        reference.FullRef))
                {
                    AnsiConsole.MarkupLine(
                        $"[red]Orphan ref:[/] {reference.FullRef}");
                }
            }
        }
        
        // Step 2 - Verify each commit they point to, removing any invalid objects and noting any missing objects.

        // Step 3 - Remove any refs that had an invalid object, and any non-partial refs that had missing objects.

        // Step 4 - Prune all objects not referenced by a ref, which gets rid of any possibly invalid non-scanned objects.

        // Step 5 - Enumerate all deployed refs and re-install any that are not in the repo (or are partial for a non-subdir deploy).
        
        return 0;
    }
}