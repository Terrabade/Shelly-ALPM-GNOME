using System;
using System.Runtime.InteropServices;
using PackageManager.Alpm.Enums;

namespace PackageManager.Alpm.Native;

internal static partial class AlpmReference
{
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_eventcb")]
    public static partial int SetEventCallback(IntPtr handle, AlpmEventCallback cb, IntPtr ctx);

    [LibraryImport(LibName, EntryPoint = "alpm_option_set_fetchcb")]
    public static partial int SetFetchCallback(IntPtr handle, AlpmFetchCallback cb, IntPtr ctx);

    [LibraryImport(LibName, EntryPoint = "alpm_option_set_questioncb")]
    public static partial int SetQuestionCallback(IntPtr handle, AlpmQuestionCallback cb, IntPtr ctx);

    [LibraryImport(LibName, EntryPoint = "alpm_option_set_progresscb")]
    public static partial int SetProgressCallback(IntPtr handle, AlpmProgressCallback cb, IntPtr ctx);

    [LibraryImport(LibName, EntryPoint = "alpm_option_get_lockfile")]
    public static partial IntPtr GetLockFile(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "alpm_option_get_cachedirs")]
    public static partial IntPtr GetCacheDirs(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "alpm_option_set_cachedirs")]
    public static partial int SetCacheDirs(IntPtr handle, IntPtr cachedirs);

    [LibraryImport(LibName, EntryPoint = "alpm_option_add_cachedir", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int AddCacheDir(IntPtr handle, string cachedir);

    [LibraryImport(LibName, EntryPoint = "alpm_option_remove_cachedir", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int RemoveCacheDir(IntPtr handle, string cachedir);


    /// <summary>
    /// Gets the list of hook directories.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>A pointer to a list of hook directories.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_hookdirs")]
    public static partial IntPtr GetHookDirs(IntPtr handle);

    /// <summary>
    /// Sets the list of hook directories.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="hookdirs">A pointer to a list of hook directories.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_hookdirs")]
    public static partial int SetHookDirs(IntPtr handle, IntPtr hookdirs);

    /// <summary>
    /// Adds a hook directory to the list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="hookdir">The hook directory to add.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_add_hookdir", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int AddHookDir(IntPtr handle, string hookdir);

    /// <summary>
    /// Removes a hook directory from the list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="hookdir">The hook directory to remove.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_remove_hookdir", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int RemoveHookDir(IntPtr handle, string hookdir);

    /// <summary>
    /// Gets the list of files to overwrite during installation.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>A pointer to a list of file globs.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_overwrite_files")]
    public static partial IntPtr GetOverwriteFiles(IntPtr handle);

    /// <summary>
    /// Sets the list of files to overwrite during installation.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="globs">A pointer to a list of file globs.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_overwrite_files")]
    public static partial int SetOverwriteFiles(IntPtr handle, IntPtr globs);

    /// <summary>
    /// Adds a file glob to the overwrite list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="glob">The file glob to add.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_add_overwrite_file",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial int AddOverwriteFile(IntPtr handle, string glob);

    /// <summary>
    /// Removes a file glob from the overwrite list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="glob">The file glob to remove.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_remove_overwrite_file",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial int RemoveOverwriteFile(IntPtr handle, string glob);

    /// <summary>
    /// Sets the log file path.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="logfile">The log file path.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_logfile", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int SetLogFile(IntPtr handle, string logfile);

    /// <summary>
    /// Sets the GPG directory path.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="gpgdir">The GPG directory path.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_gpgdir", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int SetGpgDir(IntPtr handle, string gpgdir);

    /// <summary>
    /// Gets whether to use syslog for logging.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>1 if syslog is used, 0 otherwise.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_usesyslog")]
    public static partial int GetUseSyslog(IntPtr handle);

    /// <summary>
    /// Sets whether to use syslog for logging.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="usesyslog">1 to use syslog, 0 otherwise.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_usesyslog")]
    public static partial int SetUseSyslog(IntPtr handle, int usesyslog);

    /// <summary>
    /// Gets the list of packages that should not be upgraded.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>A pointer to a list of package names.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_noupgrades")]
    public static partial IntPtr GetNoUpgrades(IntPtr handle);

    /// <summary>
    /// Adds a package to the no-upgrade list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="path">The package name or path.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_add_noupgrade", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int AddNoUpgrade(IntPtr handle, string path);

    /// <summary>
    /// Sets the list of packages that should not be upgraded.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="noupgrade">A pointer to a list of package names.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_noupgrades")]
    public static partial int SetNoUpgrades(IntPtr handle, IntPtr noupgrade);

    /// <summary>
    /// Removes a package from the no-upgrade list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="path">The package name or path.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_remove_noupgrade",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial int RemoveNoUpgrade(IntPtr handle, string path);

    /// <summary>
    /// Gets the list of files that should not be extracted.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>A pointer to a list of file paths.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_noextracts")]
    public static partial IntPtr GetNoExtracts(IntPtr handle);

    /// <summary>
    /// Adds a file to the no-extract list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="path">The file path.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_add_noextract", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int AddNoExtract(IntPtr handle, string path);

    /// <summary>
    /// Sets the list of files that should not be extracted.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="noextract">A pointer to a list of file paths.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_noextracts")]
    public static partial int SetNoExtracts(IntPtr handle, IntPtr noextract);

    /// <summary>
    /// Removes a file from the no-extract list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="path">The file path.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_remove_noextract",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial int RemoveNoExtract(IntPtr handle, string path);

    /// <summary>
    /// Gets the list of ignored packages.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>A pointer to a list of package names.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_ignorepkgs")]
    public static partial IntPtr GetIgnorePkgs(IntPtr handle);

    /// <summary>
    /// Adds a package to the ignore list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="pkg">The package name.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_add_ignorepkg", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int AddIgnorePkg(IntPtr handle, string pkg);

    /// <summary>
    /// Sets the list of ignored packages.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="ignorepkgs">A pointer to a list of package names.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_ignorepkgs")]
    public static partial int SetIgnorePkgs(IntPtr handle, IntPtr ignorepkgs);

    /// <summary>
    /// Removes a package from the ignore list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="pkg">The package name.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_remove_ignorepkg",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial int RemoveIgnorePkg(IntPtr handle, string pkg);

    /// <summary>
    /// Gets the list of ignored groups.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>A pointer to a list of group names.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_ignoregroups")]
    public static partial IntPtr GetIgnoreGroups(IntPtr handle);

    /// <summary>
    /// Adds a group to the ignore list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="grp">The group name.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_add_ignoregroup", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int AddIgnoreGroup(IntPtr handle, string grp);

    /// <summary>
    /// Sets the list of ignored groups.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="ignoregrps">A pointer to a list of group names.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_ignoregroups")]
    public static partial int SetIgnoreGroups(IntPtr handle, IntPtr ignoregrps);

    /// <summary>
    /// Removes a group from the ignore list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="grp">The group name.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_remove_ignoregroup",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial int RemoveIgnoreGroup(IntPtr handle, string grp);

    /// <summary>
    /// Gets the list of allowed architectures.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>A pointer to a list of architecture names.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_architectures")]
    public static partial IntPtr GetArchitectures(IntPtr handle);

    /// <summary>
    /// Adds an architecture to the allowed list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="arch">The architecture name.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_add_architecture",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial int AddArchitecture(IntPtr handle, string arch);

    /// <summary>
    /// Sets the list of allowed architectures.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="arches">A pointer to a list of architecture names.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_architectures")]
    public static partial int SetArchitectures(IntPtr handle, IntPtr arches);

    /// <summary>
    /// Removes an architecture from the allowed list.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="arch">The architecture name.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_remove_architecture",
        StringMarshalling = StringMarshalling.Utf8)]
    public static partial int RemoveArchitecture(IntPtr handle, string arch);

    /// <summary>
    /// Gets whether to check for free disk space before transactions.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>1 if space is checked, 0 otherwise.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_checkspace")]
    public static partial int GetCheckSpace(IntPtr handle);

    /// <summary>
    /// Sets whether to check for free disk space before transactions.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="checkspace">1 to check space, 0 otherwise.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_checkspace")]
    public static partial int SetCheckSpace(IntPtr handle, int checkspace);

    /// <summary>
    /// Sets the package database extension.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="dbext">The database extension.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_dbext", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int SetDbExt(IntPtr handle, string dbext);

    /// <summary>
    /// Gets the default signature verification level.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>The signature level.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_default_siglevel")]
    public static partial AlpmSigLevel GetDefaultSigLevel(IntPtr handle);

    /// <summary>
    /// Sets the default signature verification level.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="level">The signature level.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_default_siglevel")]
    public static partial int SetDefaultSigLevel(IntPtr handle, AlpmSigLevel level);

    /// <summary>
    /// Gets the signature verification level for local files.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>The signature level.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_local_file_siglevel")]
    public static partial AlpmSigLevel GetLocalFileSigLevel(IntPtr handle);

    /// <summary>
    /// Sets the signature verification level for local files.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="level">The signature level.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_local_file_siglevel")]
    public static partial int SetLocalFileSigLevel(IntPtr handle, AlpmSigLevel level);

    /// <summary>
    /// Gets the signature verification level for remote files.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>The signature level.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_remote_file_siglevel")]
    public static partial AlpmSigLevel GetRemoteFileSigLevel(IntPtr handle);

    /// <summary>
    /// Sets the signature verification level for remote files.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="level">The signature level.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_remote_file_siglevel")]
    public static partial int SetRemoteFileSigLevel(IntPtr handle, AlpmSigLevel level);

    /// <summary>
    /// Gets the number of parallel downloads.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>The number of parallel downloads.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_get_parallel_downloads")]
    public static partial int GetParallelDownloads(IntPtr handle);

    /// <summary>
    /// Sets the number of parallel downloads.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="num_streams">The number of parallel downloads.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_option_set_parallel_downloads")]
    public static partial int SetParallelDownloads(IntPtr handle, uint num_streams);

}