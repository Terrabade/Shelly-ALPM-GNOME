using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PackageManager.AppImage.Events.EventArgs;
using Shelly.Utilities;

namespace PackageManager.AppImage.AppImageV2;

public class AppImageManagerV2
{
    private readonly string _installDirectory = XdgPaths.BinHome();

    private static readonly string LocalDbPath =
        XdgPaths.ShellyCache("appimage-local-meta-store", "appimage-metadata-v2.db");

    public event EventHandler<AppImageErrorEventArgs>? ErrorEvent;
    public event EventHandler<AppImageMessageEventArgs>? MessageEvent;

    private void LogMessage(string message)
    {
        MessageEvent?.Invoke(this, new AppImageMessageEventArgs(message));
    }

    private void LogError(string error)
    {
        ErrorEvent?.Invoke(this, new AppImageErrorEventArgs(error));
    }

    private void LogWarning(string message)
    {
        MessageEvent?.Invoke(this, new AppImageMessageEventArgs($"Warning: {message}"));
    }

    public async Task<int> InstallAppImage(string location)
    {
        var filePath = Path.GetFullPath(location);
        var appName = Path.GetFileNameWithoutExtension(filePath);
        var destAppImagePath = Path.Combine(_installDirectory, $"{appName}.AppImage");

        if (!Directory.Exists(_installDirectory))
            Directory.CreateDirectory(_installDirectory);

        LogMessage($"Installing AppImage {appName}...");
        File.Copy(filePath, destAppImagePath, true);
        SetFilePermissions(destAppImagePath, "a+x");

        var appImageDto = await ExtractMetadata(destAppImagePath);
        if (appImageDto == null)
        {
            LogError("Failed to extract metadata during installation.");
            return 1;
        }

        await AddAppImageToLocalDb(appImageDto);

        return 0;
    }

    public async Task<bool> AppImageConfigureUpdates(string updateInfo, string name, UpdateType updateType, bool allowPrerelease = false)
    {
        LogMessage($"Configuring updates for {name} {updateInfo}, type: {updateType}, allowPrerelease: {allowPrerelease}...");
        var appImages = await GetAppImagesFromLocalDb();
        var appImage = appImages.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
        if (appImage == null) return false;
        await ConfigureUpdates(updateInfo, updateType, ref appImage, allowPrerelease);
        var update = await CheckUpdate(appImage);
        if (update != null)
        {
            appImage.UpdateVersion = update.Version;
        }
        return await AddAppImageToLocalDb(appImage);
    }

    private Task ConfigureUpdates(string updateInfo, UpdateType updateType, ref AppImageDtoV2 appImage, bool allowPrerelease = false)
    {
        appImage.AllowPrerelease = allowPrerelease;
        switch (updateType)
        {
            case UpdateType.None:
                break;
            case UpdateType.StaticUrl:
                appImage.UpdateType = UpdateType.StaticUrl;
                appImage.UpdateURl = updateInfo;
                appImage.UpdateType = updateType;
                break;
            case UpdateType.GitHub:
            case UpdateType.GitLab:
            case UpdateType.Codeberg:
            case UpdateType.Forgejo:
                if (updateInfo.Count(c => c == '/') == 1)
                {
                    appImage.RepoOwner = updateInfo.Split('/')[0];
                    appImage.RepoName = updateInfo.Split('/')[1];
                    appImage.UpdateType = updateType;
                }
                else
                {
                    LogWarning(
                        "Could not parse update info. Please use the format: <user>/<repo> (e.g., github.com/user/repo or gitlab.com/user/repo");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(updateType), updateType, null);
        }

        return Task.CompletedTask;
    }

    public async Task<bool> AddAppImageToLocalDb(AppImageDtoV2 appImage)
    {
        try
        {
            var appImages = await GetAppImagesFromLocalDb();
            if (appImages.Any(a => string.Equals(a.Name, appImage.Name, StringComparison.OrdinalIgnoreCase)))
            {
                appImages.RemoveAll(a => string.Equals(a.Name, appImage.Name, StringComparison.OrdinalIgnoreCase));
            }

            appImages.Add(appImage);

            await EnsureDbDirectoryExists();
            var json = JsonSerializer.Serialize(appImages, AppImageJsonContextV2.Default.ListAppImageDtoV2);
            await File.WriteAllTextAsync(LocalDbPath, json);
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Error adding AppImage to local DB: {ex.Message}");
            return false;
        }
    }

