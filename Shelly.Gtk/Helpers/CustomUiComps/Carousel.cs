using GObject;
using Gtk;

namespace Shelly.Gtk.Helpers.CustomUiComps;

[Subclass<Box>]
public partial class Carousel 
{
    private Stack _stack = null!;
    private Button _prevButton = null!;
    private Button _nextButton = null!;

    public event EventHandler? PageChanged;

    public static Carousel New()
    {
        return NewWithProperties([]);
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        SetSpacing(8);

        _stack = Stack.New();
        _stack.TransitionType = StackTransitionType.SlideLeftRight;
        _stack.Hexpand = true;
        _stack.Vexpand = true;

        _prevButton = Button.NewFromIconName("go-previous-symbolic");
        _prevButton.Valign = Align.Center;
        _prevButton.OnClicked += (_, _) => Previous();

        _nextButton = Button.NewFromIconName("go-next-symbolic");
        _nextButton.Valign = Align.Center;
        _nextButton.OnClicked += (_, _) => Next();

        Append(_prevButton);
        Append(_stack);
        Append(_nextButton);
        
        _stack.OnNotify += (s, e) =>
        {
            if (e.Pspec.GetName() != "visible-child") return;
            UpdateButtons();
            PageChanged?.Invoke(this, EventArgs.Empty);
        };

        UpdateButtons();
    }

    public void AddWidget(Widget widget)
    {
        _stack.AddChild(widget);
        if (_stack.GetVisibleChild() == null)
            _stack.SetVisibleChild(widget);
        UpdateButtons();
    }

    public void RemoveAll()
    {
        while (_stack.GetFirstChild() is { } child)
            _stack.Remove(child);
        UpdateButtons();
    }

    private void Next()
    {
        var current = _stack.GetVisibleChild();
        var next = current?.GetNextSibling();
        if (next != null)
            _stack.SetVisibleChild(next);
    }

    private void Previous()
    {
        var current = _stack.GetVisibleChild();
        var prev = current?.GetPrevSibling();
        if (prev != null)
            _stack.SetVisibleChild(prev);
    }

    public Widget? GetVisibleChild() => _stack.GetVisibleChild();

    public List<Widget> GetChildren()
    {
        var list = new List<Widget>();
        var child = _stack.GetFirstChild();
        while (child != null)
        {
            list.Add(child);
            child = child.GetNextSibling();
        }
        return list;
    }

    private void UpdateButtons()
    {
        var current = _stack.GetVisibleChild();
        _prevButton.SetSensitive(current?.GetPrevSibling() != null);
        _nextButton.SetSensitive(current?.GetNextSibling() != null);
    }
}
