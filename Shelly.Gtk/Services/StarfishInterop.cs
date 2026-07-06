using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shelly.Gtk.Services;

/// <summary>
/// P/Invoke wrapper for the native lib-starfish.so shared library.
/// </summary>
public static partial class StarfishInterop
{
    private const string LibName = "libstarfish";

    [LibraryImport(LibName, EntryPoint = "starfish_graph_widget_init")]
    private static partial int NativeInit();

    [LibraryImport(LibName, EntryPoint = "starfish_graph_widget_create")]
    private static partial nint NativeCreateWidget();

    [LibraryImport(LibName, EntryPoint = "starfish_graph_widget_create_display_only",
        StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint NativeCreateDisplayOnly(string json);

    [LibraryImport(LibName, EntryPoint = "starfish_graph_widget_shutdown")]
    private static partial void NativeShutdown();

    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized) return;
        var result = NativeInit();
        if (result != 0)
            throw new InvalidOperationException("Failed to initialize Starfish graph widget library.");
        _initialized = true;
    }

    public static global::Gtk.Widget CreateGraphWidget()
    {
        EnsureInitialized();
        var handle = NativeCreateWidget();
        if (handle == 0)
            throw new InvalidOperationException("Failed to create Starfish graph widget.");
        return global::Gtk.Widget.NewFromPointer(handle, false);
    }

    public static global::Gtk.Widget CreateDisplayOnlyGraphWidget(
        string rootPackage, Dictionary<string, List<string>> dependencyMap)
    {
        EnsureInitialized();
        var request = new StarfishDisplayOnlyRequest
        {
            RootPackage = rootPackage,
            DependencyMap = dependencyMap
        };
        var json = JsonSerializer.Serialize(request, StarfishJsonContext.Default.StarfishDisplayOnlyRequest);
        var handle = NativeCreateDisplayOnly(json);
        if (handle == 0)
            throw new InvalidOperationException("Failed to create Starfish display-only graph widget.");
        return global::Gtk.Widget.NewFromPointer(handle, false);
    }

    public static void Shutdown()
    {
        if (!_initialized) return;
        NativeShutdown();
        _initialized = false;
    }
}

public class StarfishDisplayOnlyRequest
{
    public string RootPackage { get; set; } = "";
    public Dictionary<string, List<string>> DependencyMap { get; set; } = new();
}

[JsonSerializable(typeof(StarfishDisplayOnlyRequest))]
public partial class StarfishJsonContext : JsonSerializerContext
{
}
