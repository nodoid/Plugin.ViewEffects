using Microsoft.Maui.Graphics;

namespace Plugin.ViewEffects.Effects;

/// <summary>
/// Tints the contents from light blue to dark blue (freezing over), then shatters the frozen view
/// into a grid of ice shards that scatter outward and fall under gravity while fading.
/// </summary>
sealed class FreezeDrawable : EffectDrawable
{
    const int Columns = 6;
    const int Rows = 8;
    const float FreezeEnd = 0.4f;           // fraction of the timeline spent freezing before shattering

    static readonly Color LightBlue = Color.FromRgb(0xAD, 0xD8, 0xE6);
    static readonly Color DarkBlue = Color.FromRgb(0x0D, 0x2B, 0x6B);

    public FreezeDrawable(IImage? image, Color baseColor) : base(image, baseColor) { }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float w = dirtyRect.Width, h = dirtyRect.Height;
        var p = (float)Progress;
        var freeze = SmoothStep(0f, FreezeEnd, p);
        var shatter = SmoothStep(FreezeEnd, 1f, p);
        var iceTint = Lerp(LightBlue, DarkBlue, freeze);

        float cellW = w / Columns, cellH = h / Rows;
        float centreX = w / 2f, centreY = h / 2f;
        float spread = Math.Max(w, h) * 0.9f;

        for (int row = 0; row < Rows; row++)
        for (int col = 0; col < Columns; col++)
        {
            int seed = row * Columns + col;
            float cellCx = (col + 0.5f) * cellW;
            float cellCy = (row + 0.5f) * cellH;

            // Direction away from centre, with per-shard jitter so the scatter looks natural.
            float dirX = (cellCx - centreX) / Math.Max(1f, centreX) + (Hash01(seed) - 0.5f);
            float dirY = (cellCy - centreY) / Math.Max(1f, centreY) + (Hash01(seed * 3) - 0.5f);

            float offX = dirX * shatter * spread * 0.6f;
            float offY = dirY * shatter * spread * 0.4f + shatter * shatter * h * 1.1f; // gravity
            float spin = (Hash01(seed * 7) - 0.5f) * 220f * shatter;
            float alpha = 1f - SmoothStep(0.65f, 1f, shatter);

            canvas.SaveState();
            canvas.Alpha = alpha;
            canvas.Translate(offX, offY);
            canvas.Rotate(spin, cellCx, cellCy);
            canvas.ClipRectangle(col * cellW, row * cellH, cellW, cellH);
            DrawContent(canvas, 0, 0, w, h);
            canvas.FillColor = iceTint.WithAlpha(0.45f);
            canvas.FillRectangle(0, 0, w, h);
            canvas.RestoreState();
        }
    }
}
