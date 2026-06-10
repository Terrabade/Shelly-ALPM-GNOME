using Gtk.Internal;
using Shelly.Gtk.Helpers;
using Shelly.Gtk.UiModels;
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
            PkgbuildDiffQuestionDto d => new PkgbuildDiffAnswer(d.QuestionId, 
                await ShowPkgbuildDialogAsync(d, genericQuestionService)),
            _ => throw new InvalidOperationException($"Unhandled QuestionRequest {req.GetType()}")
        };
        
        var frame = JsonPackFrame.EncodeFrame<QuestionResponseDto>(resp);
        await writeFrame(frame);
        return true;
    }
    private static Task<bool> ShowPkgbuildDialogAsync(
        PkgbuildDiffQuestionDto dto,
        IGenericQuestionService genericQuestionService)
    {
        var args = new PackageBuildDiffEventArgs(
            dto.PackageName,
            dto.DiffLines!); 

        genericQuestionService.RaisePackageBuildDiff(args);

        return args.ResponseTask;
    }
}
