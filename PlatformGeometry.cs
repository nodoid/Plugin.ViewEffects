using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Plugin.ViewEffects;

/// <summary>
/// Resolves a view's rectangle in its window's coordinate space from the underlying platform view —
/// the space a <see cref="Microsoft.Maui.WindowOverlay"/> draws in. This accounts for chrome (navigation
/// bars, safe areas) that the cross-platform layout tree doesn't expose, so an overlay drawn at these
/// coordinates lines up exactly with the live view.
/// </summary>
static class PlatformGeometry
{
    public static RectF? WindowBounds(View view)
    {
        if (view.Handler?.PlatformView is not { } platform)
            return null;

#if IOS || MACCATALYST
        if (platform is UIKit.UIView native && native.Window is { } window)
        {
            var r = native.ConvertRectToView(native.Bounds, window);
            return new RectF((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height);
        }
#elif ANDROID
        if (platform is Android.Views.View native)
        {
            var location = new int[2];
            native.GetLocationInWindow(location);
            float density = native.Resources?.DisplayMetrics?.Density ?? 1f;
            if (density <= 0) density = 1f;
            return new RectF(location[0] / density, location[1] / density,
                             native.Width / density, native.Height / density);
        }
#elif WINDOWS
        if (platform is Microsoft.UI.Xaml.FrameworkElement native)
        {
            var transform = native.TransformToVisual(null);
            var p = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            return new RectF((float)p.X, (float)p.Y, (float)native.ActualWidth, (float)native.ActualHeight);
        }
#endif
        return null;
    }
}
