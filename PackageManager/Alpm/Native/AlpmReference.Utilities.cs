using System;
using System.Runtime.InteropServices;
using PackageManager.Alpm.Enums;

namespace PackageManager.Alpm.Native;

internal static partial class AlpmReference
{
    /// <summary>
    /// Computes the MD5 sum of a file.
    /// </summary>
    /// <param name="filename">The file path.</param>
    /// <returns>The MD5 sum string pointer.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_compute_md5sum", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr ComputeMd5Sum(string filename);

    /// <summary>
    /// Computes the SHA256 sum of a file.
    /// </summary>
    /// <param name="filename">The file path.</param>
    /// <returns>The SHA256 sum string pointer.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_compute_sha256sum", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr ComputeSha256Sum(string filename);

    /// <summary>
    /// Gets the error string for a given error number.
    /// </summary>
    /// <param name="err">The error number.</param>
    /// <returns>The error string.</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_strerror")]
    public static partial IntPtr StrError(AlpmErrno err);

    /// <summary>
    /// Computes a string representation of a dependency.
    /// </summary>
    /// <param name="dep">The dependency pointer.</param>
    /// <returns>A pointer to the computed string (must be freed by caller).</returns>
    [LibraryImport(LibName, EntryPoint = "alpm_dep_compute_string")]
    public static partial IntPtr DepComputeString(IntPtr dep);
}