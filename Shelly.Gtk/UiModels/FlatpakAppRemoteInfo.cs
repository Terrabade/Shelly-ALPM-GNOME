
namespace Shelly.Gtk.UiModels;

public partial class FlatpakRemoteRefInfo
{
    public ulong DownloadSize { get; set; }
    public ulong InstalledSize { get; set; }
    public List<string> Permissions { get; set; } = [];
}