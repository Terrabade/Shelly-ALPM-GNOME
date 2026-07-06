using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using Shelly.Utilities.Networking;

namespace PackageManager.Aur;

public static class AurCachingUtility
{
    public static void CacheAurPackages()
    {
        var client = OptimizedClient.CreateClient(300, 5, 5);
        var result = client.GetStreamAsync("https://aur.archlinux.org/packages-meta-v1.json.gz").GetAwaiter()
            .GetResult();
        var gZipStream = new GZipStream(result, CompressionMode.Decompress);

        var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "shelly");
        if (!Directory.Exists(configPath))
        {
            Directory.CreateDirectory(configPath);
        }

        var filePath = Path.Combine(configPath, "aur-packages.json");
        using var fileStream = File.Create(filePath);
        gZipStream.CopyTo(fileStream);
    }
}