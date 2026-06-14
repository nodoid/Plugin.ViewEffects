using Microsoft.Maui.Graphics;

namespace Plugin.ViewEffects.Effects;

/// <summary>
/// Base class for the removal-effect drawables. Each effect renders a single frame for a given
/// <see cref="Progress"/> value (0 → 1); the animation runner tweens <see cref="Progress"/> and
/// invalidates the host <c>GraphicsView</c> each frame.
/// </summary>
abstract class EffectDrawable : IDrawable
{
    /// <summary>The captured snapshot of the view, or <c>null</c> if capture was unavailable.</summary>
    protected IImage? Image { get; }

    /// <summary>Fallback fill used when no snapshot is available.</summary>
    protected Color BaseColor { get; }

    /// <summary>Animation progress, 0 (start) → 1 (fully removed).</summary>
    public double Progress { get; set; }

    protected EffectDrawable(IImage? image, Color baseColor)
    {
        Image = image;
        BaseColor = baseColor;
    }

    public abstract void Draw(ICanvas canvas, RectF dirtyRect);

    /// <summary>Draws the captured snapshot (or the fallback colour) into the given rectangle.</summary>
    protected void DrawContent(ICanvas canvas, float x, float y, float w, float h)
    {
        if (Image is not null)
        {
            canvas.DrawImage(Image, x, y, w, h);
        }
        else
        {
            canvas.FillColor = BaseColor;
            canvas.FillRectangle(x, y, w, h);
        }
    }

    /// <summary>Draws a filled circle by way of a fully-rounded rectangle (works on every backend).</summary>
    protected static void FillDot(ICanvas canvas, float cx, float cy, float radius)
        => canvas.FillRoundedRectangle(cx - radius, cy - radius, radius * 2, radius * 2, radius);

    /// <summary>Deterministic pseudo-random value in [0, 1) for a given seed (no global RNG state).</summary>
    public static float Hash01(int seed)
    {
        unchecked
        {
            uint x = (uint)seed * 2654435761u + 1013904223u;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return (x & 0xFFFFFF) / (float)0x1000000;
        }
    }

    /// <summary>Smooth 0→1 ramp over [edge0, edge1], clamped outside.</summary>
    protected static float SmoothStep(float edge0, float edge1, float v)
    {
        if (edge1 <= edge0) return v < edge0 ? 0f : 1f;
        var t = Math.Clamp((v - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>Linear interpolation between two colours.</summary>
    protected static Color Lerp(Color a, Color b, float t)
        => Color.FromRgba(
            a.Red + (b.Red - a.Red) * t,
            a.Green + (b.Green - a.Green) * t,
            a.Blue + (b.Blue - a.Blue) * t,
            a.Alpha + (b.Alpha - a.Alpha) * t);
}
