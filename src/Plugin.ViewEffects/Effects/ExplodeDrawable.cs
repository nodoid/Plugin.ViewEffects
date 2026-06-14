using Microsoft.Maui.Graphics;

namespace Plugin.ViewEffects.Effects;

/// <summary>
/// The explosion burst: the snapshot briefly swells and fades while a spray of coloured particles
/// flies outward from the centre and falls away. (The slow-to-fast vibration that precedes the burst
/// is applied to the live view by the runner, before this drawable takes over.)
/// </summary>
sealed class ExplodeDrawable : EffectDrawable
{
    const int Particles = 70;

    static readonly Color[] Palette =
    {
        Colors.Red, Colors.OrangeRed, Colors.Orange, Colors.Gold, Colors.Yellow,
        Colors.LimeGreen, Colors.Green, Colors.Cyan, Colors.DeepSkyBlue, Colors.Blue,
        Colors.BlueViolet, Colors.Magenta, Colors.HotPink, Colors.White,
    };

    public ExplodeDrawable(IImage? image, Color baseColor) : base(image, baseColor) { }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float w = dirtyRect.Width, h = dirtyRect.Height;
        var p = (float)Progress;
        float cx = w / 2f, cy = h / 2f;
        float maxR = MathF.Sqrt(w * w + h * h) * 0.65f;

        // The original content swells slightly and fades out at the very start of the burst.
        float baseAlpha = 1f - SmoothStep(0f, 0.3f, p);
        if (baseAlpha > 0f)
        {
            float scale = 1f + p * 0.25f;
            float sw = w * scale, sh = h * scale;
            canvas.SaveState();
            canvas.Alpha = baseAlpha;
            DrawContent(canvas, cx - sw / 2f, cy - sh / 2f, sw, sh);
            canvas.RestoreState();
        }

        float particleAlpha = 1f - SmoothStep(0.7f, 1f, p);
        if (particleAlpha <= 0f) return;

        canvas.SaveState();
        canvas.Alpha = particleAlpha;
        for (int i = 0; i < Particles; i++)
        {
            float angle = Hash01(i) * MathF.Tau;
            float speed = 0.35f + Hash01(i * 5) * 0.65f;
            float dist = p * speed * maxR;
            float px = cx + MathF.Cos(angle) * dist;
            float py = cy + MathF.Sin(angle) * dist + p * p * h * 0.5f; // a little gravity
            float radius = (3f + Hash01(i * 11) * 7f) * (1f - p * 0.35f);

            canvas.FillColor = Palette[i % Palette.Length];
            FillDot(canvas, px, py, MathF.Max(0.5f, radius));
        }
        canvas.RestoreState();
    }
}
