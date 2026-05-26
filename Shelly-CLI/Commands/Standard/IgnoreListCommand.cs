using System.Text;
using System.Text.Json;
using PackageManager.Alpm;
using PackageManager.Wire;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Standard;

public class IgnoreListSettings : JsonSettings;

public class IgnoreListCommand : Command<IgnoreListSettings>
{
    public override int Execute(CommandContext context, IgnoreListSettings settings)
    {
        using var manager = new AlpmManager();
        var ignoredPackages = manager.GetIgnoredPackages();

        if (settings.JsonOutput)
        {
            if (Program.IsUiMode)
            {
                JsonPackFrame.WriteToStdout(ignoredPackages);
                return 0;
            }

            var json = JsonSerializer.Serialize(ignoredPackages, ShellyCLIJsonContext.Default.ListString);
            // Write directly to stdout stream to bypass Spectre.Console redirection
            using var stdout = Console.OpenStandardOutput();
            using var writer = new StreamWriter(stdout, Encoding.UTF8);
            writer.WriteLine(json);
            writer.Flush();
            return 0;
        }

        if (ignoredPackages.Count == 0)
        {
            if (Program.IsUiMode)
                Console.Error.WriteLine("IgnorePkg list is empty.");
            else
                AnsiConsole.MarkupLine("[yellow]IgnorePkg list is empty.[/]");

            return 0;
        }

        foreach (var package in ignoredPackages)
            if (Program.IsUiMode)
                Console.WriteLine(package);
            else
                AnsiConsole.WriteLine(package);

        return 0;
    }
}