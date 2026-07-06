using GLib;
using Gtk;
using Shelly.Utilities.Eventing;
using Shelly.Utilities.Models;
using static Shelly.GTK.Resources.Translations;
using WrapMode = Gtk.WrapMode;

namespace Shelly.Gtk.Windows.Dialog;

/// <summary>
///     Consolidated PKGBUILD review dialog. Always renders the unified diff and,
///     when present, layers the install-scriptlet warnings on top — replacing the
///     former separate <c>PackageBuildDialog</c> and <c>PkgbuildWarningDialog</c>.
/// </summary>
public static class PkgbuildReviewDialog
{
    public static Task<bool> ShowAsync(Window? parent, string packageName,
        IReadOnlyList<PkgbuildDiffLine> diff,
        IReadOnlyList<PkgbuildWarningDto> warnings,
        IReadOnlyDictionary<string, string>? sourceFiles = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        var hasWarnings = warnings.Count > 0;
        var changesReviewed = !hasWarnings;
        
        var dialog = Window.New();
        dialog.SetTitle(T("Review PKGBUILD changes"));
        if (parent is not null)
            dialog.SetTransientFor(parent);
        dialog.SetModal(true);
        dialog.SetDefaultSize(720, 560);
        dialog.SetResizable(true);
        
        var outer = Box.New(Orientation.Vertical, 12);
        outer.SetMarginTop(16);
        outer.SetMarginBottom(16);
        outer.SetMarginStart(16);
        outer.SetMarginEnd(16);

        var heading = Label.New(string.Format(
            T("Review PKGBUILD changes for {0}"), packageName));
        heading.SetXalign(0);
        heading.SetWrap(true);
        heading.AddCssClass("title-3");
        outer.Append(heading);
        
        var notebook = Notebook.New();
        notebook.SetVexpand(true);
        notebook.SetHexpand(true);
        notebook.SetScrollable(true);
        
        // Diff section — the part that the regression dropped.
        var diffBox = Box.New(Orientation.Vertical, 0);
        diffBox.SetHalign(Align.Fill);
        diffBox.SetHexpand(true);

        foreach (var line in diff)
        {
            var lineLabel = Label.New(string.Empty);
            lineLabel.SetHalign(Align.Fill);
            lineLabel.SetHexpand(true);
            lineLabel.SetXalign(0);
            lineLabel.SetJustify(Justification.Left);

            var escaped = Markup.EscapeText(line.Text);
            var markup = line.Kind switch
            {
                PkgbuildDiffKind.Added => $"<tt><span foreground=\"#26a269\">+ {escaped}</span></tt>",
                PkgbuildDiffKind.Removed => $"<tt><span foreground=\"#c01c28\">- {escaped}</span></tt>",
                _ => $"<tt>  {escaped}</tt>"
            };
            lineLabel.SetMarkup(markup);
            diffBox.Append(lineLabel);
        }

        var diffScroll = ScrolledWindow.New();
        diffScroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        diffScroll.SetVexpand(true);
        diffScroll.SetHexpand(true);
        diffScroll.SetChild(diffBox);

        var diffFrame = Frame.New(null);
        diffFrame.SetChild(diffScroll);
        
        // Warnings section — only when PostInstallValidator produced findings.
        if (hasWarnings)
        {
            var warningsBox = Box.New(Orientation.Vertical, 6);
            warningsBox.SetMarginTop(8);
            warningsBox.SetMarginBottom(8);
            warningsBox.SetMarginStart(8);
            warningsBox.SetMarginEnd(8);

            var subtitle = Label.New(T(
                "These commands fetch and execute code outside of shelly and libalpm's control. Review them before continuing."));
            subtitle.SetXalign(0);
            subtitle.SetWrap(true);
            subtitle.AddCssClass("error");
            warningsBox.Append(subtitle);

            foreach (var warning in warnings)
                warningsBox.Append(MakeWarningRow(warning));

            var warningsScroll = ScrolledWindow.New();
            warningsScroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            warningsScroll.SetVexpand(true);
            warningsScroll.SetHexpand(true);
            warningsScroll.SetChild(warningsBox);

            var warningsTabLabel = Box.New(Orientation.Horizontal, 4);
            var warningsIcon = Image.NewFromIconName("dialog-warning-symbolic");
            warningsIcon.SetPixelSize(14);
            warningsTabLabel.Append(warningsIcon);
            warningsTabLabel.Append(Label.New(string.Format(T("Warnings ({0})"), warnings.Count)));

            notebook.AppendPage(warningsScroll, warningsTabLabel);
        }
        
        var changesPage = notebook.AppendPage(diffFrame, Label.New(T("Changes")));
        outer.Append(notebook);
        outer.Append(MakeScanStatusBanner(hasWarnings, warnings.Count));
        
        var buttonBox = Box.New(Orientation.Horizontal, 8);
        buttonBox.SetHalign(Align.End);

        var cancel = Button.NewWithLabel(T("Cancel"));
        cancel.OnClicked += (_, _) =>
        {
            tcs.TrySetResult(false);
            dialog.Close();
        };
        buttonBox.Append(cancel);

        // When warnings exist, use the cautious "Install Anyway" affordance and
        // focus Cancel; otherwise Confirm is the suggested default.
        var proceed = Button.NewWithLabel(
            hasWarnings ? T("Review Changes") : T("Confirm"));        
        proceed.AddCssClass("suggested-action");

        notebook.OnSwitchPage += (_, e) =>
        {
            if (e.PageNum == changesPage)
                MarkChangesReviewed();
        };
        
        proceed.OnClicked += (_, _) =>
        {
            if (!changesReviewed)
            {
                notebook.SetCurrentPage(changesPage);
                MarkChangesReviewed();
                return;
            }

            tcs.TrySetResult(true);
            dialog.Close();
        };     
        
        buttonBox.Append(proceed);

        outer.Append(buttonBox);

        dialog.SetChild(outer);

        // Treat window close (X / Escape) as a cancellation.
        dialog.OnCloseRequest += (_, _) =>
        {
            tcs.TrySetResult(false);
            return false;
        };

        dialog.Present();
        if (hasWarnings)
            cancel.GrabFocus();
        else
            proceed.GrabFocus();

        return tcs.Task;

        void MarkChangesReviewed()
        {
            if (changesReviewed)
                return;

            changesReviewed = true;

            proceed.RemoveCssClass("suggested-action");
            proceed.AddCssClass("destructive-action");
            proceed.SetLabel(T("Install Anyway"));
        }
    }

