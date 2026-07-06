using System.Runtime.InteropServices;

namespace Shelly.Gtk.Services.TrayServices;

public sealed partial class TrayDBus : ITrayDbus, IDisposable
{
    private const string LibGio = "gio-2.0";
    private const string LibGObject = "gobject-2.0";
    private const string LibGLib = "glib-2.0";

    private IntPtr _connection = IntPtr.Zero;

    [LibraryImport(LibGio, EntryPoint = "g_bus_get_sync")]
    private static partial IntPtr g_bus_get_sync(int busType, IntPtr cancellable, out IntPtr error);

    [LibraryImport(LibGio, EntryPoint = "g_dbus_connection_call_sync", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr g_dbus_connection_call_sync(
        IntPtr connection,
        string? busName,
        string objectPath,
        string interfaceName,
        string methodName,
        IntPtr parameters,
        IntPtr replyType,
        int flags,
        int timeoutMsec,
        IntPtr cancellable,
        out IntPtr error);

    [LibraryImport(LibGObject, EntryPoint = "g_object_unref")]
    private static partial void g_object_unref(IntPtr obj);

    [LibraryImport(LibGLib, EntryPoint = "g_variant_unref")]
    private static partial void g_variant_unref(IntPtr variant);

    public void Dispose()
    {
        if (_connection == IntPtr.Zero) return;
        g_object_unref(_connection);
        _connection = IntPtr.Zero;
    }

    public Task RefreshSettingsAsync()
    {
        return Task.Run(() => CallTray("RefreshSettings"));
    }

    public Task UpdatesMadeInUiAsync()
    {
        return Task.Run(() => CallTray("UpdatesMadeInUi"));
    }

    private void CallTray(string method)
    {
        if (_connection == IntPtr.Zero)
        {
            _connection = g_bus_get_sync(2, IntPtr.Zero, out _);
        }

        if (_connection == IntPtr.Zero) return;

        var result = g_dbus_connection_call_sync(
            _connection,
            ShellyConstants.TrayService,
            ShellyConstants.TrayPath,
            ShellyConstants.TrayInterface,
            method,
            IntPtr.Zero,
            IntPtr.Zero,
            0,
            -1,
            IntPtr.Zero,
            out _);

        if (result != IntPtr.Zero)
        {
            g_variant_unref(result);
        }
    }
}