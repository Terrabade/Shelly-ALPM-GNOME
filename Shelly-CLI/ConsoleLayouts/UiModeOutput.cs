using PackageManager.Alpm;
using PackageManager.Aur;
using PackageManager.Wire;
using Shelly_CLI.Utility;
using Shelly.Utilities.Eventing;

namespace Shelly_CLI.ConsoleLayouts;

internal static class UiModeOutput
{
    public static async Task<bool> Run(AurPackageManager mananger, Func<AurPackageManager, Task> operation)
    {
        var hadError = false;
        return !hadError;
    }

    private static void Attach(AurPackageManager manager, Action onError)
    {
        manager.ErrorEvent += (_, e) =>
        {
            JsonPackFrame.WriteToStdout<Event>(new AlpmErrorEvent(EventLevel.Error, e.Error));
            onError();
        };
        manager.HookRun += (_, e) =>
        {
            JsonPackFrame.WriteToStdout<Event>(new AlpmHookEvent(EventLevel.Information, e.Description));
        };
        manager.ScriptletInfo += (_, e) =>
        {
            JsonPackFrame.WriteToStdout<Event>(new AlpmScriptletEvent(EventLevel.Information, e.Line));
        };
        manager.Replaces += (_, e) =>
        {
            JsonPackFrame.WriteToStdout<Event>(new AlpmReplaceEvent(e.Repository, e.PackageName, e.Replaces));
        };
        manager.Progress += (_, e) =>
        {
            JsonPackFrame.WriteToStdout<Event>(new AlpmPackageProgressEvent(e.PackageName ?? "Unknown Package",
                e.Current ?? 0, e.HowMany ?? 0, e.ProgressType.ToProgressType(), e.Percent ?? 0, null));
        };
        manager.BuildOutput += (_, e) =>
        {
            JsonPackFrame.WriteToStdout<Event>(new AlpmBuildOutputEvent(
                e.IsError ? EventLevel.Error : EventLevel.Information, e.PackageName, e.Percent ?? 0, e.ProgressMessage,
                e.Line));
        };
    }
}