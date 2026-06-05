using Gtk;
using Pango;
using Shelly.Gtk.UiModels;
using static Shelly.GTK.Resources.Translations;
using WrapMode = Gtk.WrapMode;

namespace Shelly.Gtk.Windows.Dialog;

public static class PackageBuildDiffDialog
{
    public static void ShowPackageBuildDiffDialog(
        Overlay parentOverlay,
        PackageBuildDiffEventArgs e)
    {
        var baseFrame = Frame.New(null);
        baseFrame.SetHalign(Align.Center);
        baseFrame.SetValign(Align.Center);
        baseFrame.SetSizeRequest(1000, 700);
        baseFrame.SetMarginTop(20);
        baseFrame.SetMarginBottom(20);
        baseFrame.SetMarginStart(20);
        baseFrame.SetMarginEnd(20);
        baseFrame.AddCssClass("background");
        baseFrame.AddCssClass("dialog-overlay");
        baseFrame.SetOverflow(Overflow.Hidden);

        var rootBox = Box.New(Orientation.Vertical, 12);
        rootBox.SetMarginTop(12);
        rootBox.SetMarginBottom(12);
        rootBox.SetMarginStart(12);
        rootBox.SetMarginEnd(12);

        baseFrame.SetChild(rootBox);

        var titleLabel = Label.New(T($"PKGBUILD Diff - {e.PackageName}"));
        titleLabel.AddCssClass("title-4");
        titleLabel.SetHalign(Align.Start);

        rootBox.Append(titleLabel);

        var descriptionLabel = Label.New(
            T("Review the PKGBUILD changes before continuing."));
        descriptionLabel.SetWrap(true);
        descriptionLabel.SetXalign(0);

        rootBox.Append(descriptionLabel);
        
        rootBox.Append(CreatePkgbuildDiffPanel(e.DiffLines!));
        
        var buttonBox = Box.New(Orientation.Horizontal, 8);
        buttonBox.SetHalign(Align.End);

        var cancelButton = Button.NewWithLabel(T("Cancel"));
        var confirmButton = Button.NewWithLabel(T("Accept Changes"));

        confirmButton.AddCssClass("suggested-action");

        cancelButton.OnClicked += (_, _) =>
        {
            e.SetResponse(false);
            parentOverlay.RemoveOverlay(baseFrame);
        };

        confirmButton.OnClicked += (_, _) =>
        {
            e.SetResponse(true);
            parentOverlay.RemoveOverlay(baseFrame);
        };

        buttonBox.Append(cancelButton);
        buttonBox.Append(confirmButton);

        rootBox.Append(buttonBox);

        parentOverlay.AddOverlay(baseFrame);
    }

    private static Widget CreatePkgbuildDiffPanel(
        IReadOnlyList<string>? diffLines)
    {
        var buffer = TextBuffer.New(null);

        var tagTable = buffer.GetTagTable();

        var addedTag = TextTag.New("added");
        addedTag.Foreground = "#4CAF50";
        var removedTag = TextTag.New("removed");
        removedTag.Foreground = "#E57373";        
        var headerTag = TextTag.New("header");
        headerTag.Foreground = "#64B5F6";
        var fileTag = TextTag.New("file");
        fileTag.Weight = 700;
        
        tagTable.Add(addedTag);
        tagTable.Add(removedTag);
        tagTable.Add(headerTag);
        tagTable.Add(fileTag);
        
        bool inAddedBlock = false;
        bool inRemovedBlock = false;
        
        if (diffLines != null)
        {
            foreach (var line in diffLines)
            {
                buffer.GetEndIter(out var start);

                var startOffset = start.GetOffset();
                
                buffer.Insert(
                    start,
                    line + Environment.NewLine,
                    -1);
                
                buffer.GetIterAtOffset(
                    out var tagStart,
                    startOffset);
                
                buffer.GetEndIter(out var tagEnd);
                
                if (line.StartsWith("@@"))
                {
                    buffer.ApplyTag(
                        headerTag,
                        tagStart,
                        tagEnd);

                    inAddedBlock = false;
                    inRemovedBlock = false;
                }
                else if (line.StartsWith("---") ||
                         line.StartsWith("+++"))
                {
                    buffer.ApplyTag(
                        fileTag,
                        tagStart,
                        tagEnd);

                    inAddedBlock = false;
                    inRemovedBlock = false;
                }
                else if (line.StartsWith("+"))
                {
                    inAddedBlock = line.Contains('(');
                    inRemovedBlock = false;

                    buffer.ApplyTag(
                        addedTag,
                        tagStart,
                        tagEnd);
                }
                else if (line.StartsWith("-"))
                {
                    inRemovedBlock = line.Contains('(');
                    inAddedBlock = false;

                    buffer.ApplyTag(
                        removedTag,
                        tagStart,
                        tagEnd);
                }
                else if (inAddedBlock)
                {
                    buffer.ApplyTag(
                        addedTag,
                        tagStart,
                        tagEnd);

                    if (line.Contains(')'))
                        inAddedBlock = false;
                }
                else if (inRemovedBlock)
                {
                    buffer.ApplyTag(
                        removedTag,
                        tagStart,
                        tagEnd);

                    if (line.Contains(')'))
                        inRemovedBlock = false;
                }
            }
        }

        
        var textView = TextView.NewWithBuffer(buffer);
        textView.SetEditable(false);
        textView.SetCursorVisible(false);
        textView.SetMonospace(true);
        textView.SetWrapMode(WrapMode.None);
        textView.SetVexpand(true);
        textView.SetHexpand(true);

        var scroll = ScrolledWindow.New();
        scroll.SetPolicy(
            PolicyType.Automatic,
            PolicyType.Automatic);

        scroll.SetChild(textView);
        scroll.SetVexpand(true);
        scroll.SetHexpand(true);
        
        return scroll;
    }
}