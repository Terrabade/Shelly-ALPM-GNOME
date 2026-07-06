using System;
using System.Runtime.InteropServices;
using PackageManager.Alpm.Enums;

namespace PackageManager.Alpm.Native;

internal static partial class AlpmReference
{
    [LibraryImport(LibName, EntryPoint = "alpm_get_localdb")]
    public static partial IntPtr GetLocalDb(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "alpm_get_syncdbs")]
    public static partial IntPtr GetSyncDbs(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "alpm_register_syncdb", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr RegisterSyncDb(IntPtr handle, string treename, AlpmSigLevel level);

    /// <summary>
    /// Unregisters all sync databases.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_unregister_all_syncdbs")]
    public static partial int UnregisterAllSyncDbs(IntPtr handle);

    /// <summary>
    /// Unregisters a specific database.
    /// </summary>
    /// <param name="db">The database to unregister.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_unregister")]
    public static partial int DbUnregister(IntPtr db);

    /// <summary>
    /// Gets the name of a database.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <returns>The name of the database.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_get_name")]
    public static partial IntPtr DbGetName(IntPtr db);

    /// <summary>
    /// Gets the signature verification level of a database.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <returns>The signature level.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_get_siglevel")]
    public static partial AlpmSigLevel DbGetSigLevel(IntPtr db);

    /// <summary>
    /// Gets whether a database is valid.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <returns>1 if valid, 0 otherwise.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_get_valid")]
    public static partial int DbGetValid(IntPtr db);

    /// <summary>
    /// Gets the list of servers for a database.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <returns>A pointer to a list of server URLs.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_get_servers")]
    public static partial IntPtr DbGetServers(IntPtr db);

    /// <summary>
    /// Sets the list of servers for a database.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <param name="servers">A pointer to a list of server URLs.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_set_servers")]
    public static partial int DbSetServers(IntPtr db, IntPtr servers);

    /// <summary>
    /// Adds a server to a database.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <param name="url">The server URL.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_add_server", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int DbAddServer(IntPtr db, string url);

    /// <summary>
    /// Removes a server from a database.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <param name="url">The server URL.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_remove_server", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int DbRemoveServer(IntPtr db, string url);

    /// <summary>
    /// Updates a list of databases.
    /// </summary>
    /// <param name="handle">The alpm handle.</param>
    /// <param name="databases">A pointer to a list of databases to update.</param>
    /// <param name="force">Whether to force the update.</param>
    /// <returns>0 on success, 1 if up-to-date, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_update")]
    public static partial int Update(IntPtr handle, IntPtr databases, [MarshalAs(UnmanagedType.Bool)] bool force);

    /// <summary>
    /// Gets a package from a database.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <param name="name">The name of the package.</param>
    /// <returns>A pointer to the package, or IntPtr.Zero if not found.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_get_pkg", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr DbGetPkg(IntPtr db, string name);

    /// <summary>
    /// Gets the package cache for a database.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <returns>A pointer to the package cache.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_get_pkgcache")]
    public static partial IntPtr DbGetPkgCache(IntPtr db);

    /// <summary>
    /// Gets the group cache for a database.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <returns>A pointer to the group cache.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_get_groupcache")]
    public static partial IntPtr DbGetGroupCache(IntPtr db);

    /// <summary>
    /// Searches for packages in a database.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <param name="needles">A pointer to a list of search terms.</param>
    /// <param name="results">A pointer to the list of results.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_search")]
    public static partial int DbSearch(IntPtr db, IntPtr needles, out IntPtr results);

    /// <summary>
    /// Sets the usage of a database.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <param name="usage">The database usage.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_set_usage")]
    public static partial int DbSetUsage(IntPtr db, AlpmDbUsage usage);

    /// <summary>
    /// Gets the usage of a database.
    /// </summary>
    /// <param name="db">The database handle.</param>
    /// <param name="usage">The database usage.</param>
    /// <returns>0 on success, -1 on error.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_db_get_usage")]
    public static partial int DbGetUsage(IntPtr db, out AlpmDbUsage usage);
}