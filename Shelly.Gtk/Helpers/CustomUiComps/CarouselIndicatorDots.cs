using GObject;
using Gtk;

namespace Shelly.Gtk.Helpers.CustomUiComps;

[Subclass<Box>]
public partial class CarouselIndicatorDots
{
    private Carousel? _carousel;

    public static CarouselIndicatorDots New(Carousel carousel)
    {
        var dots = NewWithProperties([]);
        dots.Setup(carousel);
        return dots;
    }

    partial void Initialize()
    {
        SetOrientation(Orientation.Horizontal);
        SetSpacing(6);
        
        Halign = Align.Center;
        MarginTop = 4;
        MarginBottom = 4;
    }

    private void Setup(Carousel carousel)
    {
        _carousel = carousel;
        _carousel.PageChanged += (_, _) => Update();
        Update();
    }

    public void Update()
    {
        if (_carousel == null) return;
        
        while (GetFirstChild() is { } child)
            Remove(child);

        var children = _carousel.GetChildren();
        var visibleChild = _carousel.GetVisibleChild();

        if (children.Count <= 1) return;

        foreach (var child in children)
        {
            var dot = Image.NewFromIconName("media-record-symbolic");
            
            dot.AddCssClass(child == visibleChild ? "carousel-dot-active" : "carousel-dot-inactive");
            
            if (child != visibleChild)
                dot.Opacity = 0.3;
            
            Append(dot);
        }
    }
}
