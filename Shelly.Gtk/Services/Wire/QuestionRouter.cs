using Gtk;
using Shelly.Gtk.Helpers;
using Shelly.Gtk.Windows.Dialog;
using Shelly.Utilities.Eventing;
using Shelly.Utilities.Models;


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
        // Always present the PKGBUILD diff; scriptlet warnings (when present) are
        // layered on top. Compute the diff on this side from the old/new PKGBUILD
        // already carried on the wire (no DTO/protocol change required).
        var warnings = d.Warnings ?? [];
        var diff = PkgbuildDiff.BuildLines(d.OldPkgbuild ?? string.Empty, d.NewPkgbuild ?? string.Empty);

        // GTK widgets must only be touched from the main thread; marshal there
        // and bridge the dialog result back to this background wire thread.
        var tcs = new TaskCompletionSource<bool>();

        GLib.Functions.IdleAdd(0, () =>
        {
            var parent = (Gio.Application.GetDefault() as Application)?.GetActiveWindow();
            _ = PkgbuildReviewDialog.ShowAsync(parent, d.PackageName, diff, warnings)
                .ContinueWith(t => tcs.TrySetResult(t.IsCompletedSuccessfully && t.Result));
            return false;
        });

        return tcs.Task;
    }
}
