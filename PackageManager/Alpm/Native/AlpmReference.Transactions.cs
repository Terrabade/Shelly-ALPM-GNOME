using System;
using System.Runtime.InteropServices;
using PackageManager.Alpm.Enums;

namespace PackageManager.Alpm.Native;

internal static partial class AlpmReference
{
    /// <summary>
        /// Gets the flags of the current transaction.
        /// </summary>
        /// <param name="handle">The alpm handle.</param>
        /// <returns>The transaction flags.</returns>
        [LibraryImport(LibName, EntryPoint = "alpm_trans_get_flags")]
        public static partial AlpmTransFlag TransGetFlags(IntPtr handle);

        /// <summary>
        /// Gets the list of packages to be added in the current transaction.
        /// </summary>
        /// <param name="handle">The alpm handle.</param>
        /// <returns>A pointer to a list of packages.</returns>
        [LibraryImport(LibName, EntryPoint = "alpm_trans_get_add")]
        public static partial IntPtr TransGetAdd(IntPtr handle);

        /// <summary>
        /// Gets the list of packages to be removed in the current transaction.
        /// </summary>
        /// <param name="handle">The alpm handle.</param>
        /// <returns>A pointer to a list of packages.</returns>
        [LibraryImport(LibName, EntryPoint = "alpm_trans_get_remove")]
        public static partial IntPtr TransGetRemove(IntPtr handle);

        /// <summary>
        /// Initializes a new transaction.
        /// </summary>
        /// <param name="handle">The alpm handle.</param>
        /// <param name="flags">The transaction flags.</param>
        /// <returns>0 on success, -1 on error.</returns>
        [LibraryImport(LibName, EntryPoint = "alpm_trans_init")]
        public static partial int TransInit(IntPtr handle, AlpmTransFlag flags);

        /// <summary>
        /// Prepares the transaction.
        /// </summary>
        /// <param name="handle">The alpm handle.</param>
        /// <param name="data">A pointer to return error data if preparation fails.</param>
        /// <returns>0 on success, -1 on error.</returns>
        [LibraryImport(LibName, EntryPoint = "alpm_trans_prepare")]
        public static partial int TransPrepare(IntPtr handle, out IntPtr data);

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        /// <param name="handle">The alpm handle.</param>
        /// <param name="data">A pointer to return error data if commit fails.</param>
        /// <returns>0 on success, -1 on error.</returns>
        [LibraryImport(LibName, EntryPoint = "alpm_trans_commit")]
        public static partial int TransCommit(IntPtr handle, out IntPtr data);

        /// <summary>
        /// Interrupts the current transaction.
        /// </summary>
        /// <param name="handle">The alpm handle.</param>
        /// <returns>0 on success, -1 on error.</returns>
        [LibraryImport(LibName, EntryPoint = "alpm_trans_interrupt")]
        public static partial int TransInterrupt(IntPtr handle);

        /// <summary>
        /// Releases the current transaction.
        /// </summary>
        /// <param name="handle">The alpm handle.</param>
        /// <returns>0 on success, -1 on error.</returns>
        [LibraryImport(LibName, EntryPoint = "alpm_trans_release")]
        public static partial int TransRelease(IntPtr handle);

        /// <summary>
        /// Performs a system upgrade.
        /// </summary>
        /// <param name="handle">The alpm handle.</param>
        /// <param name="enable_downgrade">Whether to enable downgrades.</param>
        /// <returns>0 on success, -1 on error.</returns>
        [LibraryImport(LibName, EntryPoint = "alpm_sync_sysupgrade")]
        public static partial int SyncSysupgrade(IntPtr handle, [MarshalAs(UnmanagedType.Bool)] bool enable_downgrade);

        /// <summary>
        /// Adds a package to the transaction.
        /// </summary>
        /// <param name="handle">The alpm handle.</param>
        /// <param name="pkg">The package to add.</param>
        /// <returns>0 on success, -1 on error.</returns>
        [LibraryImport(LibName, EntryPoint = "alpm_add_pkg")]
        public static partial int AddPkg(IntPtr handle, IntPtr pkg);

        /// <summary>
        /// Removes a package from the transaction.
        /// </summary>
        /// <param name="handle">The alpm handle.</param>
        /// <param name="pkg">The package to remove.</param>
        /// <returns>0 on success, -1 on error.</returns>
        [LibraryImport(LibName, EntryPoint = "alpm_remove_pkg")]
        public static partial int RemovePkg(IntPtr handle, IntPtr pkg);
}