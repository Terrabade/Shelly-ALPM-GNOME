using Gtk;
using Shelly.Gtk.Services;

namespace Shelly.Gtk.Windows.Dialog;

public static class DependencyGraphPreview
{
    public static void Show(
        Window? parent, 
        string packageName, 
        Dictionary<string, List<string>> dependencyMap)
    {
        var dialog = Window.New();
        dialog.SetTransientFor(parent);                  
        dialog.SetModal(true);                         
        dialog.SetDefaultSize(900, 650);    
        dialog.SetResizable(true);

        var box = Box.New(Orientation.Vertical, 12);
        box.SetMarginTop(16);
        box.SetMarginBottom(16);
        box.SetMarginStart(16);
        box.SetMarginEnd(16);

        var graphWidget = StarfishInterop.CreateDisplayOnlyGraphWidget(packageName, dependencyMap);
        graphWidget.SetVexpand(true);
        graphWidget.SetHexpand(true);

        var scrolledWindow = ScrolledWindow.New();
        scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
        scrolledWindow.SetVexpand(true);
        scrolledWindow.SetHexpand(true);
        scrolledWindow.AddCssClass("view");
        
        var boxWrapper = Box.New(Orientation.Vertical, 0);
        boxWrapper.Append(graphWidget);
        boxWrapper.MarginTop = 12;
        boxWrapper.MarginBottom = 12;
        boxWrapper.MarginStart = 12;
        boxWrapper.MarginEnd = 12;
        scrolledWindow.SetChild(boxWrapper);
        
        box.Append(scrolledWindow);

        var buttonBox = Box.New(Orientation.Horizontal, 8);
        buttonBox.SetHalign(Align.End);

        var closeButton = Button.NewWithLabel("Close");
        closeButton.OnClicked += (_, _) => dialog.Close();
        buttonBox.Append(closeButton);
        box.Append(buttonBox);

        dialog.SetChild(box);
       
        //Dont let the GC win...
        //Long story short with invoking this and the GC collecting something before it can and causes a crash.
        dialog.OnCloseRequest += (_, _) =>
        {
            graphWidget.Dispose();
            return false;
        };
        
        dialog.Present();
    }
}
