using System.Diagnostics.CodeAnalysis;
using PackageManager.Flatpak;
using PackageManager.Ostree;
using PackageManager.Ostree.Enums;
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

        List<OstreeRef> invalidRefs = [];
        List<OstreeRef> validRefs = [];
        
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

        foreach (var repo in repositories)
        {
            var refs = ostreeManager.ListRefs(repo);

            foreach (var reference in refs)
            {
                var commit = ostreeManager.GetCommitForRef(repo, reference.FullRef)!;

                reference.Commit = commit;
                
                if (string.IsNullOrWhiteSpace(commit))
                {
                    AnsiConsole.MarkupLine(
                        $"[red]Missing commit:[/] {reference.FullRef}");
                    
                    invalidRefs.Add(reference);
                    continue;
                }

                var fsck = ostreeManager.FsckCommit(repo, commit);

                if (fsck.Status == FsckStatus.Ok)
                {
                    AnsiConsole.MarkupLine(
                        $"[green]OK:[/] {reference.FullRef} -> {commit}");
                    
                    validRefs.Add(reference);
                }
                else
                {
                    AnsiConsole.MarkupLine(
                        $"[red]FSCK FAIL:[/] {reference.FullRef} ({fsck.Status})");
                    
                    invalidRefs.Add(reference);

                    if (fsck.MissingObjects.Count > 0)
                    {
                        AnsiConsole.MarkupLine(
                            $"  Missing: {string.Join(", ", fsck.MissingObjects)}");
                    }

                    if (fsck.InvalidObjects.Count > 0)
                    {
                        AnsiConsole.MarkupLine(
                            $"  Invalid: {string.Join(", ", fsck.InvalidObjects)}");
                    }

                    if (!string.IsNullOrWhiteSpace(fsck.ErrorMessage))
                    {
                        AnsiConsole.MarkupLine(
                            $"  Error: {fsck.ErrorMessage}");
                    }
                }
            }
        }        
        
        // Step 3 - Remove any refs that had an invalid object, and any non-partial refs that had missing objects.

        foreach (var reference in invalidRefs)
        {
            var removed = OstreeManager.DeleteRef(reference.RepoPath, reference.Remote, reference.Ref);
            
            if (removed)
            {
                AnsiConsole.MarkupLine(
                    $"[green]Removed:[/] {reference.FullRef}");
            }
            else
            {
                AnsiConsole.MarkupLine(
                    $"[red]Failed to remove:[/] {reference.FullRef}");
            }
            
        }

        // Step 4 - Prune all objects not referenced by a ref, which gets rid of any possibly invalid non-scanned objects.

        // Step 5 - Enumerate all deployed refs and re-install any that are not in the repo (or are partial for a non-subdir deploy).
        
        return 0;
    }
}