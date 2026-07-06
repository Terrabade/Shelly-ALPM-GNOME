using System;
using System.Runtime.InteropServices;
using PackageManager.Flatpak.Enums;

namespace PackageManager.Flatpak.Models;

/// <summary>
/// A specialized class for handling remote references returned by flatpak_installation_list_remote_refs_sync_full.
/// </summary>
public class FlatpakRemoteRef(IntPtr pkgPtr, InstallLevel installLevel)
{
    private string Id => PtrToStringSafe(FlatpakReference.RefGetName(pkgPtr));
    private string Arch => PtrToStringSafe(FlatpakReference.RefGetArch(pkgPtr));
    private string Branch => PtrToStringSafe(FlatpakReference.RefGetBranch(pkgPtr));
    private string RemoteName => PtrToStringSafe(FlatpakReference.RemoteRefGetRemoteName(pkgPtr));
    private int Kind => FlatpakReference.RefGetKind(pkgPtr);

    private static string PtrToStringSafe(IntPtr ptr)
    {
        return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }

    public FlatpakPackageDto ToDto() => new()
    {
        Id = Id,
        Name = Id, 
        Branch = Branch,
        Arch = Arch,
        Kind = Kind,
        Remote = RemoteName,
        Version = Branch,
        Summary = string.Empty,
        InstallLevel = installLevel
    };
}
