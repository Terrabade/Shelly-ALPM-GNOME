using System.Net;
using System.Text.RegularExpressions;
using PackageManager.Alpm;
using Shelly_CLI.Utility;
using Shelly.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Standard;

public partial class DowngradePackageCommand : AsyncCommand<DowngradePackageCommandSettings>
{
    private const string ArchRepo = "https://archive.archlinux.org/packages/";
    private const string PacmanCache = "/var/cache/pacman/pkg/";

    [GeneratedRegex("-x86.*")]
    private static partial Regex X86Regex();

    [GeneratedRegex("[a-zA-Z0-9.]+")]
    private static partial Regex VersionRegex();

    [GeneratedRegex("([0-9]+|[a-z0-9]{6,})")]
    private static partial Regex HashOrMinorRegex();

    public override async Task<int> ExecuteAsync(CommandContext context, DowngradePackageCommandSettings settings)
    {
        if (Program.IsUiMode) return HandleUiModeDowngrade(settings);

        if (settings.Packages.Length is 0 or > 1)
        {
            AnsiConsole.MarkupLine("[red]Error: No packages specified or more than one package specified.[/]");
            return 1;
        }


        var package = settings.Packages[0];
        AnsiConsole.MarkupLine($"[yellow]Looking for downgrade options for:[/] {package.EscapeMarkup()}");

        var packages = await SearchArchArchive(package);
        string selection;
        if (settings.NoConfirm)
        {
            selection = packages[0];
        }
        else
        {
            selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Select Version[/]")
                    .AddChoices(packages));
            AnsiConsole.WriteLine(selection);
        }

        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All,
            MaxAutomaticRedirections = 10,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        };
        using var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromMinutes(15);
        client.DefaultRequestHeaders.UserAgent.Add(Http.UserAgent);

        var fileName = $"{selection}";
        var url = $"{ArchRepo}{package[0]}/{package}/{fileName}";

        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        await AnsiConsole.Status()
            .StartAsync($"[yellow]Downloading {fileName.EscapeMarkup()}...[/]", async _ =>
            {
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);
            });

        AnsiConsole.MarkupLine($"[green]Downloaded to {filePath.EscapeMarkup()}[/]");

        if (!settings.NoConfirm && !AnsiConsole.Confirm("Do you want to proceed with the installation?"))
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
            return 0;
        }

        RootElevator.EnsureRootExectuion();

        using var manager = new AlpmManager();
        manager.Initialize(true);

        var renderLock = new object();
        manager.Question += (_, args) =>
        {
            lock (renderLock)
            {
                AnsiConsole.WriteLine();
                QuestionHandler.HandleQuestion(args, Program.IsUiMode, settings.NoConfirm);
            }
        };

        var currentPkgIndex = 0;
        var totalPkgs = settings.Packages.Length;
        string? lastPackageName = null;
        var lastPercent = 0;
        manager.Progress += (_, args) =>
        {
            lock (renderLock)
            {
                var name = args.PackageName ?? "unknown";
                var pct = args.Percent ?? 0;
                var bar = string.Join("", Enumerable.Repeat("🐚 ", pct * 2 / 5)) + new string('░', 20 - pct / 5);
                var actionType = args.ProgressType;

                // Detect package change
                if (name != lastPackageName)
                {
                    // If this isn't the first package, complete the previous line
                    if (lastPackageName != null)
                    {
                        Console.WriteLine(); // Move to new line
                        currentPkgIndex++;
                    }

                    lastPackageName = name;
                    lastPercent = 0;
                }

                // Update current line with carriage return
                Console.Write(
                    $"\r({currentPkgIndex + 1}/{totalPkgs}) installing {name,-40}  [{bar}] {pct,3}% - {actionType,-20}");

                lastPercent = pct;
            }
        };

        var hadError = false;
        manager.ErrorEvent += (_, e) =>
        {
            lock (renderLock)
            {
                AnsiConsole.MarkupLine($"[red]ERROR: {e.Error.EscapeMarkup()}[/]");
            }

            hadError = true;
        };

        AnsiConsole.MarkupLine("[yellow]Installing package...[/]");
        var result = await manager.InstallLocalPackage(filePath);

        if (File.Exists(filePath)) File.Delete(filePath);

        if (!result || hadError)
        {
            AnsiConsole.MarkupLine("[red]Downgrade failed. See errors above.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine("[green]Package downgraded successfully![/]");
        return 0;
    }

    private static async Task<List<string>> SearchArchArchive(string packageName)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All,
            MaxAutomaticRedirections = 10,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        };
        using var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromMinutes(15);
        client.DefaultRequestHeaders.UserAgent.Add(Http.UserAgent);
        using var result = await client.GetAsync($"{ArchRepo}{packageName[0]}/{packageName}/");
        var content = await result.Content.ReadAsStringAsync();

        var htmlRegex =
            new Regex(
                $"<a href=\"(?<filename>{Regex.Escape(packageName)}-[a-zA-Z0-9._+]+-[0-9]+-[a-zA-Z0-9_]+\\.pkg\\.tar\\.(?:zst|gz))\">",
                RegexOptions.Multiline);

        return htmlRegex.Matches(content)
            .Select(match => match.Groups["filename"].Value)
            .ToList();
    }

    private List<string> SearchLocalCache(string packageName)
    {
        return Directory.GetFiles(PacmanCache)
            .Select(filepath => Path.GetFileName(filepath))
            .Where(filename => Regex.IsMatch(filename,
                $@"^{Regex.Escape(packageName)}-{VersionRegex()}-{HashOrMinorRegex()}-.*\.pkg\.tar\..*"))
            .Select(pkgname => X86Regex().Replace(Path.GetFileName(pkgname), ""))
            .ToList();
    }

    private static int HandleUiModeDowngrade(DowngradePackageCommandSettings settings)
    {
        //Not implemented need to figure out how to handle ui
        return 1;
    }
}