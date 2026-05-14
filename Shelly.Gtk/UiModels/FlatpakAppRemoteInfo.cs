using MemoryPack;

namespace Shelly.Gtk.UiModels;

[MemoryPackable]
public partial class FlatpakRemoteRefInfo
{
    public ulong DownloadSize { get; set; }
    public ulong InstalledSize { get; set; }
}