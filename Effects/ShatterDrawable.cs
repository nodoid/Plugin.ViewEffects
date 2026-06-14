using Microsoft.Maui.Graphics;

namespace Plugin.ViewEffects.Effects;

/// <summary>
/// Glass-shatter: the view tints blue and a web of cracks radiates from a <see cref="ShatterOrigin"/>
/// impact point, then it breaks into a spray of irregular angular shards that fly outward from that
/// point, spinning and falling under gravity while fading — like shattering glass.
/// </summary>
sealed class ShatterDrawable : EffectDrawable
{
    const int Sectors = 18;     // radial cracks
    const int Rings = 3;        // shard rings from impact outward
    const float CrackEnd = 0.22f;

    static readonly Color Glass = Color.FromRgb(0x9C, 0xD2, 0xF0);
    static readonly Color CrackLine = Color.FromRgba(1f, 1f, 1f, 0.85f);

    readonly ShatterOrigin _origin;

    public ShatterDrawable(IImage? image, Color baseColor, ShatterOrigin origin)
        : base(image, baseColor) => _origin = origin;

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float w = dirtyRect.Width, h = dirtyRect.Height;
        var p = (float)Progress;
        float crack = SmoothStep(0f, CrackEnd, p);
        float fly = SmoothStep(CrackEnd * 0.8f, 1f, p);

        // Impact point and the furthest distance from it to a corner (so shards cover the whole view).
        var (ix, iy) = ImpactPoint(w, h);
        float maxR = MaxCornerDistance(ix, iy, w, h) * 1.04f;
        float reach = MathF.Max(w, h);

        // Per-sector crack angles (jittered) and per-ring radii (jittered).
        Span<float> angles = stackalloc float[Sectors + 1];
        for (int s = 0; s < Sectors; s++)
        {
            float jitter = (Hash01(s * 13) - 0.5f) * (MathF.Tau / Sectors) * 0.6f;
            angles[s] = s * (MathF.Tau / Sectors) + jitter;
        }
        angles[Sectors] = angles[0] + MathF.Tau; // close the ring

        for (int s = 0; s < Sectors; s++)
        {
            float a0 = angles[s], a1 = angles[s + 1];
            for (int ring = 0; ring < Rings; ring++)
            {
                float rIn = RingRadius(ring, maxR, s);
                float rOut = RingRadius(ring + 1, maxR, s);
                DrawShard(canvas, w, h, ix, iy, reach, a0, a1, rIn, rOut, crack, fly, seed: s * Rings + ring);
            }
        }

        DrawCracks(canvas, ix, iy, angles, maxR, crack, fly);
    }

    void DrawShard(ICanvas canvas, float w, float h, float ix, float iy, float reach,
                   float a0, float a1, float rIn, float rOut, float crack, float fly, int seed)
    {
        // Shard polygon (a wedge between two cracks and two rings), centred on the impact point.
        var path = new PathF();
        path.MoveTo(ix + MathF.Cos(a0) * rIn, iy + MathF.Sin(a0) * rIn);
        path.LineTo(ix + MathF.Cos(a1) * rIn, iy + MathF.Sin(a1) * rIn);
        path.LineTo(ix + MathF.Cos(a1) * rOut, iy + MathF.Sin(a1) * rOut);
        path.LineTo(ix + MathF.Cos(a0) * rOut, iy + MathF.Sin(a0) * rOut);
        path.Close();

        // Fly outward from the impact along the shard's mean direction, with spin and gravity.
        float midA = (a0 + a1) * 0.5f;
        float midR = (rIn + rOut) * 0.5f;
        float shardCx = ix + MathF.Cos(midA) * midR;
        float shardCy = iy + MathF.Sin(midA) * midR;

        float speed = 0.55f + Hash01(seed * 7) * 0.8f;
        float dist = fly * speed * reach * 1.3f;
        float offX = MathF.Cos(midA) * dist;
        float offY = MathF.Sin(midA) * dist + fly * fly * h * 0.7f; // gravity
        float spin = (Hash01(seed * 5) - 0.5f) * 260f * fly;
        float alpha = 1f - SmoothStep(0.7f, 1f, fly);

        canvas.SaveState();
        canvas.Alpha = alpha;
        canvas.Translate(offX, offY);
        canvas.Rotate(spin, shardCx, shardCy);
        canvas.ClipPath(path, WindingMode.NonZero);

        DrawContent(canvas, 0, 0, w, h);

        // Glassy blue wash, deepening as the cracks set in.
        canvas.FillColor = Glass.WithAlpha(0.16f + crack * 0.4f);
        canvas.FillRectangle(0, 0, w, h);

        // Bright fractured edge that catches the light, fading as the shard tumbles away.
        canvas.StrokeColor = CrackLine.WithAlpha(0.5f * (1f - SmoothStep(0.6f, 1f, fly)));
        canvas.StrokeSize = 1f;
        canvas.DrawPath(path);

        canvas.RestoreState();
    }

    void DrawCracks(ICanvas canvas, float ix, float iy, Span<float> angles, float maxR, float crack, float fly)
    {
        float lineAlpha = crack * (1f - SmoothStep(0f, 0.5f, fly));
        if (lineAlpha <= 0.01f) return;

        canvas.SaveState();
        canvas.StrokeColor = CrackLine.WithAlpha(lineAlpha);
        canvas.StrokeSize = 1.4f;
        for (int s = 0; s < Sectors; s++)
            canvas.DrawLine(ix, iy, ix + MathF.Cos(angles[s]) * maxR, iy + MathF.Sin(angles[s]) * maxR);
        canvas.RestoreState();
    }

    float RingRadius(int ring, float maxR, int seed)
    {
        if (ring == 0) return 0f;
        if (ring >= Rings) return maxR;
        float baseFrac = ring / (float)Rings;
        float jitter = (Hash01(seed * 17 + ring * 3) - 0.5f) * 0.18f;
        return Math.Clamp(baseFrac + jitter, 0.08f, 0.95f) * maxR;
    }

    (float x, float y) ImpactPoint(float w, float h) => _origin switch
    {
        ShatterOrigin.TopLeft => (0f, 0f),
        ShatterOrigin.TopCentre => (w / 2f, 0f),
        ShatterOrigin.TopRight => (w, 0f),
        ShatterOrigin.Left => (0f, h / 2f),
        ShatterOrigin.Right => (w, h / 2f),
        ShatterOrigin.BottomLeft => (0f, h),
        ShatterOrigin.BottomCentre => (w / 2f, h),
        ShatterOrigin.BottomRight => (w, h),
        _ => (w / 2f, h / 2f), // Centre
    };

    static float MaxCornerDistance(float ix, float iy, float w, float h)
    {
        float d = 0f;
        foreach (var (cxn, cyn) in new[] { (0f, 0f), (w, 0f), (0f, h), (w, h) })
            d = MathF.Max(d, MathF.Sqrt((ix - cxn) * (ix - cxn) + (iy - cyn) * (iy - cyn)));
        return d;
    }
}
