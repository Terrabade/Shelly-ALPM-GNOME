using System.Text.Json.Serialization;
using MemoryPack;

namespace Shelly.Gtk.UiModels;

[MemoryPackable]
public partial class SyncModel
{
    public SyncMetaData MetaData { get; set; } = new();
    public List<SyncPackageModel> Packages { get; set; } = [];
    public List<SyncAurModel> Aur { get; set; } = [];
    public List<SyncFlatpakModel> Flatpaks { get; set; } = [];

    public int TotalPackageCount => Packages.Count + Aur.Count + Flatpaks.Count;
}

[MemoryPackable]
public partial class SyncPackageModel
{
    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string? OldVersion { get; set; }

    public string? DownloadSize { get; set; }
}

[MemoryPackable]
public partial class SyncAurModel
{
    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string? OldVersion { get; set; }
}

[MemoryPackable]
public partial class SyncFlatpakModel
{
    public string Id { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string Version { get; set; } = string.Empty;
}

[MemoryPackable]
public partial class SyncMetaData
{
    public string Version { get; set; } = "v1";
    public string Date { get; set; } = string.Empty;
    public long Time { get; set; }
}