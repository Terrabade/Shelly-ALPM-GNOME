using MemoryPack;

namespace Shelly.Gtk.UiModels.PackageManagerObjects;

[MemoryPackable]
public partial record LocalPackageDto(
    string Name,
    long Size
);
