using System.Collections.Generic;

namespace PackageManager.Flatpak;

public partial record FlatpakRemoteRefInfo
{
    public ulong DownloadSize { get; set; }
    public ulong InstalledSize { get; set; }
    public List<string> Permissions { get; set; } = [];
}