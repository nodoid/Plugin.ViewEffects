using Microsoft.Maui.Graphics;

namespace Plugin.ViewEffects.Effects;

/// <summary>
/// Melts the view: it is sliced into vertical strips that drip downward at varying speeds, stretching
/// as they go, past the bottom of the view, then fade out.
/// </summary>
sealed class MeltDrawable : EffectDrawable
{
    const int Strips = 14;

    public MeltDrawable(IImage? image, Color baseColor) : base(image, baseColor) { }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float w = dirtyRect.Width, h = dirtyRect.Height;
        var p = (float)Progress;
        float stripW = w / Strips;

        for (int i = 0; i < Strips; i++)
        {
            // Per-strip speed so the melt front is uneven, like real dripping.
            float speed = 0.55f + Hash01(i) * 0.9f;
            float drop = p * h * speed * 1.35f;
            float stretch = 1f + p * speed * 0.8f;
            float alpha = 1f - SmoothStep(0.55f, 1f, p);

            canvas.SaveState();
            canvas.Alpha = alpha;
            // Clip to this strip's column (tall enough to keep the stretched/dropped content visible).
            canvas.ClipRectangle(i * stripW, 0, stripW + 0.5f, h * (1f + stretch) + drop);
            canvas.Translate(0, drop);
            canvas.Scale(1f, stretch);   // anchored at the strip's top, stretches downward
            DrawContent(canvas, 0, 0, w, h);
            canvas.RestoreState();
        }
    }
}
