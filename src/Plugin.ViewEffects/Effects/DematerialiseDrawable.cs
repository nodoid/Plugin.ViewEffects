using Microsoft.Maui.Graphics;

namespace Plugin.ViewEffects.Effects;

/// <summary>
/// TARDIS-style dematerialisation: the contents flicker translucently in and out, the strobing growing
/// faster and harder as the view fades, with a faint blue-white ghost pulsing in counterphase behind
/// it — until nothing is left.
/// </summary>
sealed class DematerialiseDrawable : EffectDrawable
{
    const float Cycles = 7f;     // base number of fade pulses across the timeline
    static readonly Color GhostTint = Color.FromRgb(0xCF, 0xE8, 0xFF);

    public DematerialiseDrawable(IImage? image, Color baseColor) : base(image, baseColor) { }

    public override void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float w = dirtyRect.Width, h = dirtyRect.Height;
        var p = (float)Progress;

        // Overall envelope: presence fades from 1 → 0, easing out so it lingers then vanishes.
        float presence = 1f - SmoothStep(0f, 1f, p);

        // The strobe accelerates as it dematerialises (phase grows with p²).
        float phase = (Cycles * (p + p * p)) * MathF.Tau;
        float wave = 0.5f + 0.5f * MathF.Cos(phase);   // 0 → 1
        // Harden the flicker over time so it snaps on/off near the end.
        float sharp = MathF.Pow(wave, 1f + p * 3f);

        // The solid view: pulsing translucency, never fully solid once it starts going.
        float bodyAlpha = presence * (0.15f + 0.85f * sharp);
        if (bodyAlpha > 0.01f)
        {
            canvas.SaveState();
            canvas.Alpha = bodyAlpha;
            DrawContent(canvas, 0, 0, w, h);
            canvas.RestoreState();
        }

        // The ghost: a slightly larger blue-white echo pulsing in counterphase (visible when the body dims).
        float ghostAlpha = presence * (1f - sharp) * 0.5f;
        if (ghostAlpha > 0.01f)
        {
            float grow = 1f + p * 0.06f + (1f - sharp) * 0.03f;
            float gw = w * grow, gh = h * grow;
            float gx = (w - gw) / 2f, gy = (h - gh) / 2f;

            canvas.SaveState();
            canvas.Alpha = ghostAlpha;
            DrawContent(canvas, gx, gy, gw, gh);
            // Wash it toward blue-white so the echo reads as the dematerialisation glow.
            canvas.FillColor = GhostTint.WithAlpha(0.6f);
            canvas.FillRectangle(gx, gy, gw, gh);
            canvas.RestoreState();
        }
    }
}
