using System.Collections.Generic;
using MemoryPack;

namespace PackageManager.Alpm.Package;

[MemoryPackable]
public partial record AlpmPackageTreeDto(string Name)
{
    public List<AlpmPackageTreeDto> Files { get; } = [];
}