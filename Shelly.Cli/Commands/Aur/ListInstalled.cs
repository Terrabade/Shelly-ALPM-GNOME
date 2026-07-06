using System.CommandLine;
using System.Text.Json;
using PackageManager.Aur;
using Shelly.Cli.Interactions;

namespace Shelly.Cli.Commands.Aur;

public class ListInstalled : GlobalSettingsCommand
{
    private bool ShowHidden { get; set; }

    private bool ExplicitOnly { get; set; }

    private bool DependencyOnly { get; set; }

    public static Command Create()
    {
        var showHidden = new Option<bool>("--show-hidden")
            { Description = "Include hidden packages in the listing" };
        
        var explicitOnly = new Option<bool>("--explicitOnly", "-e")
            { Description = "Only show explicitly installed packages" };
        
        var dependencyOnly = new Option<bool>("--dependencyOnly", "-d")
            { Description = "Only show packages that are dependencies of other packages" };

        var command = new Command("list", "List installed AUR packages")
        {
            showHidden,
            explicitOnly,
            dependencyOnly,
        };

        command.SetAction(async (parseResult, _) =>
        {
            var instance = new ListInstalled
            {
                ShowHidden = parseResult.GetValue(showHidden),
                ExplicitOnly = parseResult.GetValue(explicitOnly),
                DependencyOnly = parseResult.GetValue(dependencyOnly),
            };
            GlobalOptions.Apply(instance, parseResult);
            await instance.ExecuteAsync(ShellyConsoleFactory.Create());
            return 0;
        });

        return command;
    }

    public override async ValueTask ExecuteAsync(IShellyConsole console)
    {
        using var manager = new AurPackageManager();
        await manager.Initialize(showHiddenPackages: ShowHidden);

        var packages = await manager.GetInstalledPackages();
        var sorted = packages.OrderBy(p => p.Name).ToList();

        if (UiMode)
        {
            JsonPackFrame.WriteToStdout(sorted);
            return;
        }

        if (JsonOutput)
        {
            console.WriteLine(JsonSerializer.Serialize(sorted, ShellyCliJsonContext.Default.ListAurPackageDto));
            return;
        }

        if (ExplicitOnly && !DependencyOnly)
        {
            sorted = sorted.Where(x => x.Explicit).ToList();
        }
        
        if(DependencyOnly && !ExplicitOnly)
        {
            sorted = sorted.Where(x => !x.Explicit).ToList();
        }

        console.WriteLine(BasicTable.Execute(
            ["Name", "Version", "Description"], sorted,
            p => p.Name,
            p => p.Version,
            p => Truncate(p.Description ?? "", 60)));
        console.WriteLine(AnsiUtilities.Colorize(
            $"Total: {sorted.Count} AUR packages installed", ConsoleColor.Blue));
    }

    public override ValueTask ExecuteUiMode() => ValueTask.CompletedTask;

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
