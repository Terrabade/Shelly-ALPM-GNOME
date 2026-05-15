using MemoryPack;

namespace PackageManager.Local;

[MemoryPackable]
public partial record LocalPackageDto(
    string Name,
    long Size
);
