using System;
using System.Runtime.InteropServices;
using PackageManager.Flatpak.Enums;

namespace PackageManager.Flatpak.Models;

public class FlatpackPackage(IntPtr pkgPtr)
{
    public string Id => PtrToStringSafe(FlatpakReference.RefGetName(pkgPtr));
    public string Arch => PtrToStringSafe(FlatpakReference.RefGetArch(pkgPtr));
    public string Branch => PtrToStringSafe(FlatpakReference.RefGetBranch(pkgPtr));

    public string Name => PtrToStringSafe(FlatpakReference.InstalledRefGetAppDataName(pkgPtr)) is { Length: > 0 } name
        ? name
        : Id;

    public string Summary => PtrToStringSafe(FlatpakReference.InstalledRefGetAppDataSummary(pkgPtr));
    public string LastCommit => PtrToStringSafe(FlatpakReference.InstalledGetLatestCommit(pkgPtr));

    public string Version => PtrToStringSafe(FlatpakReference.InstalledRefGetAppDataVersion(pkgPtr)) is
        { Length: > 0 } ver
        ? ver
        : Branch;

    public string Origin => PtrToStringSafe(FlatpakReference.InstalledRefGetOrigin(pkgPtr));
    public uint InstalledSize => FlatpakReference.InstalledRefGetInstalledSize(pkgPtr);

    public int Kind => FlatpakReference.RefGetKind(pkgPtr);

    private static string PtrToStringSafe(IntPtr ptr)
    {
        return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }

    public FlatpakPackageDto ToDto(InstallLevel installLevel = InstallLevel.System) => new()
    {
        Id = Id,
        Branch = Branch,
        Name = Name,
        Arch = Arch,
        Summary = Summary,
        Version = Version,
        LatestCommit = LastCommit,
        Kind = Kind,
        Remote = Origin,
        InstallLevel = installLevel,
        InstalledSize = InstalledSize,
    };
}