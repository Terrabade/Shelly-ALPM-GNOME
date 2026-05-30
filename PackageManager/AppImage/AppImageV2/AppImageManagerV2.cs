using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PackageManager.AppImage.AppImageV2;
using PackageManager.AppImage.Events.EventArgs;
using Shelly.Utilities;

namespace PackageManager.AppImage;

public class AppImageManagerV2
{
    private const string InstallDirectory = "/home/caro/shellyAppImages";

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

    public async Task<int> InstallAppImage(string location, UpdateType updateType = UpdateType.None,
        string updateInfo = "")
    {
        var filePath = Path.GetFullPath(location);
        var appName = Path.GetFileNameWithoutExtension(filePath);
        var destAppImagePath = Path.Combine(InstallDirectory, $"{appName}.AppImage");

        if (!Directory.Exists(InstallDirectory))
            Directory.CreateDirectory(InstallDirectory);

        LogMessage($"Installing AppImage {appName}...");
        File.Copy(filePath, destAppImagePath, true);
        SetFilePermissions(destAppImagePath, "a+x");

        var appImageDto = await ExtractMetadata(destAppImagePath);
        if (appImageDto == null)
        {
            LogError("Failed to extract metadata during installation.");
            return 1;
        }

        await ConfigureUpdates(updateInfo, updateType, ref appImageDto);

        await AddAppImageToLocalDb(appImageDto);

        return 0;
    }

    public async Task<bool> AppImageConfigureUpdates(string updateInfo, string name, UpdateType updateType)
    {
        LogMessage($"Configuring updates for {name} {updateInfo}, type: {updateType}...");
        var appImages = await GetAppImagesFromLocalDb();
        var appImage = appImages.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
        if (appImage == null) return false;
        await ConfigureUpdates(updateInfo, updateType, ref appImage);
        return await AddAppImageToLocalDb(appImage);
    }

    private Task ConfigureUpdates(string updateInfo, UpdateType updateType, ref AppImageDtoV2 appImage)
    {
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

    private async Task<bool> AddAppImageToLocalDb(AppImageDtoV2 appImage)
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
            var json = JsonSerializer.Serialize(appImages, AppImageJsonContext.Default.ListAppImageDto);
            await File.WriteAllTextAsync(LocalDbPath, json);
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Error adding AppImage to local DB: {ex.Message}");
            return false;
        }
    }

    private async Task<List<AppImageDtoV2>> GetAppImagesFromLocalDb()
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

    private static string GithubToReleasesApi(string owner, string repo) =>
        $"https://api.github.com/repos/{owner}/{repo}/releases";

    private static async Task<AppImageUpdateDto?> CheckGitHubUpdate(string owner, string repo, string appName,
        string currentVersion)
    {
        try
        {
            var url = GithubToReleasesApi(owner, repo);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(Http.UserAgent);
            var response = await client.GetAsync(url + "/latest");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var latestVersion = root.GetProperty("tag_name").GetString() ?? "";

            string? downloadUrl = null;
            if (!root.TryGetProperty("assets", out var assets))
                return new AppImageUpdateDto
                {
                    Name = appName,
                    Version = latestVersion,
                    DownloadUrl = downloadUrl ?? "",
                    IsUpdateAvailable = latestVersion != currentVersion
                };
            //TODO: Check Arch matches system arch and download correct one 
            downloadUrl = (from asset in assets.EnumerateArray()
                let assetName = asset.GetProperty("name").GetString() ?? ""
                where assetName.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase)
                select asset.GetProperty("browser_download_url").GetString()).FirstOrDefault();

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

    #region GitLab

    private static string GitLabToReleasesApi(string url)
    {
        var uri = new Uri(url);
        var path = uri.AbsolutePath.Trim('/');
        if (path.EndsWith("/-/releases")) path = path.Substring(0, path.Length - 11);

        var encodedPath = Uri.EscapeDataString(path);
        url = $"https://gitlab.com/api/v4/projects/{encodedPath}/releases/permalink/latest";


        return url;
    }

    private static async Task<AppImageUpdateDto?> CheckGitLabUpdate(string repo, string appName, string currentVersion)
    {
        try
        {
            var url = GitLabToReleasesApi(repo);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(Http.UserAgent);
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var latestVersion = root.GetProperty("tag_name").GetString() ?? "";

            string? downloadUrl = null;
            if (!root.TryGetProperty("assets", out var assets))
                return new AppImageUpdateDto
                {
                    Name = appName,
                    Version = latestVersion,
                    DownloadUrl = downloadUrl ?? "",
                    IsUpdateAvailable = latestVersion != currentVersion
                };
            if (!assets.TryGetProperty("links", out var links))
                return new AppImageUpdateDto
                {
                    Name = appName,
                    Version = latestVersion,
                    DownloadUrl = downloadUrl ?? "",
                    IsUpdateAvailable = latestVersion != currentVersion
                };
            downloadUrl = (from link in links.EnumerateArray()
                let linkName = link.GetProperty("name").GetString() ?? ""
                where linkName.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase)
                select link.GetProperty("url").GetString()).FirstOrDefault();

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

    private static string GiteaToReleasesApi(string url, string domain)
    {
        if (url.Contains(domain) && !url.Contains("/api/v1/repos/"))
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath.Trim('/');
            if (path.EndsWith("/releases")) path = path.Substring(0, path.Length - 9);

            url = $"https://{domain}/api/v1/repos/{path}/releases/latest";
        }

        return url;
    }

    private static async Task<AppImageUpdateDto?> CheckGiteaUpdate(string repo, string appName, string currentVersion,
        string domain)
    {
        try
        {
            var url = GiteaToReleasesApi(repo, domain);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(Http.UserAgent);
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var latestVersion = root.GetProperty("tag_name").GetString() ?? "";

            string? downloadUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var assetName = asset.GetProperty("name").GetString() ?? "";
                    if (assetName.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
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

    private static async Task<AppImageUpdateDto?> CheckCodebergUpdate(string repo, string appName,
        string currentVersion)
    {
        return await CheckGiteaUpdate(repo, appName, currentVersion, "codeberg.org");
    }

    private static async Task<AppImageUpdateDto?> CheckForgejoUpdate(string repo, string appName, string currentVersion)
    {
        var uri = new Uri(repo);
        return await CheckGiteaUpdate(repo, appName, currentVersion, uri.Host);
    }

    #endregion
}