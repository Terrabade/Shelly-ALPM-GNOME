
using Shelly.Gtk.Enums;

namespace Shelly.Gtk.UiModels;

public partial class FlatpakRemoteDto
{
    public string Name { get; set; } = string.Empty;

    public InstallLevel Scope { get; set; }
    
    public string Url { get; set; } = string.Empty;
}