    public async Task<List<AppImageDtoV2>> GetAppImagesFromLocalDb()
    {
        try
        {
            if (!File.Exists(LocalDbPath))
            {
                return [];
            }

            var json = await File.ReadAllTextAsync(LocalDbPath);
            return JsonSerializer.Deserialize(json, AppImageJsonContextV2.Default.ListAppImageDtoV2) ??
                   [];
        }
        catch (Exception ex)
        {
            LogError($"Error reading AppImage local DB: {ex.Message}");
            return [];
        }
    }

    private async Task<AppImageDtoV2?> ExtractMetadata(string filePath)
    {
        var appName = Path.GetFileNameWithoutExtension(filePath);
        var workingDir = Path.Combine(Path.GetTempPath(), "Shelly", $"sync-{appName}");
        var appImageVersion = "Unknown";
        var desktopName = "";
        var destIconName = "";
        var description = "";

        try
        {
            if (Directory.Exists(workingDir)) Directory.Delete(workingDir, true);
            Directory.CreateDirectory(workingDir);

            SetFilePermissions(filePath, "a+x");

            var squashfsRoot = Path.Combine(workingDir, "squashfs-root");

            try
            {
                var extractProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = "--appimage-extract",
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                await extractProcess!.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                LogWarning($"Could not execute AppImage directly: {ex.Message}.");
                return null;
            }

            var desktopFile = Directory.GetFiles(squashfsRoot, "*.desktop", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();
            string? iconName = null;
            if (desktopFile != null)
            {
                var lines = await File.ReadAllLinesAsync(desktopFile);
                var iconLine = lines.FirstOrDefault(l => l.StartsWith("Icon="));
                if (iconLine != null)
                {
                    iconName = iconLine.Split('=', 2)[1].Trim();
                }
            }

            string? iconPath = null;
            if (!string.IsNullOrEmpty(iconName))
            {
                iconPath = SafeGetFiles(squashfsRoot, $"{iconName}.*").FirstOrDefault();
            }

            if (iconPath == null)
            {
                iconPath = Path.Combine(squashfsRoot, ".DirIcon");
                if (!File.Exists(iconPath)) iconPath = null;
            }

            var finalIconPath = "application-x-executable";
            if (iconPath != null)
            {
                var extension = Path.GetExtension(iconPath).ToLower();
                if (string.IsNullOrEmpty(extension) || extension == ".diricon")
                {
                    extension = ".png";
                }

                var iconSubDir = extension == ".svg" ? "icons/hicolor/scalable/apps" : "icons/hicolor/256x256/apps";
                var systemIconDir = Path.Combine("/usr/share", iconSubDir);
                var userIconDir = Path.Combine(XdgPaths.DataHome(), iconSubDir);

                destIconName = $"{CleanInvalidNames(appName).ToLower()}{extension}";

                foreach (var iconDir in new[] { systemIconDir, userIconDir })
                {
                    try
                    {
                        Directory.CreateDirectory(iconDir);
                        var destIconPath = Path.Combine(iconDir, destIconName);
                        File.Copy(iconPath, destIconPath, true);
                        finalIconPath = CleanInvalidNames(appName).ToLower();
                        LogMessage($"Updated icon: {destIconPath}");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Could not copy icon to {iconDir}: {ex.Message}");
                    }
                }

                foreach (var themeDir in new[]
                             { "/usr/share/icons/hicolor", Path.Combine(XdgPaths.DataHome(), "icons/hicolor") })
                {
                    if (Directory.Exists(themeDir))
                        UpdateIconCache(themeDir);
                }
            }

            if (desktopFile != null)
            {
                try
                {
                    var desktopLines = await File.ReadAllLinesAsync(desktopFile);
                    var patchedContent = new StringBuilder();
                    foreach (var line in desktopLines)
                    {
                        if (line.StartsWith("Exec="))
                        {
                            // Preserve %u, %U, %f, %F and other field codes from the original Exec line
                            var execValue = line["Exec=".Length..].Trim();
                            var fieldCodes = "";
                            foreach (var token in execValue.Split(' ').Skip(1))
                            {
                                if (!token.StartsWith('%')) continue;
                                fieldCodes = $" {token}";
                                break;
                            }

                            patchedContent.AppendLine($"Exec=\"{filePath}\"{fieldCodes}");
                        }
                        else if (line.StartsWith("TryExec="))
                        {
                            //do nothing 
                        }
                        else if (line.StartsWith("Icon="))
                        {
                            patchedContent.AppendLine($"Icon={finalIconPath}");
                        }
                        else if (line.StartsWith("X-AppImage-Version="))
                        {
                            appImageVersion = line.Split('=')[1];
                            patchedContent.AppendLine(line);
                        }
                        else if (line.StartsWith("Name="))
                        {
                            if (string.IsNullOrEmpty(desktopName)) desktopName = line.Split('=')[1];
                            patchedContent.AppendLine(line);
                        }
                        else if (line.StartsWith("Comment="))
                        {
                            if (string.IsNullOrEmpty(description)) description = line.Split('=')[1];
                            patchedContent.AppendLine(line);
                        }
                        else
                        {
                            patchedContent.AppendLine(line);
                        }
                    }

                    var cleanName = CleanInvalidNames(appName);
                    var desktopFileName = $"{cleanName}.desktop";
                    var desktopContent = patchedContent.ToString();

                    foreach (var desktopDir in new[]
                                 { "/usr/share/applications", Path.Combine(XdgPaths.DataHome(), "applications") })
                    {
                        try
                        {
                            Directory.CreateDirectory(desktopDir);
                            var desktopFilePath = Path.Combine(desktopDir, desktopFileName);
                            await File.WriteAllTextAsync(desktopFilePath, desktopContent);
                            SetFilePermissions(desktopFilePath, "644");
                            UpdateDesktopDatabase(desktopDir);
                            LogMessage($"Updated desktop entry: {desktopFilePath}");
                        }
                        catch (Exception ex)
                        {
                            LogWarning($"Could not update desktop entry in {desktopDir}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"Could not update any desktop entry: {ex.Message}");
                    CreateDesktopEntry(appName, filePath, icon: finalIconPath);
                }
            }
            else
            {
                LogMessage($"No desktop file found in AppImage, creating default one.");
                CreateDesktopEntry(appName, filePath, icon: finalIconPath);
            }

            var updateInfo = await GetAppImageUpdateInfo(filePath);

            var appImageDto = new AppImageDtoV2
            {
                Name = appName,
                Version = appImageVersion,
                RawUpdateInfo = updateInfo,
                IconName = Path.GetFileNameWithoutExtension(destIconName),
                Description = description,
                DesktopName = string.IsNullOrEmpty(desktopName) ? appName : desktopName,
                SizeOnDisk = new FileInfo(filePath).Length,
            };

            return appImageDto;
        }
        catch (Exception ex)
        {
            LogError($"Error extracting metadata for {appName}: {ex.Message}");
            return null;
        }
        finally
        {
            try
            {
                if (Directory.Exists(workingDir)) Directory.Delete(workingDir, true);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private void SetFilePermissions(string filePath, string permissions)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"{permissions} \"{filePath}\"",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            LogWarning($"Could not set file permissions: {ex.Message}");
        }
    }

    private static Task EnsureDbDirectoryExists()
    {
        try
        {
            var directory = Path.GetDirectoryName(LocalDbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    private static List<string> SafeGetFiles(string rootDir, string searchPattern)
    {
        var results = new List<string>();
        try
        {
            results.AddRange(Directory.GetFiles(rootDir, searchPattern, SearchOption.TopDirectoryOnly));
        }
        catch
        {
            /* ignore access errors */
        }

        try
        {
            foreach (var dir in Directory.GetDirectories(rootDir))
            {
                var dirInfo = new DirectoryInfo(dir);
                if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;
                results.AddRange(SafeGetFiles(dir, searchPattern));
            }
        }
        catch
        {
            /* ignore access errors */
        }

        return results;
    }

    private static string CleanInvalidNames(string name)
    {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("\\", "-");
    }

    private void UpdateIconCache(string iconThemeDir)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "gtk-update-icon-cache",
                Arguments = $"-f -t \"{iconThemeDir}\"",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            LogWarning($"Could not update icon cache for {iconThemeDir}: {ex.Message}");
        }
    }

    private void UpdateDesktopDatabase(string desktopDir)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "update-desktop-database",
                Arguments = $"\"{desktopDir}\"",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            LogWarning($"Could not set desktop database: {ex.Message}");
        }
    }

    private void CreateDesktopEntry(
        string appName,
        string executablePath,
        string? comment = null,
        string icon = "application-x-executable",
        bool terminal = false,
        string categories = "Utility;")
    {
        var cleanName = CleanInvalidNames(appName);
        var desktopFileName = $"{cleanName}.desktop";

        var content = new StringBuilder();
        content.AppendLine("[Desktop Entry]");
        content.AppendLine("Version=1.0");
        content.AppendLine("Type=Application");
        content.AppendLine($"Name={appName}");
        content.AppendLine($"Comment={comment ?? $"{appName} application"}");
        content.AppendLine($"Exec=\"{executablePath}\"");
        content.AppendLine($"Icon={icon}");
        content.AppendLine($"Terminal={terminal.ToString().ToLower()}");
        content.AppendLine($"Categories={categories}");
        content.AppendLine("StartupNotify=true");

        foreach (var desktopDir in new[]
                     { "/usr/share/applications", Path.Combine(XdgPaths.DataHome(), "applications") })
        {
            try
            {
                Directory.CreateDirectory(desktopDir);
                var desktopFilePath = Path.Combine(desktopDir, desktopFileName);
                File.WriteAllText(desktopFilePath, content.ToString());
                SetFilePermissions(desktopFilePath, "644");
                UpdateDesktopDatabase(desktopDir);

                LogMessage($"Desktop entry created: {desktopFilePath}");
            }
            catch (Exception ex)
            {
                LogWarning($"Could not create desktop entry in {desktopDir}: {ex.Message}");
            }
        }
    }

    public async Task<string> GetAppImageUpdateInfo(string appImagePath)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = appImagePath,
                    Arguments = "--appimage-updateinfo",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                LogError($"Failed to get update info for {appImagePath}: {error}");
                return string.Empty;
            }

            return output.Trim();
        }
        catch (Exception ex)
        {
            LogError($"Could not get update info for {appImagePath}: {ex.Message}");
            return string.Empty;
        }
    }

    #region Github

    public async Task<List<AppImageUpdateDto>> CheckForAppImageUpdates()
    {
        var updates = new List<AppImageUpdateDto>();
        var installedAppImages = await GetAppImagesFromLocalDb();

        foreach (var appImage in installedAppImages)
        {
            var update = await CheckUpdate(appImage);
            if (update != null && update.IsUpdateAvailable)
            {
                updates.Add(update);
            }
        }

        return updates;
    }

    public async Task<AppImageUpdateDto?> CheckUpdate(AppImageDtoV2 appImage)
    {
        return appImage.UpdateType switch
        {
            UpdateType.GitHub => appImage is { RepoOwner: not null, RepoName: not null }
                ? await CheckGitHubUpdate(appImage.RepoOwner, appImage.RepoName, appImage.Name, appImage.Version,
                    appImage.AllowPrerelease)
                : null,
            UpdateType.GitLab => appImage is { RepoOwner: not null, RepoName: not null }
                ? await CheckGitLabUpdate(appImage.RepoOwner, appImage.RepoName, appImage.Name, appImage.Version,
                    appImage.AllowPrerelease)
                : null,
            UpdateType.Codeberg => appImage is { RepoOwner: not null, RepoName: not null }
                ? await CheckCodebergUpdate(appImage.RepoName, appImage.RepoOwner, appImage.Name, appImage.Version,
                    appImage.AllowPrerelease)
                : null,
            UpdateType.Forgejo => appImage.RepoOwner != null && appImage.RepoName != null
                ? await CheckForgejoUpdate(appImage.RepoName, appImage.Name, appImage.RepoOwner, appImage.Version,
                    appImage.AllowPrerelease)
                : null,
            UpdateType.StaticUrl => await CheckStaticUrlUpdate(appImage.UpdateURl, appImage.Name,
                appImage.Version),
            _ => null
        };
    }

    private static async Task<AppImageUpdateDto?> CheckStaticUrlUpdate(string url, string appName,
        string currentVersion)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(Http.UserAgent);
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var lastModified = response.Content.Headers.LastModified?.ToString() ?? "";
            var etag = response.Headers.ETag?.Tag ?? "";
            var version = !string.IsNullOrEmpty(etag) ? etag : lastModified;
            version = version.Replace("\"", "");

            return new AppImageUpdateDto
            {
                Name = appName,
                Version = version,
                DownloadUrl = url,
                IsUpdateAvailable = version != currentVersion
            };
        }
        catch
        {
            return null;
        }
    }

    private static string GetSystemArchitecture()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x86_64",
            Architecture.Arm64 => "aarch64",
            Architecture.X86 => "i386",
            Architecture.Arm => "arm",
            _ => "x86_64"
        };
    }

    private static bool IsCorrectArchitecture(string assetName)
    {
        var systemArch = GetSystemArchitecture();
        var lowerName = assetName.ToLowerInvariant();

     
        string[] x8664Aliases = ["x86_64", "amd64", "x64"];
        string[] aarch64Aliases = ["aarch64", "arm64", "armv8"];
        string[] i386Aliases = ["i386", "i686", "x86"];
        string[] armhfAliases = ["armhf", "armv7l", "arm"];

        var targetAliases = systemArch switch
        {
            "x86_64" => x8664Aliases,
            "aarch64" => aarch64Aliases,
            "i386" => i386Aliases,
            "arm" => armhfAliases,
            _ => [systemArch]
        };
        
        if (targetAliases.Any(lowerName.Contains))
        {
            return true;
        }
        
        var allAliases = x8664Aliases.Concat(aarch64Aliases).Concat(i386Aliases).Concat(armhfAliases);
        var otherArchitectures = allAliases.Where(a => !targetAliases.Contains(a));

        return !otherArchitectures.Any(lowerName.Contains);
    }

    private static string GithubToReleasesApi(string owner, string repo) =>
        $"https://api.github.com/repos/{owner}/{repo}/releases";

    private static async Task<AppImageUpdateDto?> CheckGitHubUpdate(string owner, string repo, string appName,
        string currentVersion, bool allowPrerelease = false)
    {
        try
        {
            var url = GithubToReleasesApi(owner, repo);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(Http.UserAgent);
            
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0) return null;

            var release = root.EnumerateArray()
                .FirstOrDefault(r => allowPrerelease || !r.GetProperty("prerelease").GetBoolean());

            if (release.ValueKind == JsonValueKind.Undefined) return null;

            var latestVersion = release.GetProperty("tag_name").GetString() ?? "";

            string? downloadUrl = null;
            if (!release.TryGetProperty("assets", out var assets))
                return new AppImageUpdateDto
                {
                    Name = appName,
                    Version = latestVersion,
                    DownloadUrl = downloadUrl ?? "",
                    IsUpdateAvailable = latestVersion != currentVersion
                };

            var appImageAssets = assets.EnumerateArray()
                .Where(asset => (asset.GetProperty("name").GetString() ?? "").EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (appImageAssets.Count == 1)
            {
                downloadUrl = appImageAssets[0].GetProperty("browser_download_url").GetString();
            }
            else if (appImageAssets.Count > 1)
            {
                downloadUrl = appImageAssets
                    .Where(asset => IsCorrectArchitecture(asset.GetProperty("name").GetString() ?? ""))
                    .Select(asset => asset.GetProperty("browser_download_url").GetString())
                    .FirstOrDefault();
                
                downloadUrl ??= appImageAssets[0].GetProperty("browser_download_url").GetString();
            }

            return new AppImageUpdateDto
            {
                Name = appName,
                Version = latestVersion,
                DownloadUrl = downloadUrl ?? "",
                IsUpdateAvailable = latestVersion != currentVersion
            };
        }
        catch
        {
            return null;
        }
    }

    #endregion

    public async Task<int> RunUpdate(AppImageUpdateDto update)
    {
        var appImages = await GetAppImagesFromLocalDb();
        var appImage = appImages.FirstOrDefault(a => string.Equals(a.Name, update.Name, StringComparison.OrdinalIgnoreCase));
        if (appImage == null)
        {
            LogError($"AppImage '{update.Name}' not found in local database.");
            return 1;
        }

        if (string.IsNullOrEmpty(update.DownloadUrl))
        {
            LogError($"No download URL found for {update.Name}.");
            return 1;
        }

        var currentPath = Path.Combine(_installDirectory, $"{appImage.Name}.AppImage");
        if (!File.Exists(currentPath))
        {
            LogError($"Current AppImage not found at {currentPath}.");
            return 1;
        }

        var backupDir = XdgPaths.ShellyCache(update.Name);
        var backupPath = Path.Combine(backupDir, $"{appImage.Name}-{appImage.Version}.AppImage.bak");
        var downloadPath = currentPath + ".rep";

        try
        {
            if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

            LogMessage($"Downloading update for {update.Name}...");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(Http.UserAgent);
                var response = await client.GetAsync(update.DownloadUrl);
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);
            }

            SetFilePermissions(downloadPath, "a+x");

            // Extract metadata from the downloaded file to verify it's a valid AppImage
            var newMetadata = await ExtractMetadata(downloadPath);
            if (newMetadata == null)
            {
                LogError("Failed to verify downloaded AppImage metadata.");
                if (File.Exists(downloadPath)) File.Delete(downloadPath);
                return 1;
            }

            // Architecture validation
            if (!IsCorrectArchitecture(Path.GetFileName(update.DownloadUrl) ?? ""))
            {
                LogWarning($"The downloaded AppImage might not match your system architecture. Proceeding with caution...");
            }

            LogMessage($"Backing up current version to {backupPath}...");
            File.Copy(currentPath, backupPath, true);

            try
            {
                LogMessage("Installing new version...");
                File.Move(downloadPath, currentPath, true);
            }
            catch (Exception ex)
            {
                LogError($"Error installing new version: {ex.Message}. Rolling back...");
                File.Copy(backupPath, currentPath, true);
                return 1;
            }

            // Update database with new metadata
            appImage.Version = update.Version;
            appImage.UpdateVersion = update.Version;
            appImage.Description = newMetadata.Description;
            appImage.IconName = newMetadata.IconName;
            
            await AddAppImageToLocalDb(appImage);

            return 0;
        }
        catch (Exception ex)
        {
            LogError($"Error during update: {ex.Message}");
            if (File.Exists(downloadPath)) File.Delete(downloadPath);
            if (File.Exists(backupPath) && !File.Exists(currentPath))
            {
                File.Copy(backupPath, currentPath);
            }

            return 1;
        }
    }

    #region GitLab

    private static string GitLabToReleasesApi(string owner, string repo, bool allowPrerelease)
    {
        var encodedPath = Uri.EscapeDataString($"{owner}/{repo}");
        return allowPrerelease 
            ? $"https://gitlab.com/api/v4/projects/{encodedPath}/releases"
            : $"https://gitlab.com/api/v4/projects/{encodedPath}/releases/permalink/latest";
    }

    private static async Task<AppImageUpdateDto?> CheckGitLabUpdate(string owner, string repo, string appName, string currentVersion, bool allowPrerelease = false)
    {
        try
        {
            var url = GitLabToReleasesApi(owner, repo, allowPrerelease);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(Http.UserAgent);
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            JsonElement release;
            if (allowPrerelease)
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0) return null;
                release = doc.RootElement.EnumerateArray().First();
            }
            else
            {
                release = doc.RootElement;
            }

            var latestVersion = release.GetProperty("tag_name").GetString() ?? "";

            string? downloadUrl = null;
            if (!release.TryGetProperty("assets", out var assets) || !assets.TryGetProperty("links", out var links))
            {
                return new AppImageUpdateDto
                {
                    Name = appName,
                    Version = latestVersion,
                    DownloadUrl = "",
                    IsUpdateAvailable = latestVersion != currentVersion
                };
            }

            var appImageLinks = links.EnumerateArray()
                .Where(link => (link.GetProperty("name").GetString() ?? "").EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (appImageLinks.Count == 1)
            {
                downloadUrl = appImageLinks[0].GetProperty("url").GetString();
            }
            else if (appImageLinks.Count > 1)
            {
                downloadUrl = appImageLinks
                    .Where(link => IsCorrectArchitecture(link.GetProperty("name").GetString() ?? ""))
                    .Select(link => link.GetProperty("url").GetString())
                    .FirstOrDefault();
                
                downloadUrl ??= appImageLinks[0].GetProperty("url").GetString();
            }

            return new AppImageUpdateDto
            {
                Name = appName,
                Version = latestVersion,
                DownloadUrl = downloadUrl ?? "",
                IsUpdateAvailable = latestVersion != currentVersion
            };
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Codeberg / Forgejo

    private static string GiteaToReleasesApi(string domain, string owner, string repo, bool allowPrerelease)
        => allowPrerelease 
            ? $"https://{domain}/api/v1/repos/{owner}/{repo}/releases"
            : $"https://{domain}/api/v1/repos/{owner}/{repo}/releases/latest";
    

    private static async Task<AppImageUpdateDto?> CheckGiteaUpdate(string owner, string repo, string appName, string currentVersion,
        string domain, bool allowPrerelease = false)
    {
        try
        {
            var url = GiteaToReleasesApi(domain, owner, repo, allowPrerelease);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(Http.UserAgent);
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            JsonElement release;
            if (allowPrerelease)
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0) return null;
                // Gitea returns releases sorted by created_at descending.
                release = doc.RootElement.EnumerateArray().First();
            }
            else
            {
                // If not allowPrerelease, we use the /latest endpoint.
                release = doc.RootElement;
            }

            var latestVersion = release.GetProperty("tag_name").GetString() ?? "";

            string? downloadUrl = null;
            if (release.TryGetProperty("assets", out var assets))
            {
                var appImageAssets = assets.EnumerateArray()
                    .Where(asset => (asset.GetProperty("name").GetString() ?? "").EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (appImageAssets.Count == 1)
                {
                    downloadUrl = appImageAssets[0].GetProperty("browser_download_url").GetString();
                }
                else if (appImageAssets.Count > 1)
                {
                    downloadUrl = appImageAssets
                        .Where(asset => IsCorrectArchitecture(asset.GetProperty("name").GetString() ?? ""))
                        .Select(asset => asset.GetProperty("browser_download_url").GetString())
                        .FirstOrDefault();
                    
                    downloadUrl ??= appImageAssets[0].GetProperty("browser_download_url").GetString();
                }
            }

            return new AppImageUpdateDto
            {
                Name = appName,
                Version = latestVersion,
                DownloadUrl = downloadUrl ?? "",
                IsUpdateAvailable = latestVersion != currentVersion
            };
        }
        catch
        {
            return null;
        }
    }

    private static async Task<AppImageUpdateDto?> CheckCodebergUpdate(string repo, string owner, string appName,
        string currentVersion, bool allowPrerelease = false)
    {
        return await CheckGiteaUpdate(owner, repo, appName, currentVersion, "codeberg.org", allowPrerelease);
    }

    private static async Task<AppImageUpdateDto?> CheckForgejoUpdate(string repo, string appName, string owner, string currentVersion, bool allowPrerelease = false)
    {
        var uri = new Uri(repo);
        return await CheckGiteaUpdate(owner, repo, appName, currentVersion, uri.Host, allowPrerelease);
    }

    #endregion
}