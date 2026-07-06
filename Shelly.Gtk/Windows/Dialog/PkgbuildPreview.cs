using Gtk;
using Pango;
using Shelly.Gtk.Services;
using Shelly.Gtk.UiModels;
using WrapMode = Gtk.WrapMode;
 
namespace Shelly.Gtk.Windows.Dialog;
 
public static class PkgbuildPreview
{
    public static void ShowPackageBuildPreview(Window? parent, PackageBuildEventArgs? e)
    {
        if (e is null) return;
        
        var tcs = new TaskCompletionSource<bool>();
 
        var dialog = Window.New();
        
        dialog.SetTitle(e.Title);                      
        dialog.SetTransientFor(parent);                  
        dialog.SetModal(true);                         
        dialog.SetDefaultSize(900, 650);    
        dialog.SetResizable(true);
 
        var outer = Box.New(Orientation.Vertical, 12);
 
        var notebook = Notebook.New();
        notebook.SetVexpand(true);
        notebook.SetHexpand(true);
        notebook.SetScrollable(true);
 
        if (!string.IsNullOrEmpty(e.PkgBuild))
        {
            var textView = TextView.New();
            textView.WrapMode = WrapMode.WordChar;
            textView.Editable = false;          
            textView.Monospace = true;        
            textView.CursorVisible = false;
            textView.LeftMargin = 12;
            textView.RightMargin = 12;
            textView.TopMargin = 12;
            textView.BottomMargin = 12;
            textView.GetBuffer().SetText(e.PkgBuild, -1);
 
            var scrolledWindow = ScrolledWindow.New();
            scrolledWindow.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            scrolledWindow.SetVexpand(true);
            scrolledWindow.SetHexpand(true);
            scrolledWindow.AddCssClass("view"); 
            scrolledWindow.SetChild(textView);
 
            notebook.AppendPage(scrolledWindow, Label.New("PKGBUILD"));
        }
        
        foreach (var (name, content) in e.SourceFiles)
        {
            if (string.IsNullOrEmpty(content)) continue;
 
            var sourceView = TextView.New();
            sourceView.SetWrapMode(WrapMode.WordChar);
            sourceView.Editable = false;
            sourceView.Monospace = true;
            sourceView.CursorVisible = false;
            sourceView.LeftMargin = 12;
            sourceView.RightMargin = 12;
            sourceView.TopMargin = 12;
            sourceView.BottomMargin = 12;
            sourceView.GetBuffer().SetText(content, -1);
 
            var sourceScroll = ScrolledWindow.New();
            sourceScroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            sourceScroll.SetVexpand(true);
            sourceScroll.SetHexpand(true);
            sourceScroll.AddCssClass("view");
            sourceScroll.SetChild(sourceView);
 
            notebook.AppendPage(sourceScroll, Label.New(name));
        }
 
        outer.Append(notebook);
 
        dialog.SetChild(outer);
 
        dialog.OnCloseRequest += (_, _) =>
        {
            tcs.TrySetResult(false);
            dialog.Dispose();
            return false;
        };
 
        dialog.Present();
    }
}