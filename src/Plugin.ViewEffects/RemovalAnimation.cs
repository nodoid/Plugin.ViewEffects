namespace Plugin.ViewEffects;

/// <summary>
/// The animation played over a view immediately before it is removed from its parent.
/// </summary>
public enum RemovalAnimation
{
    /// <summary>No animation — the view is removed instantly.</summary>
    None,

    /// <summary>
    /// The contents tint from light blue to dark blue (freezing), then shatter into a scatter of
    /// ice shards that fall away under gravity.
    /// </summary>
    Freeze,

    /// <summary>The contents melt: vertical slices drip down past the bottom of the view and fade out.</summary>
    Melt,

    /// <summary>The view vibrates from slow to fast, then bursts outward in a spray of coloured particles.</summary>
    Explode,

    /// <summary>
    /// Glass shatter: the view tints blue and a web of cracks radiates from a
    /// <see cref="ShatterOrigin"/> impact point, then it breaks into a spray of angular glass shards
    /// that fly outward from that point, spin, and fall away.
    /// </summary>
    Shatter,

    /// <summary>
    /// TARDIS-style dematerialisation: the view flickers translucently in and out — pulsing faster as
    /// it goes — behind a fading blue-white ghost, until it has faded away entirely.
    /// </summary>
    Dematerialise,

    /// <summary>
    /// Plughole: the view spirals around its centre — swirling faster toward the middle — while being
    /// pulled inward and shrinking to nothing, like water going down a drain.
    /// </summary>
    Plughole,

    /// <summary>
    /// Tennis disappear: the view volleys back and forth across the screen in arcs (entering from a
    /// <see cref="TennisSide"/>), and on the final volley flies off and is removed. The matching
    /// "appear" is <c>TennisAppearAsync</c>.
    /// </summary>
    TennisDisappear,
}

/// <summary>The side a tennis animation enters from (and, for disappear, exits toward).</summary>
public enum TennisSide
{
    /// <summary>Enters from the left.</summary>
    Left,

    /// <summary>Enters from the right.</summary>
    Right,
}

/// <summary>
/// The impact point a <see cref="RemovalAnimation.Shatter"/> radiates from — the nine anchor points of
/// the view. Defaults to <see cref="Centre"/>.
/// </summary>
public enum ShatterOrigin
{
    /// <summary>The centre of the view (default).</summary>
    Centre,

    TopLeft,
    TopCentre,
    TopRight,
    Left,
    Right,
    BottomLeft,
    BottomCentre,
    BottomRight,
}
