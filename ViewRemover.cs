using Microsoft.Maui.Controls;

namespace Plugin.ViewEffects;

/// <summary>
/// Provides attached properties that physically remove a <see cref="View"/> from its parent's
/// visual tree when a condition is met, rather than merely collapsing it like
/// <see cref="VisualElement.IsVisible"/> does — optionally playing a removal animation first.
///
/// <para>
/// Setting <c>IsVisible="false"</c> keeps the element in the visual tree; it is still measured,
/// still bound, and still holds onto its resources. <see cref="ViewRemover"/> instead detaches the
/// view from its parent container so it no longer participates in layout at all, and re-attaches it
/// when the condition flips back.
/// </para>
///
/// <para>
/// Set <see cref="AnimationProperty">ViewRemover.Animation</see> to play a
/// <see cref="RemovalAnimation"/> (Freeze, Melt, Explode, Shatter) over the view before it is removed.
/// <see cref="DurationProperty">ViewRemover.Duration</see> sets the length in seconds (default 3).
/// </para>
///
/// <para>
/// Supported containers: any <see cref="Layout"/> (e.g. <c>VerticalStackLayout</c>, <c>Grid</c>,
/// <c>FlexLayout</c>) — restored at its original child index — as well as the single-content hosts
/// <see cref="ContentView"/> and <see cref="ScrollView"/>, restored via their <c>Content</c> property.
/// </para>
///
/// <example>
/// <code lang="xaml">
/// xmlns:vr="clr-namespace:Plugin.ViewEffects;assembly=Plugin.ViewEffects"
///
/// &lt;Label Text="Premium feature"
///        vr:ViewRemover.RemoveWhen="{Binding IsFreeTier}"
///        vr:ViewRemover.Animation="Shatter"
///        vr:ViewRemover.ShatterOrigin="TopRight"
///        vr:ViewRemover.Duration="2.5" /&gt;
/// </code>
/// </example>
/// </summary>
public static class ViewRemover
{
    /// <summary>
    /// When <c>true</c>, the view is removed from its parent container (after the configured
    /// <see cref="AnimationProperty">Animation</see>, if any). When it returns to <c>false</c>, the
    /// view is re-inserted at the index it originally occupied.
    /// </summary>
    public static readonly BindableProperty RemoveWhenProperty = BindableProperty.CreateAttached(
        "RemoveWhen", typeof(bool), typeof(ViewRemover), false, propertyChanged: OnRemoveWhenChanged);

    /// <summary>The <see cref="RemovalAnimation"/> to play before removing the view. Defaults to <see cref="RemovalAnimation.None"/>.</summary>
    public static readonly BindableProperty AnimationProperty = BindableProperty.CreateAttached(
        "Animation", typeof(RemovalAnimation), typeof(ViewRemover), RemovalAnimation.None);

    /// <summary>The impact point used by <see cref="RemovalAnimation.Shatter"/>. Defaults to <see cref="ShatterOrigin.Centre"/>.</summary>
    public static readonly BindableProperty ShatterOriginProperty = BindableProperty.CreateAttached(
        "ShatterOrigin", typeof(ShatterOrigin), typeof(ViewRemover), ShatterOrigin.Centre);

    /// <summary>The side used by <see cref="RemovalAnimation.TennisDisappear"/>. Defaults to <see cref="TennisSide.Left"/>.</summary>
    public static readonly BindableProperty TennisSideProperty = BindableProperty.CreateAttached(
        "TennisSide", typeof(TennisSide), typeof(ViewRemover), TennisSide.Left);

    /// <summary>
    /// The animation length in seconds. Left at <see cref="ViewEffects.UseDefault"/> (0) it uses the
    /// configured default duration (set via <c>UseViewEffects</c>, otherwise 3 seconds).
    /// </summary>
    public static readonly BindableProperty DurationProperty = BindableProperty.CreateAttached(
        "Duration", typeof(double), typeof(ViewRemover), ViewEffects.UseDefault);

    /// <summary>Stores the slot a removed view occupied, so it can be restored.</summary>
    static readonly BindableProperty RemovalStateProperty = BindableProperty.CreateAttached(
        "RemovalState", typeof(RemovalState), typeof(ViewRemover), null);

    public static bool GetRemoveWhen(BindableObject view) => (bool)view.GetValue(RemoveWhenProperty);
    public static void SetRemoveWhen(BindableObject view, bool value) => view.SetValue(RemoveWhenProperty, value);

    public static RemovalAnimation GetAnimation(BindableObject view) => (RemovalAnimation)view.GetValue(AnimationProperty);
    public static void SetAnimation(BindableObject view, RemovalAnimation value) => view.SetValue(AnimationProperty, value);

    public static ShatterOrigin GetShatterOrigin(BindableObject view) => (ShatterOrigin)view.GetValue(ShatterOriginProperty);
    public static void SetShatterOrigin(BindableObject view, ShatterOrigin value) => view.SetValue(ShatterOriginProperty, value);

    public static TennisSide GetTennisSide(BindableObject view) => (TennisSide)view.GetValue(TennisSideProperty);
    public static void SetTennisSide(BindableObject view, TennisSide value) => view.SetValue(TennisSideProperty, value);

    public static double GetDuration(BindableObject view) => (double)view.GetValue(DurationProperty);
    public static void SetDuration(BindableObject view, double value) => view.SetValue(DurationProperty, value);

    static RemovalState? GetRemovalState(BindableObject view) => (RemovalState?)view.GetValue(RemovalStateProperty);
    static void SetRemovalState(BindableObject view, RemovalState? value) => view.SetValue(RemovalStateProperty, value);

    static void OnRemoveWhenChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not View view)
            return;

        if ((bool)newValue)
            Remove(view);
        else
            Restore(view);
    }

    static void Remove(View view)
    {
        // Already removed (or removal already in flight) — nothing to do.
        if (GetRemovalState(view) is not null)
            return;

        if (RemovalEffects.LayoutSlot.Capture(view) is not { } slot)
            return;

        // Record the slot up-front so the view can be restored even mid-animation.
        SetRemovalState(view, new RemovalState(slot));

        var animation = GetAnimation(view);
        if (animation == RemovalAnimation.None)
            slot.Detach(view);
        else
            _ = RemovalEffects.RunAsync(view, slot, animation, GetShatterOrigin(view), GetTennisSide(view), GetDuration(view));
    }

    static void Restore(View view)
    {
        // If an effect is mid-flight, cancelling it already restores the view to its place.
        bool handledByEffect = RemovalEffects.Cancel(view);

        if (GetRemovalState(view) is not { } state)
            return;

        SetRemovalState(view, null);

        // Otherwise (instant removal, or an effect that already completed) re-attach it ourselves.
        if (!handledByEffect)
            state.Slot.Attach(view);
    }

    sealed record RemovalState(RemovalEffects.LayoutSlot Slot);
}
