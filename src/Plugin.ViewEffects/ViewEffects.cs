using Microsoft.Maui.Controls;

namespace Plugin.ViewEffects;

/// <summary>
/// Imperative entry points for the removal animations. Each method plays its effect and then removes
/// the view from its parent. The view must currently live in a supported container (any <c>Layout</c>,
/// or a <c>ContentView</c>/<c>ScrollView</c> as that host's content).
/// </summary>
public static class ViewEffects
{
    /// <summary>The built-in default animation length, in seconds, before any configuration.</summary>
    public const double DefaultSeconds = 3;

    /// <summary>
    /// Sentinel for the <c>seconds</c> parameters meaning "use the configured default duration"
    /// (set via <c>UseViewEffects</c>, or <see cref="DefaultSeconds"/> if unconfigured).
    /// </summary>
    public const double UseDefault = 0;

    /// <summary>The effective default duration, overridable at startup through <c>UseViewEffects</c>.</summary>
    internal static double ConfiguredDefaultSeconds = DefaultSeconds;

    /// <summary>The built-in default duration for <see cref="UnblurAsync"/>, in seconds.</summary>
    public const double DefaultUnblurSeconds = 6;

    /// <summary>The effective unblur default, overridable at startup through <c>UseViewEffects</c>.</summary>
    internal static double ConfiguredUnblurSeconds = DefaultUnblurSeconds;

    /// <summary>Freezes the view (light → dark blue) then shatters it into falling ice shards, then removes it.</summary>
    public static Task FreezeAsync(this View view, double seconds = UseDefault)
        => PlayAsync(view, RemovalAnimation.Freeze, seconds: seconds);

    /// <summary>Melts the view's contents down to the bottom and away, then removes it.</summary>
    public static Task MeltAsync(this View view, double seconds = UseDefault)
        => PlayAsync(view, RemovalAnimation.Melt, seconds: seconds);

    /// <summary>Vibrates the view from slow to fast, bursts it into coloured particles, then removes it.</summary>
    public static Task ExplodeAsync(this View view, double seconds = UseDefault)
        => PlayAsync(view, RemovalAnimation.Explode, seconds: seconds);

    /// <summary>
    /// Glass-shatters the view (cracks radiating from <paramref name="origin"/> into flying shards across
    /// the whole window), then removes it.
    /// </summary>
    public static Task ShatterAsync(this View view, ShatterOrigin origin = ShatterOrigin.Centre,
                                    double seconds = UseDefault)
        => PlayAsync(view, RemovalAnimation.Shatter, origin, seconds: seconds);

    /// <summary>Spirals the view down a plughole (swirling inward and shrinking to nothing), then removes it.</summary>
    public static Task PlugholeAsync(this View view, double seconds = UseDefault)
        => PlayAsync(view, RemovalAnimation.Plughole, seconds: seconds);

    /// <summary>Dematerialises the view TARDIS-style (flickering translucently away), then removes it.</summary>
    public static Task DematerialiseAsync(this View view, double seconds = UseDefault)
        => PlayAsync(view, RemovalAnimation.Dematerialise, seconds: seconds);

    /// <summary>
    /// Materialises the view TARDIS-style — the reverse of <see cref="DematerialiseAsync"/>. The view
    /// starts blank and flickers into existence at its final laid-out size, ending fully present.
    /// </summary>
    public static Task MaterialiseAsync(this View view, double seconds = UseDefault)
    {
        ArgumentNullException.ThrowIfNull(view);
        return RemovalEffects.RunMaterialiseAsync(view, seconds);
    }

    /// <summary>
    /// Unblur: the view is captured and shown fully blurred, then the blur is gradually removed over
    /// <paramref name="seconds"/> (default 6) until the sharp original is revealed in place. The sharp
    /// original is not shown until the very end. With <paramref name="tap"/> set to <see cref="TapEnable.On"/>,
    /// tapping the view skips straight to the unblurred result.
    /// </summary>
    /// <param name="timestep">
    /// When greater than 0, the unblur snaps through discrete steps held for roughly this many seconds
    /// each, rather than dissolving smoothly. The default (0) gives the smooth animation.
    /// </param>
    public static Task UnblurAsync(this View view, double seconds = UseDefault,
                                   TapEnable tap = TapEnable.Off, double timestep = 0)
    {
        ArgumentNullException.ThrowIfNull(view);
        var slot = RemovalEffects.LayoutSlot.Capture(view);
        return slot is null
            ? Task.CompletedTask
            : RemovalEffects.RunUnblurAsync(view, slot.Value, seconds, tap, timestep);
    }

    /// <summary>
    /// Tennis appear: the view volleys in from <paramref name="side"/>, arcing back and forth across the
    /// screen four times, then lands in place.
    /// </summary>
    public static Task TennisAppearAsync(this View view, TennisSide side, double seconds = UseDefault)
    {
        ArgumentNullException.ThrowIfNull(view);
        var slot = RemovalEffects.LayoutSlot.Capture(view);
        return slot is null
            ? Task.CompletedTask
            : RemovalEffects.RunTennisAppearAsync(view, slot.Value, side, seconds);
    }

    /// <summary>
    /// Tennis disappear: the view volleys back and forth across the screen four times (entering from
    /// <paramref name="side"/>) and on the final volley flies off and is removed.
    /// </summary>
    public static Task TennisDisappearAsync(this View view, TennisSide side, double seconds = UseDefault)
        => PlayAsync(view, RemovalAnimation.TennisDisappear, default, side, seconds);

    /// <summary>Plays an arbitrary <see cref="RemovalAnimation"/> over the view, then removes it.</summary>
    public static Task PlayAsync(this View view, RemovalAnimation animation,
                                 ShatterOrigin origin = ShatterOrigin.Centre,
                                 TennisSide tennisSide = TennisSide.Left,
                                 double seconds = UseDefault)
    {
        ArgumentNullException.ThrowIfNull(view);
        var slot = RemovalEffects.LayoutSlot.Capture(view);
        return slot is null
            ? Task.CompletedTask
            : RemovalEffects.RunAsync(view, slot.Value, animation, origin, tennisSide, seconds);
    }
}
