using Microsoft.Maui.Graphics;

namespace Plugin.ViewEffects.Effects;

/// <summary>
/// Unblur: renders the snapshot from fully blurred to sharp as <see cref="EffectDrawable.Progress"/>
/// goes 0 → 1. Blur is approximated by progressively-downsized copies of the snapshot drawn back at full
/// size (the platform's upscaling softens them); adjacent levels are cross-dissolved for a smooth ramp.
/// </summary>
sealed class UnblurDrawable : EffectDrawable
{
    readonly IImage[] _levels; // ordered blurriest → sharpest (last is the original)
    readonly int _steps;       // 0 = smooth cross-dissolve; >0 = snap through this many discrete steps

    public UnblurDrawable(IImage[] levels, Color baseColor, int steps = 0) : base(null, baseColor)
    {
        _levels = levels;
        _steps = steps;
    }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float w = dirtyRect.Width, h = dirtyRect.Height;
        int n = _levels.Length;
        if (n == 0)
        {
            canvas.FillColor = BaseColor;
            canvas.FillRectangle(0, 0, w, h);
            return;
        }

        float p = (float)Math.Clamp(Progress, 0, 1);

        if (_steps > 0)
        {
            // Discrete stepping: hold a level, then snap to the next sharper one. Ends sharp at p = 1.
            float quantised = p >= 1f ? 1f : MathF.Floor(p * _steps) / _steps;
            int idx = Math.Clamp((int)MathF.Round(quantised * (n - 1)), 0, n - 1);
            canvas.DrawImage(_levels[idx], 0, 0, w, h);
            return;
        }

        // Smooth: cross-dissolve between adjacent levels.
        float fl = p * (n - 1);
        int lo = Math.Clamp((int)fl, 0, n - 1);
        int hi = Math.Min(lo + 1, n - 1);
        float frac = fl - lo;

        canvas.DrawImage(_levels[lo], 0, 0, w, h);
        if (hi != lo && frac > 0.001f)
        {
            canvas.Alpha = frac;
            canvas.DrawImage(_levels[hi], 0, 0, w, h);
            canvas.Alpha = 1f;
        }
    }
}
