namespace Shelly.Gtk.Helpers;

public static class FlatpakRefHandler
{
    private static string? _pendingAppId;
    public static event Func<string, string, Task>? InstallRequested;

    public static void RegisterAppId(string appId)
    {
        _pendingAppId = appId;
    }
    
    public static void OnPageLoaded()
    {
        if (_pendingAppId is not { } appId)
            return;

        _pendingAppId = null;
        
        RequestInstall(appId);

    }

    public static void RequestInstall(string appId)
    {
        Console.Error.WriteLine($"RequestInstall chamado, handlers: {InstallRequested?.GetInvocationList().Length ?? 0}");
        if (InstallRequested != null)
            _ = InstallRequested.Invoke(appId, "flathub");
    }

}