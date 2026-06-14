using System.Threading;
using Microsoft.Maui; // ViewExtensions.CaptureAsync, WindowOverlay, IWindow
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform; // PlatformImage
using Plugin.ViewEffects.Effects;

namespace Plugin.ViewEffects;

/// <summary>
/// Drives the removal/reveal animations. Contained effects (freeze, melt, dematerialise, materialise,
/// plughole) snapshot the view and animate a transient <see cref="GraphicsView"/> in its layout slot.
/// Spanning effects (shatter, explode) instead draw on a full-window <see cref="WindowOverlay"/> so their
/// shards/particles can fly across the whole screen, hiding the live view until the animation completes.
/// </summary>
static class RemovalEffects
{
    /// <summary>The snapshot-drawable effects.</summary>
    enum EffectKind { Freeze, Melt, Explode, Shatter, Dematerialise, Plughole }

    /// <summary>Effects whose particles travel beyond the view's own bounds, so they need a full-window overlay.</summary>
    static bool IsSpanning(EffectKind kind) => kind is EffectKind.Shatter or EffectKind.Explode;

    /// <summary>Identifies the parent container slot a view occupies, so it can be restored later.</summary>
    internal readonly record struct LayoutSlot(Element Parent, int Index, bool IsContentHost)
    {
        public static LayoutSlot? Capture(View view) => view.Parent switch
        {
            // ScrollView and ContentView derive from Layout, so they must be matched first.
            ScrollView sv when ReferenceEquals(sv.Content, view) => new LayoutSlot(sv, -1, true),
            ContentView cv when ReferenceEquals(cv.Content, view) => new LayoutSlot(cv, -1, true),
            Layout layout => ((IList<IView>)layout).IndexOf(view) is var i and >= 0
                ? new LayoutSlot(layout, i, false)
                : null,
            _ => null,
        };

        /// <summary>Removes a child from this slot's container.</summary>
        public void Detach(View child)
        {
            switch (Parent)
            {
                case ScrollView sv when ReferenceEquals(sv.Content, child): sv.Content = null; break;
                case ContentView cv when ReferenceEquals(cv.Content, child): cv.Content = null; break;
                case Layout layout: ((IList<IView>)layout).Remove(child); break;
            }
        }

        /// <summary>Inserts a child into this slot at its original index (clamped) or as the content.</summary>
        public void Attach(View child)
        {
            switch (Parent)
            {
                case ScrollView sv: sv.Content = child; break;
                case ContentView cv: cv.Content = child; break;
                case Layout layout:
                    var children = (IList<IView>)layout;
                    children.Insert(Math.Clamp(Index, 0, children.Count), child);
                    break;
            }
        }
    }

    /// <summary>
    /// Tracks an in-flight effect so it can be cancelled (e.g. if the view is restored mid-animation).
    /// <see cref="Restore"/> fully undoes the effect, leaving the view visible in place.
    /// </summary>
    sealed class ActiveEffect
    {
        public required CancellationTokenSource Cts { get; init; }
        public required Action Restore { get; init; }
    }

    static readonly BindableProperty ActiveEffectProperty = BindableProperty.CreateAttached(
        "ActiveEffect", typeof(ActiveEffect), typeof(RemovalEffects), null);

    static int _animationCounter;

    /// <summary>True while an effect is currently playing on the view.</summary>
    internal static bool IsAnimating(BindableObject view) => view.GetValue(ActiveEffectProperty) is ActiveEffect;

    /// <summary>
    /// Cancels and fully undoes any in-flight effect, leaving the view visible in place. Returns
    /// <c>true</c> if an effect was actually torn down.
    /// </summary>
    internal static bool Cancel(BindableObject view)
    {
        if (view.GetValue(ActiveEffectProperty) is not ActiveEffect active)
            return false;

        view.SetValue(ActiveEffectProperty, null);
        active.Cts.Cancel();
        active.Restore();
        return true;
    }

    /// <summary>
    /// Plays <paramref name="animation"/> over <paramref name="view"/> and then removes it from
    /// <paramref name="slot"/>. Safe to fire-and-forget.
    /// </summary>
    internal static Task RunAsync(View view, LayoutSlot slot, RemovalAnimation animation,
                                  ShatterOrigin origin, TennisSide tennisSide, double seconds)
    {
        switch (animation)
        {
            case RemovalAnimation.None:
                slot.Detach(view);
                return Task.CompletedTask;
            case RemovalAnimation.TennisDisappear:
                return RunTennisAsync(view, slot, tennisSide, disappear: true, seconds);
            default:
                return RunCoreAsync(view, slot, ToKind(animation), origin, seconds);
        }
    }

