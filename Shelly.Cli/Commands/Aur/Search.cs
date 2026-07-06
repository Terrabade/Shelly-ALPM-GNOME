using System.CommandLine;
using System.Text.Json;
using PackageManager.Alpm;
using PackageManager.Alpm.Package;
using PackageManager.Aur;
using PackageManager.Aur.Models;
using Shelly.Cli.Commands.Standard;
using Shelly.Cli.Interactions;
using Shelly.Utilities;
using static Shelly.Cli.Interactions.AnsiUtilities;

namespace Shelly.Cli.Commands.Aur;

public class Search : GlobalSettingsCommand
{
    private string[] Query { get; set; } = [];

    private bool SearchStandard { get; set; } = false;

    public static Command Create()
    {
        var query = new Argument<string[]>("query")
        {
            Description = "Search term to find packages in the Arch User Repository", Arity = ArgumentArity.OneOrMore
        };

        var searchStandard = new Option<bool>("--standard","-s")
        {
            Required = false,
            Description = "Searches standard packages in addition to the AUR"
        };

        var command = new Command("search", "Search the Arch User Repository")
        {
            searchStandard,
            query
        };

        command.SetAction(async (parseResult, _) =>
        {
            var instance = new Search
            {
                SearchStandard = parseResult.GetValue(searchStandard),
                Query = parseResult.GetValue(query) ?? []
            };
            GlobalOptions.Apply(instance, parseResult);
            await instance.ExecuteAsync(ShellyConsoleFactory.Create());
            return 0;
        });

        return command;
    }

    public override async ValueTask ExecuteAsync(IShellyConsole console)
    {
        var query = string.Join(" ", Query);

        if (string.IsNullOrWhiteSpace(query))
        {
            console.WriteLine(AnsiUtilities.Colorize("Query cannot be empty.", ConsoleColor.Red));
            return;
        }

        if (query.Length < 2)
        {
            console.WriteLine(AnsiUtilities.Colorize("Error: Query must be at least 2 characters long",
                ConsoleColor.Red));
            return;
        }

        List<AlpmPackageDto> alpmPackageDtos = [];
        if (SearchStandard)
        {
            var alpm = new AlpmManager();
            alpm.Initialize();
            alpmPackageDtos = alpm.GetAvailablePackages();
            alpmPackageDtos = alpmPackageDtos
                .Select(x => new { Package = x, Score = StringMatcher.PartialRatio(query, x.Name) })
                .Where(x => x.Score >= 90)
                .Select(x => x.Package)
                .ToList();
            alpm.Dispose();
            alpmPackageDtos.Reverse();
        }

        using var manager = new AurPackageManager();
        await manager.Initialize();

        var results = await manager.SearchPackages(query);
        

        if (UiMode)
        {
            JsonPackFrame.WriteToStdout(results);
            return;
        }

        if (JsonOutput)
        {
            console.WriteLine(JsonSerializer.Serialize(results, ShellyCliJsonContext.Default.ListAurPackageDto));
            if (SearchStandard)
            {
                console.WriteLine(JsonSerializer.Serialize(alpmPackageDtos,
                    ShellyCliJsonContext.Default.ListAlpmPackageDto));
            }

            return;
        }
        foreach (var alpmPackageDto in alpmPackageDtos)
        {
            results.Add(new AurPackageDto()
            {
                Name =  alpmPackageDto.Name,
                Version = alpmPackageDto.Version,
                Maintainer = alpmPackageDto.Repository,
                LastModified = new DateTimeOffset(alpmPackageDto.BuildDate).ToUnixTimeSeconds(),
                Description = alpmPackageDto.Description,
            });
        }
        console.WriteLine(BasicTable.Execute(
            ["Name", "Version", "Maintainer/Repository", "Last Updated/Build Date", "Description"], results.Take(25).ToList(),
            p => p.Name,
            p => p.Version,
            p => p.Maintainer ?? "Unknown Maintainer",
            p => DateTimeOffset.FromUnixTimeSeconds(p.LastModified).ToString("yyyy-MM-dd HH:mm:ss"),
            p => Truncate(p.Description ?? "", 60)));
        console.WriteLine(AnsiUtilities.Colorize($"Total results: {results.Count}", ConsoleColor.Blue));
    }

    public override ValueTask ExecuteUiMode() => ValueTask.CompletedTask;

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}