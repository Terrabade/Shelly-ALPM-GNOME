using Gtk;

namespace Shelly.Gtk.Helpers;

public static class AnimationHelper
{
    public static void FadeOutAndLift(
        Widget widget,
        Action completed,
        double durationMs = 300,
        int liftPixels = 24)
    {
        long start = 0;
        var initialMargin = widget.MarginTop;

        widget.AddTickCallback((w, frameClock) =>
        {
            if (start == 0)
                start = frameClock.GetFrameTime();

            var elapsed = (frameClock.GetFrameTime() - start) / 1000.0;
            var t = Math.Clamp(elapsed / durationMs, 0.0, 1.0);

            t = 1 - Math.Pow(1 - t, 3);

            w.Opacity = 1.0 - t;
            w.MarginTop = initialMargin - (int)(liftPixels * t);

            if (!(t >= 1.0)) return true;
            completed?.Invoke();
            return false;
        });
    }
}