    private static Box MakeWarningRow(PkgbuildWarningDto warning)
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

        var matchedLine = Label.New(null);
        matchedLine.SetMarkup($"<tt>{Markup.EscapeText(warning.MatchedLine)}</tt>");
        matchedLine.SetSelectable(true);
        matchedLine.SetWrap(true);
        matchedLine.SetXalign(0);

        var frame = Frame.New(null);
        frame.SetChild(matchedLine);
        box.Append(frame);

        return box;
    }
    
    private static Box MakeSourceFileRow(string name, string content)
    {
        var box = Box.New(Orientation.Vertical, 6);
        box.SetMarginTop(6);

        var title = Label.New(name);
        title.SetXalign(0);
        title.AddCssClass("heading");
        box.Append(title);

        var view = TextView.New();
        view.SetEditable(false);
        view.SetMonospace(true);
        view.SetWrapMode(WrapMode.WordChar);
        view.SetCursorVisible(false);
        view.GetBuffer().SetText(content, content.Length);

        var scroll = ScrolledWindow.New();
        scroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scroll.SetMinContentHeight(160);
        scroll.SetChild(view);

        var frame = Frame.New(null);
        frame.SetChild(scroll);
        box.Append(frame);

        return box;
    }
    
    private static Box MakeScanStatusBanner(bool hasWarnings, int warningCount)
    {
        var banner = Box.New(Orientation.Horizontal, 8);
        banner.SetMarginTop(4);
        banner.SetMarginBottom(8);
        banner.SetHalign(Align.Fill);

        var iconName = hasWarnings
            ? "dialog-warning-symbolic"
            : "security-high-symbolic";

        var icon = Image.NewFromIconName(iconName);
        icon.SetPixelSize(16);
        banner.Append(icon);

        var text = hasWarnings
            ? string.Format(T("Security scan completed - {0} warning(s) found."), warningCount)
            : T("Security scan completed - no issues found.");

        var label = Label.New(text);
        label.SetXalign(0);
        label.SetWrap(true);
        label.AddCssClass(hasWarnings ? "warning" : "success");
        banner.Append(label);

        return banner;
    }
}