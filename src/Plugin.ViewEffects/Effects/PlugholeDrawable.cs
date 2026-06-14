using Microsoft.Maui.Graphics;

namespace Plugin.ViewEffects.Effects;

/// <summary>
/// Plughole: the view is sliced into concentric rings that swirl around the centre — inner rings
/// spinning faster — while being pulled inward and shrinking to nothing, like water spiralling down a
/// drain.
/// </summary>
sealed class PlugholeDrawable : EffectDrawable
{
    const int Rings = 24;

    public PlugholeDrawable(IImage? image, Color baseColor) : base(image, baseColor) { }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float w = dirtyRect.Width, h = dirtyRect.Height;
        var p = (float)Progress;
        float cx = w / 2f, cy = h / 2f;
        float maxR = MathF.Sqrt(cx * cx + cy * cy) * 1.02f;

        // Draw from the outside in, so inner (faster, smaller) rings sit on top.
        for (int i = Rings - 1; i >= 0; i--)
        {
            float rOuter = (i + 1) / (float)Rings * maxR;
            float rInner = i / (float)Rings * maxR;
            float mid = (i + 0.5f) / Rings;        // 0 at centre, 1 at the rim
            float t = 1f - mid;                     // 1 at centre, 0 at the rim — inner swirls more

            // Spiral: inner rings rotate through several turns; everyone accelerates over time.
            float angle = p * (140f + 620f * t);
            // Pull inward: each ring scales toward the centre, inner rings collapsing first.
            float scale = MathF.Max(0f, 1f - p * (0.45f + 0.55f * t));
            float alpha = 1f - SmoothStep(0.8f, 1f, p);

            canvas.SaveState();
            canvas.Alpha = alpha;

            // Rotate + scale about the centre, then clip to this ring of the original image.
            canvas.Translate(cx, cy);
            canvas.Rotate(angle);
            canvas.Scale(scale, scale);
            canvas.Translate(-cx, -cy);
            canvas.ClipPath(Ring(cx, cy, rInner, rOuter), WindingMode.EvenOdd);

            DrawContent(canvas, 0, 0, w, h);
            canvas.RestoreState();
        }
    }

    static PathF Ring(float cx, float cy, float rInner, float rOuter)
    {
        var path = new PathF();
        path.AppendCircle(cx, cy, rOuter);
        if (rInner > 0.5f)
            path.AppendCircle(cx, cy, rInner); // even-odd winding cuts the hole
        return path;
    }
}
