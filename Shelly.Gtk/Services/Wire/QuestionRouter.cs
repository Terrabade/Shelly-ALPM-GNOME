using Gtk;
using Gtk.Internal;
using Shelly.Gtk.Helpers;
using Shelly.Gtk.UiModels;
using Shelly.Gtk.Windows.Dialog;
using Shelly.Utilities.Eventing;
using Shelly.Gtk.Services;
using Shelly.Gtk.Windows.Dialog;

namespace Shelly.Gtk.Services.Wire;

internal static class QuestionRouter
{
    
    public static async Task<bool> TryDispatchAsync(string base64, Func<string,
            Task> writeFrame, IGenericQuestionService genericQuestionService)    
    {
        if (!JsonPackFrame.TryDecodePayload<QuestionRequest>(base64, out var req) || req is null)
            return false;
        
        QuestionResponseDto resp = req switch
        {
            PkgbuildDiffQuestionDto d => new PkgbuildDiffAnswer(
                d.QuestionId,
                await PromptPkgbuildDiffAsync(d)),
            _ => throw new InvalidOperationException($"Unhandled QuestionRequest {req.GetType()}")
        };
        
        var frame = JsonPackFrame.EncodeFrame<QuestionResponseDto>(resp);
        await writeFrame(frame);
        return true;
    }

    private static Task<bool> PromptPkgbuildDiffAsync(PkgbuildDiffQuestionDto d)
    {
        // No findings → preserve the previous auto-approve behavior.
        // Guard against a null list (absent on the wire / fresh install).
        var warnings = d.Warnings ?? [];
        if (warnings.Count == 0)
            return Task.FromResult(true);

        // GTK widgets must only be touched from the main thread; marshal there
        // and bridge the dialog result back to this background wire thread.
        var tcs = new TaskCompletionSource<bool>();

        GLib.Functions.IdleAdd(0, () =>
        {
            var parent = (Gio.Application.GetDefault() as Application)?.GetActiveWindow();
            _ = PkgbuildWarningDialog.ShowAsync(parent, d.PackageName, warnings)
                .ContinueWith(t => tcs.TrySetResult(t.IsCompletedSuccessfully && t.Result));
            return false;
        });

        return tcs.Task;
    }
}
