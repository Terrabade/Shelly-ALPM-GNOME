using System;
using System.Runtime.InteropServices;

namespace PackageManager.Alpm.Native;

internal static partial class AlpmReference
{
    /// <summary>
    /// Opens the mtree file for a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to the archive (struct archive*), or IntPtr.Zero on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_mtree_open")]
    public static partial IntPtr PkgMtreeOpen(IntPtr pkg);

    /// <summary>
    /// Reads the next entry from the package's mtree.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <param name="entry">Pointer to the archive_entry (struct archive_entry**).</param>
    /// <returns>ARCHIVE_OK (0) on success, ARCHIVE_EOF (1) at end, or negative on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_mtree_next")]
    public static partial int PkgMtreeNext(IntPtr pkg, out IntPtr entry);

    /// <summary>
    /// Closes the mtree file for a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_mtree_close")]
    public static partial int PkgMtreeClose(IntPtr pkg);

}