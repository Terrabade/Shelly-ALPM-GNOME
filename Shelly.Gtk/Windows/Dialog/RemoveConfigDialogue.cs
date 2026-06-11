using Gtk;
using Shelly.Gtk.Enums;
using Shelly.GTK.Resources;
using Shelly.Gtk.UiModels;

namespace Shelly.Gtk.Windows.Dialog;

public static class RemoveConfigDialogue
{
    public static GenericDialogEventArgs<ConfigRemoveEnum> BuildRemoveDialog()
    {
        var keepRadio = CheckButton.New();
        var deleteRadio = CheckButton.New();
        deleteRadio.SetGroup(keepRadio);
        keepRadio.Active = true;

        var listBox = ListBox.New();
        listBox.SelectionMode = SelectionMode.None;
        listBox.AddCssClass("boxed-list");

        var firstRadio = MakeRow(keepRadio, Translations.T("Keep Config"), Translations.T("Keep user data and configuration"));
        var secondRadio = MakeRow(deleteRadio, Translations.T("Delete Config"), Translations.T("Delete user data and configuration"));
        listBox.Append(firstRadio);
        listBox.Append(secondRadio);

        var gestureKeep = GestureClick.New();
        gestureKeep.OnReleased += (_, _) => keepRadio.Active = true;
        firstRadio.AddController(gestureKeep);

        var gestureRemove = GestureClick.New();
        gestureRemove.OnReleased += (_, _) => deleteRadio.Active = true;
        secondRadio.AddController(gestureRemove);

        var keepLabel = Label.New(Translations.T("Keep Config?"));
        keepLabel.AddCssClass("heading");

        var box = Box.New(Orientation.Vertical, 12);
        box.Append(keepLabel);
        box.Append(listBox);

        var buttonBox = Box.New(Orientation.Horizontal, 0);

        var dialogArgs = new GenericDialogEventArgs<ConfigRemoveEnum>(box);

        var closeButton = Button.NewWithLabel(Translations.T("Close"));
        closeButton.OnClicked += (_, _) => dialogArgs.SetResponse(ConfigRemoveEnum.Cancel);

        var removeButton = Button.NewWithLabel(Translations.T("Confirm"));
        removeButton.AddCssClass("suggested-action");
        removeButton.OnClicked += (_, _) =>
        {
            dialogArgs.SetResponse(keepRadio.Active
                ? ConfigRemoveEnum.KeepConfig
                : ConfigRemoveEnum.RemoveConfig);
        };

        closeButton.Hexpand = false;
        removeButton.Hexpand = false;

        buttonBox.Halign = Align.Fill;
        buttonBox.Hexpand = true;
        buttonBox.Homogeneous = true;
        buttonBox.Spacing = 5;
        buttonBox.Append(removeButton);
        buttonBox.Append(closeButton);
        box.Append(buttonBox);

        return dialogArgs;

        ListBoxRow MakeRow(CheckButton radio, string title, string subtitle)
        {
            var titleLabel = Label.New(title);
            titleLabel.Halign = Align.Start;

            var subtitleLabel = Label.New(subtitle);
            subtitleLabel.Halign = Align.Start;
            subtitleLabel.AddCssClass("dim-label");
            subtitleLabel.SetEllipsize(Pango.EllipsizeMode.End);

            var textBox = Box.New(Orientation.Vertical, 2);
            textBox.Hexpand = true;
            textBox.Append(titleLabel);
            textBox.Append(subtitleLabel);

            var rowBox = Box.New(Orientation.Horizontal, 12);
            rowBox.MarginTop = 10;
            rowBox.MarginBottom = 10;
            rowBox.MarginStart = 12;
            rowBox.MarginEnd = 12;
            radio.Valign = Align.Center;
            rowBox.Append(radio);
            rowBox.Append(textBox);

            var row = ListBoxRow.New();
            row.Child = rowBox;
            row.Activatable = true;
            row.OnActivate += (_, _) => radio.Active = true;
            return row;
        }
    }
}