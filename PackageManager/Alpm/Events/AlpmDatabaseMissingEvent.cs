using System;
using System.Runtime.InteropServices;
using PackageManager.Alpm.Enums;

namespace PackageManager.Alpm.Events;

[StructLayout(LayoutKind.Sequential)]
internal struct AlpmDatabaseMissingEvent
{
    public AlpmEventType Type; // 4 bytes

    // 4 bytes padding
    public IntPtr DbName; //8 bytes (offset 8)
}