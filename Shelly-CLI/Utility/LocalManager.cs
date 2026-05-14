using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Shelly_CLI.Commands.Standard;
using Spectre.Console;
using ZstdSharp;

namespace Shelly_CLI.Utility;

// TODO: Move to PackageManager
public static partial class LocalManager
{
    public const string InstallDir = "/opt/shelly";
    private const string DesktopDir = "/usr/share/applications";

    [GeneratedRegex(@"(\d+)x?\d*")]
    private static partial Regex ImageSizeRegex();

    public static async Task<int> InstallBinariesPackage(string filePath)
    {
        var extension = Path.GetExtension(filePath);

        var packageName = Path.GetFileName(filePath)
            .Replace(".pkg.tar" + extension, "")
            .Replace(".tar" + extension, "");
        var installDir = Path.Combine(InstallDir, packageName);
        Directory.CreateDirectory(installDir);

        var installedBinaries = new List<string>();
        var foundIcons = new SortedDictionary<string, string>();

        await using var fileStream = File.OpenRead(filePath);
        await using Stream decompressedStream = extension switch
        {
            ".gz" => new GZipStream(fileStream, CompressionMode.Decompress),
            ".zst" => new ZstdStream(fileStream, ZstdStreamMode.Decompress),
            _ => throw new NotSupportedException($"Unsupported compression: {extension}")
        };

        await using (var tarReader = new TarReader(decompressedStream))
        {
            while (await tarReader.GetNextEntryAsync() is { } entry)
            {
                var destPath = Path.Combine(installDir, entry.Name);

                switch (entry.EntryType)
                {
                    case TarEntryType.Directory:
                    {
                        Directory.CreateDirectory(destPath);
                        break;
                    }
                    case TarEntryType.RegularFile:
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                        await entry.ExtractToFileAsync(destPath, true);

                        var ext = Path.GetExtension(destPath).ToLower();
                        if (IsIcon(ext))
                        {
                            var iconFileName = Path.GetFileNameWithoutExtension(destPath).ToLower();
                            foundIcons[iconFileName] = destPath;
                        }

                        // Check if it's an ELF binary and create symlink in /usr/bin
                        if (entry.DataStream is not null)
                        {
                            await using var fs = File.OpenRead(destPath);
                            if (string.IsNullOrWhiteSpace(Path.GetExtension(destPath)) && await IsElfBinary(fs))
                            {
                                var binaryName = Path.GetFileName(destPath);
                                var linkPath = Path.Combine("/usr/bin", binaryName);
                                if (File.Exists(linkPath)) File.Delete(linkPath);
                                File.CreateSymbolicLink(linkPath, destPath);

                                installedBinaries.Add(binaryName);
                            }
                        }

                        break;
                    }
                    case TarEntryType.SymbolicLink:
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                        if (File.Exists(destPath)) File.Delete(destPath);
                        File.CreateSymbolicLink(destPath, entry.LinkName);
                        break;
                    }
                }
            }
        }

        AnsiConsole.MarkupLine($"[green]Extracted to {installDir.EscapeMarkup()}[/]");

        foreach (var binaryName in installedBinaries)
        {
            var iconName = "application-x-executable";

            if (!CleanInvalidNames(packageName)
                    .Contains(binaryName, StringComparison.OrdinalIgnoreCase)) continue;

            if (foundIcons.Count > 0)
            {
                AnsiConsole.MarkupLine(
                    $"[cyan]Found icon for {binaryName.EscapeMarkup()}: {foundIcons.FirstOrDefault().Key.EscapeMarkup()}[/]");
                var installedIconName = InstallIcon(foundIcons.FirstOrDefault().Value, binaryName);
                if (installedIconName != null) iconName = installedIconName;
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]No icon found for {binaryName.EscapeMarkup()}, using default[/]");
            }

            Console.WriteLine("Creating desktop entry...");
            CreateDesktopEntry(
                binaryName,
                binaryName,
                $"{binaryName} - Installed from {packageName}",
                iconName,
                false,
                "Utility;"
            );
            AnsiConsole.MarkupLine("[green]Desktop Entries Created[/]");
        }

        return 0;
    }

    public static List<LocalPackageDto> GetInstalledBinaryPackages()
    {
        var dirs = ListDirectories(InstallDir);
        return dirs
            .Select(dir =>
            {
                var dirInfo = new DirectoryInfo(dir);
                var size = dirInfo
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(f => f.Length);

                return new LocalPackageDto(dir, size);
            })
            .ToList();
    }

    private static List<string> ListDirectories(string path)
    {
        if (!Directory.Exists(path)) return [];
        return Directory.GetDirectories(path)
            .Select(Path.GetFullPath)
            .ToList();
    }

    public static async Task<bool> IsArchPackage(string filePath)
    {
        await using var fileStream = File.OpenRead(filePath);
        switch (Path.GetExtension(filePath))
        {
            case ".zst":
            {
                await using var zStdStream = new ZstdStream(fileStream, ZstdStreamMode.Decompress);
                await using var zstTarReader = new TarReader(zStdStream);
                while (await zstTarReader.GetNextEntryAsync() is { } entry)
                {
                    if (entry.Name.Contains("PKGINFO", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }

                break;
            }
            case ".gz":
            {
                await using var gzStream = new GZipStream(fileStream, CompressionMode.Decompress);
                await using var gzTarReader = new TarReader(gzStream);
                while (await gzTarReader.GetNextEntryAsync() is { } entry)
                {
                    if (entry.Name.Contains("PKGINFO", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }

                break;
            }
        }

        return false;
    }

    public static async Task<bool> IsBinariesPackage(string filePath)
    {
        await using var fileStream = File.OpenRead(filePath);
        await using Stream decompressedStream = Path.GetExtension(filePath) switch
        {
            ".gz" => new GZipStream(fileStream, CompressionMode.Decompress),
            ".zst" => new ZstdStream(fileStream, ZstdStreamMode.Decompress),
            _ => throw new NotSupportedException("Unsupported file extension")
        };
        await using var tarReader = new TarReader(decompressedStream);
        while (await tarReader.GetNextEntryAsync() is { } entry)
        {
            if (entry.EntryType != TarEntryType.RegularFile || entry.DataStream is null) continue;
            if (await IsElfBinary(entry.DataStream)) return true;
        }

        return false;
    }

    public static async Task<bool> RemoveBinaryPackages(List<string> packageList)
    {
        var dirs = packageList
            .Select(path => new DirectoryInfo(path))
            .Where(dir => dir.FullName.StartsWith(InstallDir + '/') && dir.Exists);

        foreach (var dir in dirs)
        {
            var pkgInfos = dir
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .ToList();

            List<FileInfo> pkgBins = [];
            foreach (var info in pkgInfos)
            {
                await using var fs = File.OpenRead(info.FullName);
                if (await IsElfBinary(fs)) pkgBins.Add(info);
            }

            List<string> desktopBins = [];

            foreach (var pkgBin in pkgBins)
            {
                var usrBin = new FileInfo(Path.Combine("/usr/bin", pkgBin.Name));
                var canDelete = pkgBin.FullName.Equals(usrBin.LinkTarget);
                if (!canDelete) continue;

                Console.WriteLine($"Removing {pkgBin.Name} from {usrBin.FullName}");
                File.Delete(usrBin.FullName);

                if (!CleanInvalidNames(dir.Name)
                        .Contains(pkgBin.Name, StringComparison.InvariantCultureIgnoreCase)) continue;

                var desktopFilePath =
                    Path.Combine(DesktopDir, $"{Path.GetFileNameWithoutExtension(pkgBin.Name)}.desktop");
                Console.WriteLine($"Removing {desktopFilePath}");
                File.Delete(desktopFilePath);
                desktopBins.Add(pkgBin.Name);
            }

            var iconInfos = pkgInfos
                .Where(info => IsIcon(info.Extension.ToLower()))
                .OrderBy(info => info.Name)
                .ToList();

            foreach (var desktopBin in desktopBins)
            {
                foreach (var icon in iconInfos)
                {
                    var extension = icon.Extension.ToLower();
                    string destDir;
                    if (extension == ".svg")
                    {
                        destDir = "/usr/share/icons/hicolor/scalable/apps";
                    }
                    else
                    {
                        var sizeMatch = ImageSizeRegex().Match(icon.Name);
                        var size = sizeMatch.Success && int.TryParse(sizeMatch.Groups[1].Value, out var s)
                            ? s
                            : 256;
                        destDir = $"/usr/share/icons/hicolor/{size}x{size}/apps";
                    }

                    var destPath = Path.Combine(destDir, $"{desktopBin}{extension}");
                    if (!File.Exists(destPath)) continue;

                    Console.WriteLine($"Removing icon {destPath}");
                    File.Delete(destPath);
                }
            }

            // Delete package directory
            Console.WriteLine($"Removing package directory {dir.FullName}");
            dir.Delete(true);
        }

        return true;
    }

    private static bool IsIcon(string i)
    {
        return i is ".png" or ".svg";
    }

    private static async Task<bool> IsElfBinary(Stream stream)
    {
        var magic = new byte[4];
        var bytesRead = await stream.ReadAsync(magic);

        return bytesRead >= 4 &&
               magic[0] == 0x7F && magic[1] == 0x45 &&
               magic[2] == 0x4C && magic[3] == 0x46;
    }

    private static void CreateDesktopEntry(
        string appName,
        string executablePath,
        string? comment = null,
        string icon = "application-x-executable",
        bool terminal = false,
        string categories = "Utility;")
    {
        var cleanName = CleanInvalidNames(appName);
        var desktopFilePath = Path.Combine(DesktopDir, $"{cleanName}.desktop");

        var content = new StringBuilder();
        content.AppendLine("[Desktop Entry]");
        content.AppendLine("Version=1.0");
        content.AppendLine("Type=Application");
        content.AppendLine($"Name={appName}");
        content.AppendLine($"Comment={comment ?? $"{appName} application"}");
        content.AppendLine($"Exec={executablePath}");
        content.AppendLine($"Icon={icon}");
        content.AppendLine($"Terminal={terminal.ToString().ToLower()}");
        content.AppendLine($"Categories={categories}");
        content.AppendLine("StartupNotify=true");

        try
        {
            Directory.CreateDirectory(DesktopDir);
            File.WriteAllText(desktopFilePath, content.ToString());
            SetFilePermissions(desktopFilePath, "644");
            UpdateDesktopDatabase(DesktopDir);

            AnsiConsole.MarkupLine($"[green]Desktop entry created: {desktopFilePath.EscapeMarkup()}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not create desktop entry: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static string CleanInvalidNames(string name)
    {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("\\", "-");
    }

    private static void SetFilePermissions(string filePath, string permissions)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"{permissions} \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not set file permissions: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static void UpdateDesktopDatabase(string desktopDir)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "update-desktop-database",
                Arguments = $"\"{desktopDir}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not set desktop database: {ex.Message.EscapeMarkup()}[/]");
        }
    }

    private static string? InstallIcon(string iconPath, string appName)
    {
        try
        {
            var extension = Path.GetExtension(iconPath);
            var iconName = $"{appName.ToLower()}{extension}";
            string destDir;
            if (extension == ".svg")
            {
                destDir = "/usr/share/icons/hicolor/scalable/apps";
            }
            else
            {
                var sizeMatch = ImageSizeRegex().Match(Path.GetFileName(iconPath));
                var size = sizeMatch.Success && int.TryParse(sizeMatch.Groups[1].Value, out var s)
                    ? s
                    : 256;
                destDir = $"/usr/share/icons/hicolor/{size}x{size}/apps";
            }

            Directory.CreateDirectory(destDir);
            var destPath = Path.Combine(destDir, iconName);

            File.Copy(iconPath, destPath, true);

            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "gtk-update-icon-cache",
                    Arguments = "-f -t /usr/share/icons/hicolor",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Failed to update icon cache: {ex.Message.EscapeMarkup()}[/]");
            }

            return appName.ToLower();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not install icon: {ex.Message.EscapeMarkup()}[/]");
            return null;
        }
    }
}