    static Task RunCoreAsync(View view, LayoutSlot slot, EffectKind kind, ShatterOrigin origin, double seconds)
    {
        if (IsAnimating(view))
            return Task.CompletedTask;

        if (seconds <= 0)
            seconds = ViewEffects.ConfiguredDefaultSeconds;

        // Spanning effects use a full-window overlay when a window is available; otherwise they fall
        // back to the contained, in-slot path (clipped to the view's bounds, but still correct).
        return IsSpanning(kind) && view.Window is not null
            ? RunSpanningAsync(view, slot, kind, origin, seconds)
            : RunInSlotAsync(view, slot, kind, origin, seconds);
    }

    // ── Contained, in-slot path ────────────────────────────────────────────────────────────────────
    static async Task RunInSlotAsync(View view, LayoutSlot slot, EffectKind kind,
                                     ShatterOrigin origin, double seconds)
    {
        var cts = new CancellationTokenSource();
        GraphicsView? overlay = null;
        try
        {
            var image = await TryCaptureAsync(view);
            var (w, h) = MeasuredSize(view);

            var drawable = CreateDrawable(kind, origin, image, view.BackgroundColor ?? Colors.LightGray);
            overlay = new GraphicsView
            {
                Drawable = drawable,
                WidthRequest = w,
                HeightRequest = h,
                InputTransparent = true,
                BackgroundColor = Colors.Transparent,
            };
            CopyLayoutAttributes(view, overlay);

            var gv = overlay;
            view.SetValue(ActiveEffectProperty, new ActiveEffect
            {
                Cts = cts,
                // Undo: drop the overlay and put the live view back in its slot.
                Restore = () => { slot.Detach(gv); slot.Attach(view); },
            });

            // Swap the live view out for the overlay, in the same slot.
            slot.Detach(view);
            slot.Attach(overlay);

            await AnimateAsync(overlay, overlay.Invalidate, drawable, seconds, EasingFor(kind), cts.Token);
        }
        catch { /* never leave the view stuck — fall through to cleanup */ }
        finally
        {
            // Completed normally (not cancelled by a restore): drop the overlay, leaving the slot empty.
            if (!cts.IsCancellationRequested && view.GetValue(ActiveEffectProperty) is ActiveEffect)
            {
                if (overlay is not null) slot.Detach(overlay);
                view.SetValue(ActiveEffectProperty, null);
            }
            cts.Dispose();
        }
    }

    // ── Full-window, spanning path (shatter / explode) ─────────────────────────────────────────────
    static async Task RunSpanningAsync(View view, LayoutSlot slot, EffectKind kind,
                                       ShatterOrigin origin, double seconds)
    {
        var cts = new CancellationTokenSource();
        var window = view.Window!;
        double originalOpacity = view.Opacity;
        WindowOverlay? overlay = null;

        // Restore: pull the overlay and make the live view visible again, right where it is.
        void RestoreView()
        {
            if (overlay is not null) { try { window.RemoveOverlay(overlay); } catch { } }
            view.Opacity = originalOpacity;
        }

        view.SetValue(ActiveEffectProperty, new ActiveEffect { Cts = cts, Restore = RestoreView });

        try
        {
            double animSeconds = seconds;

            // Explode vibrates the live view (slow → fast) before the burst.
            if (kind == EffectKind.Explode)
            {
                double vibrate = Math.Min(1.0, seconds * 0.4);
                animSeconds = Math.Max(0.2, seconds - vibrate);
                await VibrateAsync(view, vibrate, cts.Token);
                if (cts.IsCancellationRequested) return;
            }

            var image = await TryCaptureAsync(view);
            var (w, h) = MeasuredSize(view);
            var drawable = CreateDrawable(kind, origin, image, view.BackgroundColor ?? Colors.LightGray);

            overlay = new WindowOverlay(window);
            overlay.AddWindowElement(new EffectOverlayElement(drawable, () => WindowRect(view, w, h)));
            window.AddOverlay(overlay);
            view.Opacity = 0; // hide the live view but keep its layout slot (no reflow)

            await AnimateAsync(view, overlay.Invalidate, drawable, animSeconds, EasingFor(kind), cts.Token);
        }
        catch { /* fall through to cleanup */ }
        finally
        {
            // Completed normally: pull the overlay, then actually remove the view from its slot.
            if (!cts.IsCancellationRequested && view.GetValue(ActiveEffectProperty) is ActiveEffect)
            {
                if (overlay is not null) { try { window.RemoveOverlay(overlay); } catch { } }
                slot.Detach(view);
                view.Opacity = originalOpacity; // so a later re-add shows it
                view.SetValue(ActiveEffectProperty, null);
            }
            cts.Dispose();
        }
    }

