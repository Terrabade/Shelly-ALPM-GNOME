using System;
using System.Runtime.InteropServices;
using PackageManager.Alpm.Enums;

namespace PackageManager.Alpm.Native;

internal static partial class AlpmReference
{
    /// <summary>
    /// Loads a package from a file.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="filename">The package file path.</param>
    /// <param name="full">Whether to load the full package or just the header.</param>
    /// <param name="level">The signature verification level.</param>
    /// <param name="pkg">The loaded package pointer.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_load", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int PkgLoad(IntPtr handle, string filename, [MarshalAs(UnmanagedType.Bool)] bool full,
        AlpmSigLevel level, out IntPtr pkg);

    /// <summary>
    /// Finds a newer version of a package in sync databases.
    /// </summary>
    /// <param name="pkg">The package to check.</param>
    /// <param name="dbs_sync">The list of sync databases to search.</param>
    /// <returns>A pointer to the new version of the package, or IntPtr.Zero if no newer version is found.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_sync_get_new_version")]
    public static partial IntPtr SyncGetNewVersion(IntPtr pkg, IntPtr dbs_sync);

    /// <summary>
    /// Finds a package that satisfies a dependency string.
    /// </summary>
    /// <param name="pkgList"></param>
    /// <param name="depstring"></param>
    /// <returns>The first package that satisfies the dependency</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_find_satisfier", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr PkgFindSatisfier(IntPtr pkgList, string depstring);

    /// <summary>
    /// Finds a package in a list.
    /// </summary>
    /// <param name="haystack">The list of packages.</param>
    /// <param name="needle">The package name to find.</param>
    /// <returns>A pointer to the found package, or IntPtr.Zero.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_find", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr PkgFind(IntPtr haystack, string needle);

    /// <summary>
    /// Frees a package handle.
    /// </summary>
    /// <param name="pkg">The package to free.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_free")]
    public static partial int PkgFree(IntPtr pkg);

    /// <summary>
    /// Checks the MD5 sum of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_checkmd5sum")]
    public static partial int PkgCheckMd5Sum(IntPtr pkg);

    /// <summary>
    /// Compares two version strings.
    /// </summary>
    /// <param name="a">The first version string.</param>
    /// <param name="b">The second version string.</param>
    /// <returns>A value less than, equal to, or greater than 0.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_vercmp", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int PkgVerCmp(string a, string b);

    /// <summary>
    /// Computes the list of packages that require this package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to a list of package names.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_compute_requiredby")]
    public static partial IntPtr PkgComputeRequiredBy(IntPtr pkg);

    /// <summary>
    /// Computes the list of packages that this package is an optional dependency for.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to a list of package names.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_compute_optionalfor")]
    public static partial IntPtr PkgComputeOptionalFor(IntPtr pkg);

    /// <summary>
    /// Gets whether a package should be ignored.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="pkg">The package handle.</param>
    /// <returns>1 if it should be ignored, 0 otherwise.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_should_ignore")]
    public static partial int PkgShouldIgnore(IntPtr handle, IntPtr pkg);

    /// <summary>
    /// Gets the name of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The package name.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_name")]
    public static partial IntPtr GetPkgName(IntPtr pkg);

    /// <summary>
    /// Gets the filename of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The package filename.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_filename")]
    public static partial IntPtr GetPkgFileName(IntPtr pkg);

    /// <summary>
    /// Gets the version of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The package version.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_version")]
    public static partial IntPtr GetPkgVersion(IntPtr pkg);

    /// <summary>
    /// Gets the description of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The package description.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_desc")]
    public static partial IntPtr GetPkgDesc(IntPtr pkg);

    /// <summary>
    /// Gets the URL of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The package URL.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_url")]
    public static partial IntPtr GetPkgUrl(IntPtr pkg);

    /// <summary>
    /// Gets the build date of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The build date as a timestamp.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_builddate")]
    public static partial long GetPkgBuildDate(IntPtr pkg);

    /// <summary>
    /// Gets the installation date of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The installation date as a timestamp.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_installdate")]
    public static partial long GetPkgInstallDate(IntPtr pkg);

    /// <summary>
    /// Gets the packager of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The packager's name/email.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_packager")]
    public static partial IntPtr GetPkgPackager(IntPtr pkg);

    /// <summary>
    /// Gets the MD5 sum of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The MD5 sum string.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_md5sum")]
    public static partial IntPtr GetPkgMd5Sum(IntPtr pkg);

    /// <summary>
    /// Gets the SHA256 sum of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The SHA256 sum string.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_sha256sum")]
    public static partial IntPtr GetPkgSha256Sum(IntPtr pkg);

    /// <summary>
    /// Gets the architecture of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The architecture string.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_arch")]
    public static partial IntPtr GetPkgArch(IntPtr pkg);

    /// <summary>
    /// Gets the size of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The package size in bytes.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_size")]
    public static partial long GetPkgSize(IntPtr pkg);

    /// <summary>
    /// Gets the installed size of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The installed size in bytes.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_isize")]
    public static partial long GetPkgISize(IntPtr pkg);

    /// <summary>
    /// Gets the install reason of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The install reason.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_reason")]
    public static partial AlpmPkgReason GetPkgReason(IntPtr pkg);

    /// <summary>
    /// Gets the licenses of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to a list of license strings.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_licenses")]
    public static partial IntPtr GetPkgLicenses(IntPtr pkg);

    /// <summary>
    /// Gets the groups of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to a list of group strings.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_groups")]
    public static partial IntPtr GetPkgGroups(IntPtr pkg);

    /// <summary>
    /// Gets the dependencies of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to a list of dependency strings.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_depends")]
    public static partial IntPtr GetPkgDepends(IntPtr pkg);

    /// <summary>
    /// Gets the optional dependencies of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to a list of optional dependency strings.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_optdepends")]
    public static partial IntPtr GetPkgOptDepends(IntPtr pkg);

    /// <summary>
    /// Gets the check dependencies of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to a list of check dependency strings.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_checkdepends")]
    public static partial IntPtr GetPkgCheckDepends(IntPtr pkg);

    /// <summary>
    /// Gets the make dependencies of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to a list of make dependency strings.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_makedepends")]
    public static partial IntPtr GetPkgMakeDepends(IntPtr pkg);

    /// <summary>
    /// Gets the conflicts of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to a list of conflict strings.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_conflicts")]
    public static partial IntPtr GetPkgConflicts(IntPtr pkg);

    /// <summary>
    /// Gets the provides of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to a list of provide strings.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_provides")]
    public static partial IntPtr GetPkgProvides(IntPtr pkg);

    /// <summary>
    /// Gets the replaces of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to a list of replace strings.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_replaces")]
    public static partial IntPtr GetPkgReplaces(IntPtr pkg);

    /// <summary>
    /// Gets the files of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to the file list structure.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_files")]
    public static partial IntPtr GetPkgFiles(IntPtr pkg);

    /// <summary>
    /// Gets the backup files of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to a list of backup file strings.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_backup")]
    public static partial IntPtr GetPkgBackup(IntPtr pkg);

    /// <summary>
    /// Gets the database a package belongs to.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>A pointer to the database handle.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_db")]
    public static partial IntPtr GetPkgDb(IntPtr pkg);

    /// <summary>
    /// Gets the validation method of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The validation method bitmask.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_get_validation")]
    public static partial AlpmPkgValidation GetPkgValidation(IntPtr pkg);

    /// <summary>
    /// Gets whether a package has a scriptlet.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>1 if it has a scriptlet, 0 otherwise.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_has_scriptlet")]
    public static partial int PkgHasScriptlet(IntPtr pkg);

    /// <summary>
    /// Gets the download size of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <returns>The download size in bytes.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_download_size")]
    public static partial long PkgDownloadSize(IntPtr pkg);

    /// <summary>
    /// Sets the install reason of a package.
    /// </summary>
    /// <param name="pkg">The package handle.</param>
    /// <param name="reason">The install reason.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_pkg_set_reason")]
    public static partial int PkgSetReason(IntPtr pkg, AlpmPkgReason reason);
}