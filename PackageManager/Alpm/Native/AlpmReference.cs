using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PackageManager.Alpm.Enums;

[assembly: InternalsVisibleTo("PackageManager.Tests")]

namespace PackageManager.Alpm.Native;

internal static partial class AlpmReference
{
    public const string LibName = "alpm";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AlpmEventCallback(IntPtr ctx, IntPtr eventPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AlpmQuestionCallback(IntPtr ctx, IntPtr questionPtr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AlpmProgressCallback(IntPtr ctx, AlpmProgressType progress,
        IntPtr pkg, int percent, ulong howmany, ulong current);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int AlpmFetchCallback(IntPtr ctx, IntPtr url, IntPtr localpath, int force);


    [LibraryImport(LibName, EntryPoint = "alpm_initialize", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr Initialize(string root, string dbpath, out AlpmErrno error);


    [LibraryImport(LibName, EntryPoint = "alpm_release")]
    public static partial int Release(IntPtr handle);


    [LibraryImport(LibName, EntryPoint = "alpm_errno")]
    public static partial AlpmErrno ErrorNumber(IntPtr handle);

    [LibraryImport(LibName, EntryPoint = "alpm_capabilities")]
    public static partial int Capabilities();


    [LibraryImport(LibName, EntryPoint = "alpm_unlock")]
    public static partial int Unlock(IntPtr handle);

    static AlpmReference()
    {
        NativeResolver.Initialize();
    }
}