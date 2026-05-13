using MemoryPack;

namespace Shelly.Gtk.UiModels.PackageManagerObjects;

[MemoryPackable]
public partial record AlpmPackageTreeDto(string Name)
{
    public List<AlpmPackageTreeDto> Files { get; init; } = [];
}
