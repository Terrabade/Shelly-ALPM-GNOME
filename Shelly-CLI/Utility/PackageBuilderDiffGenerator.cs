using Shelly.Utilities.Models;
using Spectre.Console;

namespace Shelly_CLI.Utility;

public static class PackageBuilderDiffGenerator
{
    public static void PrintUnifiedDiff(string oldText, string newText, bool isUiMode = false)
    {
        foreach (var line in PkgbuildDiff.BuildLines(oldText, newText))
        {
            switch (line.Kind)
            {
                case PkgbuildDiffKind.Context:
                    if (isUiMode)
                        Console.Error.WriteLine($"{line.Text.EscapeMarkup()}");
                    AnsiConsole.MarkupLine($"[white]  {line.Text.EscapeMarkup()}[/]");
                    break;
                case PkgbuildDiffKind.Added:
                    if (isUiMode)
                        Console.Error.WriteLine($"[Addition+] {line.Text.EscapeMarkup()}");
                    AnsiConsole.MarkupLine($"[green]+ {line.Text.EscapeMarkup()}[/]");
                    break;
                case PkgbuildDiffKind.Removed:
                    if (isUiMode)
                        Console.Error.WriteLine($"[Removal-] {line.Text.EscapeMarkup()}");
                    AnsiConsole.MarkupLine($"[red]- {line.Text.EscapeMarkup()}[/]");
                    break;
            }
        }
    }

    public static IEnumerable<string> BuildUnifiedDiffLines(string oldText, string newText)
    {
        var result = new List<string>();
        foreach (var line in PkgbuildDiff.BuildLines(oldText, newText))
        {
            result.Add(line.Kind switch
            {
                PkgbuildDiffKind.Added => $"[green]+ {line.Text.EscapeMarkup()}[/]",
                PkgbuildDiffKind.Removed => $"[red]- {line.Text.EscapeMarkup()}[/]",
                _ => $"[white]  {line.Text.EscapeMarkup()}[/]"
            });
        }

        return result;
    }
}
