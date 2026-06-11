using System.Collections.Generic;
using System.Threading.Tasks;
using Gtk;
using Shelly.Utilities.Eventing;
using static Shelly.GTK.Resources.Translations;

namespace Shelly.Gtk.Windows.Dialog;


public static class PkgbuildWarningDialog
{
    public static Task<bool> ShowAsync(Window? parent, string packageName,
        IReadOnlyList<PkgbuildWarningDto> warnings)
    {
        var tcs = new TaskCompletionSource<bool>();

        var dialog = Window.New();
        dialog.SetTitle(T("Review install scriptlet warnings"));
        if (parent is not null)
            dialog.SetTransientFor(parent);
        dialog.SetModal(true);
        dialog.SetDefaultSize(720, 560);

        var outer = Box.New(Orientation.Vertical, 12);
        outer.SetMarginTop(16);
        outer.SetMarginBottom(16);
        outer.SetMarginStart(16);
        outer.SetMarginEnd(16);

        var heading = Label.New(string.Format(
            T("The install scriptlet for {0} runs external tools"), packageName));
        heading.SetXalign(0);
        heading.SetWrap(true);
        heading.AddCssClass("title-3");
        outer.Append(heading);

        var subtitle = Label.New(T(
            "These commands fetch and execute code outside of pacman's control. Review them before continuing."));
        subtitle.SetXalign(0);
        subtitle.SetWrap(true);
        outer.Append(subtitle);

        foreach (var warning in warnings)
            outer.Append(MakeWarningRow(warning));

        var buttonBox = Box.New(Orientation.Horizontal, 8);
        buttonBox.SetHalign(Align.End);

        var cancel = Button.NewWithLabel(T("Cancel"));
        cancel.OnClicked += (_, _) =>
        {
            tcs.TrySetResult(false);
            dialog.Close();
        };
        buttonBox.Append(cancel);

        var proceed = Button.NewWithLabel(T("Install Anyway"));
        proceed.AddCssClass("destructive-action");
        proceed.OnClicked += (_, _) =>
        {
            tcs.TrySetResult(true);
            dialog.Close();
        };
        buttonBox.Append(proceed);

        outer.Append(buttonBox);

        var scroll = ScrolledWindow.New();
        scroll.SetChild(outer);
        scroll.SetVexpand(true);
        scroll.SetHexpand(true);
        dialog.SetChild(scroll);

        // Treat window close (X / Escape) as a cancellation.
        dialog.OnCloseRequest += (_, _) =>
        {
            tcs.TrySetResult(false);
            return false;
        };

        dialog.Present();
        cancel.GrabFocus();

        return tcs.Task;
    }

    private static Widget MakeWarningRow(PkgbuildWarningDto warning)
    {
        var box = Box.New(Orientation.Vertical, 6);
        box.SetMarginTop(6);

        var title = Label.New(string.Format(
            T("{0} used in {1}"), warning.Tool, warning.Hook));
        title.SetXalign(0);
        title.AddCssClass("heading");
        title.AddCssClass(warning.Severity == "Critical" ? "error" : "warning");
        box.Append(title);

        if (!string.IsNullOrWhiteSpace(warning.Message))
        {
            var message = Label.New(warning.Message);
            message.SetXalign(0);
            message.SetWrap(true);
            box.Append(message);
        }

        var view = TextView.New();
        view.SetEditable(false);
        view.SetMonospace(true);
        view.SetWrapMode(WrapMode.WordChar);
        view.SetCursorVisible(false);
        view.GetBuffer().SetText(warning.MatchedLine, warning.MatchedLine.Length);

        var frame = Frame.New(null);
        frame.SetChild(view);
        box.Append(frame);

        return box;
    }
}