    /// <summary>A window-overlay element that draws an effect drawable at the view's window-relative rect.</summary>
    sealed class EffectOverlayElement : IWindowOverlayElement
    {
        readonly EffectDrawable _drawable;
        readonly Func<RectF> _rect;

        public EffectOverlayElement(EffectDrawable drawable, Func<RectF> rect)
        {
            _drawable = drawable;
            _rect = rect;
        }

        public bool Contains(Point point) => false; // never intercept touches

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var r = _rect();
            canvas.SaveState();
            canvas.Translate(r.X, r.Y);
            _drawable.Draw(canvas, new RectF(0, 0, r.Width, r.Height));
            canvas.RestoreState();
        }
    }

    /// <summary>
    /// The view's rectangle in the window-overlay's coordinate space. Uses the platform view's true
    /// window position (so it stays aligned including any navigation bar / safe-area offset); falls back
    /// to summing layout offsets up the parent chain if the platform view isn't available.
    /// </summary>
    static RectF WindowRect(View view, float w, float h)
    {
        if (PlatformGeometry.WindowBounds(view) is { } bounds && bounds.Width > 0 && bounds.Height > 0)
            return bounds;

        double x = 0, y = 0;
        for (Element? e = view; e is VisualElement ve; e = ve.Parent)
        {
            x += ve.X;
            y += ve.Y;
        }
        return new RectF((float)x, (float)y, w, h);
    }

    static EffectKind ToKind(RemovalAnimation animation) => animation switch
    {
        RemovalAnimation.Freeze => EffectKind.Freeze,
        RemovalAnimation.Melt => EffectKind.Melt,
        RemovalAnimation.Explode => EffectKind.Explode,
        RemovalAnimation.Dematerialise => EffectKind.Dematerialise,
        RemovalAnimation.Plughole => EffectKind.Plughole,
        _ => EffectKind.Shatter,
    };

    static EffectDrawable CreateDrawable(EffectKind kind, ShatterOrigin origin,
                                         IImage? image, Color baseColor) => kind switch
    {
        EffectKind.Freeze => new FreezeDrawable(image, baseColor),
        EffectKind.Melt => new MeltDrawable(image, baseColor),
        EffectKind.Explode => new ExplodeDrawable(image, baseColor),
        EffectKind.Dematerialise => new DematerialiseDrawable(image, baseColor),
        EffectKind.Plughole => new PlugholeDrawable(image, baseColor),
        _ => new ShatterDrawable(image, baseColor, origin),
    };

    static Easing EasingFor(EffectKind kind) => kind switch
    {
        EffectKind.Melt => Easing.CubicIn,
        EffectKind.Explode => Easing.CubicOut,
        EffectKind.Freeze => Easing.CubicIn,
        EffectKind.Plughole => Easing.CubicIn,
        EffectKind.Dematerialise => Easing.Linear,
        _ => Easing.CubicIn,
    };

    static Task AnimateAsync(IAnimatable owner, Action invalidate, EffectDrawable drawable,
                             double seconds, Easing easing, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource();
        var name = "vr-effect-" + Interlocked.Increment(ref _animationCounter);
        var length = (uint)Math.Max(1, seconds * 1000);

        var animation = new Animation(p =>
        {
            drawable.Progress = p;
            invalidate();
        }, 0, 1);

        animation.Commit(owner, name, 16, length, easing, (_, _) => tcs.TrySetResult(), null);

        if (ct.CanBeCanceled)
            ct.Register(() =>
            {
                owner.AbortAnimation(name);
                tcs.TrySetResult();
            });

        return tcs.Task;
    }

    /// <summary>Runs a 0→1 tween on <paramref name="owner"/>, invoking <paramref name="step"/> each frame.</summary>
    static Task AnimateRawAsync(IAnimatable owner, double seconds, Easing easing,
                                CancellationToken ct, Action<double> step)
    {
        var tcs = new TaskCompletionSource();
        var name = "vr-anim-" + Interlocked.Increment(ref _animationCounter);
        var length = (uint)Math.Max(1, seconds * 1000);

        var animation = new Animation(step, 0, 1);
        animation.Commit(owner, name, 16, length, easing, (_, _) => tcs.TrySetResult(), null);

        if (ct.CanBeCanceled)
            ct.Register(() =>
            {
                owner.AbortAnimation(name);
                tcs.TrySetResult();
            });

        return tcs.Task;
    }

    // ── Materialise (direct-view reveal: starts blank, flickers the live view in at its final size) ──
    internal static async Task RunMaterialiseAsync(View view, double seconds)
    {
        if (IsAnimating(view))
            return;
        if (seconds <= 0)
            seconds = ViewEffects.ConfiguredDefaultSeconds;

        var cts = new CancellationTokenSource();
        double originalOpacity = view.Opacity <= 0 ? 1 : view.Opacity;

        view.SetValue(ActiveEffectProperty, new ActiveEffect
        {
            Cts = cts,
            Restore = () => view.Opacity = originalOpacity,
        });

        try
        {
            view.Opacity = 0; // start blank
            await AnimateRawAsync(view, seconds, Easing.Linear, cts.Token,
                p => view.Opacity = originalOpacity * MaterialiseOpacity((float)p));
        }
        catch { /* fall through */ }
        finally
        {
            if (!cts.IsCancellationRequested && view.GetValue(ActiveEffectProperty) is ActiveEffect)
            {
                view.Opacity = originalOpacity; // fully present
                view.SetValue(ActiveEffectProperty, null);
            }
            cts.Dispose();
        }
    }

    // ── Unblur (reveal: starts fully blurred, sharpens to the real view; optional tap-to-skip) ───────
    internal static async Task RunUnblurAsync(View view, LayoutSlot slot, double seconds, TapEnable tap, double timestep)
    {
        if (IsAnimating(view))
            return;
        if (seconds <= 0)
            seconds = ViewEffects.ConfiguredUnblurSeconds;

        // timestep > 0 quantises the unblur into discrete steps held for ~timestep seconds each.
        int steps = timestep > 0 ? Math.Max(1, (int)Math.Round(seconds / timestep)) : 0;

        var cts = new CancellationTokenSource();
        double originalOpacity = view.Opacity;
        GraphicsView? overlay = null;
        IImage[]? levels = null;
        IImage? image = null;

        try
        {
            image = await TryCaptureAsync(view);
            if (image is null)
                return; // can't blur without a snapshot — leave the view as it is

            var (w, h) = MeasuredSize(view);
            levels = BuildBlurLevels(image);
            var drawable = new UnblurDrawable(levels, view.BackgroundColor ?? Colors.LightGray, steps) { Progress = 0 };

            overlay = new GraphicsView
            {
                Drawable = drawable,
                WidthRequest = w,
                HeightRequest = h,
                BackgroundColor = Colors.Transparent,
            };
            CopyLayoutAttributes(view, overlay);

            var tcs = new TaskCompletionSource();
            var name = "vr-unblur-" + Interlocked.Increment(ref _animationCounter);

            view.SetValue(ActiveEffectProperty, new ActiveEffect { Cts = cts, Restore = () => overlay!.AbortAnimation(name) });

            // Optional tap-to-skip: jump straight to the fully unblurred view.
            if (tap == TapEnable.On)
            {
                var gesture = new TapGestureRecognizer();
                gesture.Tapped += (_, _) =>
                {
                    drawable.Progress = 1;
                    overlay!.Invalidate();
                    overlay.AbortAnimation(name);
                    tcs.TrySetResult();
                };
                overlay.GestureRecognizers.Add(gesture);
            }

            // Swap the live view out for the (blurred) overlay so the sharp original is never shown.
            slot.Detach(view);
            slot.Attach(overlay);
            overlay.Invalidate();

            var animation = new Animation(p => { drawable.Progress = p; overlay!.Invalidate(); }, 0, 1);
            animation.Commit(overlay, name, 16, (uint)Math.Max(1, seconds * 1000), Easing.Linear,
                             (_, _) => tcs.TrySetResult(), null);
            cts.Token.Register(() => { overlay!.AbortAnimation(name); tcs.TrySetResult(); });

            await tcs.Task;
        }
        catch { /* fall through to reveal */ }
        finally
        {
            if (overlay is not null && overlay.Parent is not null) slot.Detach(overlay);
            if (image is not null && view.Parent is null) slot.Attach(view); // reveal the real, sharp view
            view.Opacity = originalOpacity;
            if (view.GetValue(ActiveEffectProperty) is ActiveEffect) view.SetValue(ActiveEffectProperty, null);
            cts.Dispose();
            if (levels is not null)
                foreach (var level in levels) { try { level.Dispose(); } catch { } }
        }
    }

    /// <summary>Builds snapshot copies from blurriest (heavily downsized) to sharpest (the original, last).</summary>
    static IImage[] BuildBlurLevels(IImage image)
    {
        float maxDim = Math.Max(image.Width, image.Height);
        float[] fractions = { 0.03f, 0.05f, 0.08f, 0.12f, 0.18f, 0.27f, 0.40f, 0.60f, 0.80f };
        var levels = new List<IImage>(fractions.Length + 1);
        foreach (var f in fractions)
        {
            try { levels.Add(image.Downsize(Math.Max(2f, maxDim * f), disposeOriginal: false)); }
            catch { /* skip a level that can't be produced */ }
        }
        levels.Add(image); // sharp original is the final level
        return levels.ToArray();
    }

    /// <summary>TARDIS materialise opacity envelope: 0 (blank) → 1 (solid), flickering fast then settling.</summary>
    static float MaterialiseOpacity(float p)
    {
        float t = Math.Clamp(p, 0f, 1f);
        float presence = t * t * (3f - 2f * t);                       // smooth 0 → 1
        float inv = 1f - p;
        float phase = 7f * (inv + inv * inv) * MathF.Tau;             // strobes fast early, slows to solid
        float wave = 0.5f + 0.5f * MathF.Cos(phase);
        float sharp = MathF.Pow(wave, 1f + inv * 3f);
        return Math.Clamp(presence * (0.12f + 0.88f * sharp), 0f, 1f);
    }

    // ── Tennis (direct-view motion: volleys across the screen in arcs) ───────────────────────────────
    static async Task RunTennisAsync(View view, LayoutSlot slot, TennisSide side, bool disappear, double seconds)
    {
        if (IsAnimating(view))
            return;
        if (seconds <= 0)
            seconds = ViewEffects.ConfiguredDefaultSeconds;

        var cts = new CancellationTokenSource();
        double originalOpacity = view.Opacity;
        double homeX = view.TranslationX, homeY = view.TranslationY;

        void RestoreView()
        {
            view.TranslationX = homeX;
            view.TranslationY = homeY;
            view.Opacity = originalOpacity;
        }

        view.SetValue(ActiveEffectProperty, new ActiveEffect { Cts = cts, Restore = RestoreView });

        try
        {
            var keys = BuildTennisPath(view, side, disappear);
            int segments = keys.Count - 1;

            // Place the view at the starting point before the first frame.
            view.TranslationX = keys[0];
            view.TranslationY = 0;

            await AnimateRawAsync(view, seconds, Easing.Linear, cts.Token, u =>
            {
                double fu = Math.Clamp(u, 0, 1) * segments;
                int i = Math.Min(segments - 1, (int)fu);
                float s = (float)(fu - i);
                float ease = s * s * (3f - 2f * s);                 // ease in/out within each volley

                double x = keys[i] + (keys[i + 1] - keys[i]) * ease;
                float peak = 50f + 36f * (1f - i / (float)segments); // taller arcs early, flatter later
                double y = -peak * 4f * s * (1f - s);                // parabolic lob (up then down)

                view.TranslationX = x;
                view.TranslationY = y;

                if (disappear && i == segments - 1)
                    view.Opacity = originalOpacity * (1f - ease);    // fade out on the exit volley
            });
        }
        catch { /* fall through */ }
        finally
        {
            if (!cts.IsCancellationRequested && view.GetValue(ActiveEffectProperty) is ActiveEffect)
            {
                if (disappear)
                {
                    slot.Detach(view);
                    RestoreView(); // reset transform/opacity so a later re-add shows it normally
                }
                else
                {
                    view.TranslationX = homeX; // land at home
                    view.TranslationY = homeY;
                    view.Opacity = originalOpacity;
                }
                view.SetValue(ActiveEffectProperty, null);
            }
            cts.Dispose();
        }
    }

    /// <summary>
    /// Appears imperatively (reveal): volleys in from <paramref name="side"/> and lands in place.
    /// </summary>
    internal static Task RunTennisAppearAsync(View view, LayoutSlot slot, TennisSide side, double seconds)
        => RunTennisAsync(view, slot, side, disappear: false, seconds);

    /// <summary>The X-translation keyframes for the tennis volley: off-screen → far/near ×4 → land/exit.</summary>
    static List<double> BuildTennisPath(View view, TennisSide side, bool disappear)
    {
        // Window width and the view's window rect, to size the swings to the screen edges.
        double winW = view.Window?.Width ?? 0;
        double boxX, boxW;
        if (PlatformGeometry.WindowBounds(view) is { } b && b.Width > 0)
        {
            boxX = b.X; boxW = b.Width;
            if (winW <= 0) winW = boxX + boxW + boxX; // rough symmetric guess
        }
        else
        {
            boxW = view.Width > 0 ? view.Width : 100;
            boxX = 0;
            if (winW <= 0) winW = boxW * 4;
        }
        if (winW <= 0) winW = boxW * 4;

        const double margin = 12;
        double nearLeft = -boxX + margin;                       // view's left edge to the screen's left
        double nearRight = (winW - boxX - boxW) - margin;       // view's right edge to the screen's right
        double offLeft = -(boxX + boxW) - 24;                   // fully off the left
        double offRight = (winW - boxX) + 24;                   // fully off the right

        bool fromLeft = side == TennisSide.Left;
        double near = fromLeft ? nearLeft : nearRight;
        double far = fromLeft ? nearRight : nearLeft;

        var keys = new List<double> { fromLeft ? offLeft : offRight };
        for (int i = 0; i < 4; i++) { keys.Add(far); keys.Add(near); } // to the other side and back, ×4

        if (disappear)
            keys[keys.Count - 1] = fromLeft ? offLeft : offRight;       // last return flies off-screen
        else
            keys.Add(0);                                                // land at home

        return keys;
    }

    static async Task VibrateAsync(View view, double seconds, CancellationToken ct)
    {
        double total = seconds * 1000, elapsed = 0;
        int i = 0;
        while (elapsed < total && !ct.IsCancellationRequested)
        {
            double frac = elapsed / total;
            double amplitude = 1.5 + frac * 11;               // grows
            uint length = (uint)Math.Max(10, 45 - frac * 34); // shortens → faster
            double dx = (EffectDrawable.Hash01(i * 2) - 0.5f) * 2 * amplitude;
            double dy = (EffectDrawable.Hash01(i * 2 + 1) - 0.5f) * 2 * amplitude;
            try { await view.TranslateToAsync(dx, dy, length, Easing.Linear); }
            catch { break; }
            elapsed += length;
            i++;
        }
        try { await view.TranslateToAsync(0, 0, 12); } catch { /* ignore */ }
    }

    static async Task<IImage?> TryCaptureAsync(View view)
    {
        try
        {
            var screenshot = await view.CaptureAsync();
            if (screenshot is null) return null;
            using var stream = await screenshot.OpenReadAsync();
            return PlatformImage.FromStream(stream, ImageFormat.Png);
        }
        catch
        {
            return null; // capture unsupported on this platform/state → effects fall back to BaseColor
        }
    }

    static (float width, float height) MeasuredSize(View view)
    {
        float w = (float)(view.Width > 0 ? view.Width : view.WidthRequest > 0 ? view.WidthRequest : 100);
        float h = (float)(view.Height > 0 ? view.Height : view.HeightRequest > 0 ? view.HeightRequest : 100);
        return (w, h);
    }

    /// <summary>Copies the common positional attached properties so the overlay lands where the view was.</summary>
    static void CopyLayoutAttributes(BindableObject from, BindableObject to)
    {
        Grid.SetRow(to, Grid.GetRow(from));
        Grid.SetColumn(to, Grid.GetColumn(from));
        Grid.SetRowSpan(to, Grid.GetRowSpan(from));
        Grid.SetColumnSpan(to, Grid.GetColumnSpan(from));
        AbsoluteLayout.SetLayoutBounds(to, AbsoluteLayout.GetLayoutBounds(from));
        AbsoluteLayout.SetLayoutFlags(to, AbsoluteLayout.GetLayoutFlags(from));
    }
}
