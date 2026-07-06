using System;

namespace PackageManager.Flatpak.Enums;

[Flags]
public enum FlatpakQueryFlags : uint
{
    None = 0,
    OnlyCached = 1 << 0,
    OnlySideloaded = 1 << 1,
    AllArches = 1 << 2,
}
