using PackageManager.Alpm;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Standard;

public class PurifyPackages : AsyncCommand<PurifyPackagesSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, PurifyPackagesSettings settings)
    {
        if (Program.IsUiMode) return await HandleUiMode(settings);

        RootElevator.EnsureRootExectuion();
        AnsiConsole.MarkupLine("[yellow] Initializing ALPM... [/]");
        using var manager = new AlpmManager();
        manager.Initialize(true);
        var results = await manager.PurifyPackages(settings.DryRun, settings.Orphans);
        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[green] No packages found to purify! [/]");
            return 0;
        }

        AnsiConsole.MarkupLine(settings.DryRun ? "[green] Running would remove: [/]" : "[green] Removed: [/]");

        var third = (int)Math.Ceiling(results.Count / 3.0);
        var columnOne = results.Take(third).ToList();
        var columnTwo = results.Skip(third).Take(third).ToList();
        var columnThree = results.Skip(third * 2).ToList();

        var table = new Table();
        if (columnOne.Count > 0) table.AddColumn("Package");
        if (columnTwo.Count > 0) table.AddColumn("Package");
        if (columnThree.Count > 0) table.AddColumn("Package");
        var length = Math.Max(columnOne.Count, Math.Max(columnTwo.Count, columnThree.Count));
        for (var i = 0; i < length; i++)
        {
            List<string> rows = [];
            if (i < columnOne.Count) rows.Add(columnOne[i]);
            if (i < columnTwo.Count) rows.Add(columnTwo[i]);
            if (i < columnThree.Count) rows.Add(columnThree[i]);
            table.AddRow(rows.ToArray());
        }

        AnsiConsole.Write(table);
        return 0;
    }

    private static async Task<int> HandleUiMode(PurifyPackagesSettings settings)
    {
        using var manager = new AlpmManager();
        manager.Initialize(true);
        var results = await manager.PurifyPackages(settings.DryRun, settings.Orphans);
        Console.WriteLine(string.Join(",", results));
        return 0;
    }
}