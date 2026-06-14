# Plugin.ViewEffects

A tiny .NET MAUI plugin that **physically removes** a `View` from its parent's visual tree when a
condition is met — and puts it back at its original position when the condition flips. It can also play
one of several **removal animations** first, and a matching **materialise** animation to bring a view in.

Unlike `IsVisible="false"`, which keeps the element in the visual tree (still measured, still bound,
still holding resources), `ViewRemover` detaches the view from its container entirely so it no longer
participates in layout at all.

## Animations

| Animation       | Effect |
|-----------------|--------|
| `Freeze`        | Contents tint from light blue to dark blue (freezing over), then shatter into ice shards that scatter and fall. |
| `Melt`          | Contents melt: vertical slices drip down past the bottom of the view and fade away. |
| `Explode`       | The view vibrates from slow to fast, then bursts outward in a spray of coloured particles. |
| `Shatter`       | Glass shatter: the view tints blue and cracks radiate from a `ShatterOrigin` impact point, then it breaks into a spray of angular glass shards that fly outward, spin, and fall. |
| `Dematerialise` | TARDIS-style: the view flickers translucently in and out — faster as it goes — behind a fading blue-white ghost, until gone. |
| `Plughole`      | The view spirals around its centre — swirling faster toward the middle — while being pulled inward and shrinking to nothing, like water down a drain. |
| `TennisDisappear` | The view volleys back and forth across the screen in arcs (entering from `Left` or `Right`), four times, then on the final volley flies off and is removed. |

Every animation has a **bound duration in seconds (default 3)**, and **once the animation completes the
view is removed**.

There are two **reveal** animations that bring a view *in* and leave it in place:

- `MaterialiseAsync` — the reverse of `Dematerialise`. The view **starts blank** and flickers into
  existence at its final laid-out size.
- `TennisAppearAsync(side)` — the view volleys in from `Left`/`Right`, arcing back and forth four times,
  then lands in place.

**`ShatterOrigin`** is the impact point the glass radiates from — one of the nine anchor points:
`Centre` (default), `TopLeft`, `TopCentre`, `TopRight`, `Left`, `Right`, `BottomLeft`, `BottomCentre`,
`BottomRight`.

`Shatter` and `Explode` render on a **full-window overlay**, so their shards and particles fly across the
whole screen rather than being clipped to the view's bounds. The other effects render within the view's
own slot.

The effects snapshot the live view and animate the snapshot. If snapshotting is unavailable on a given
platform/state, they fall back to animating a solid rectangle of the view's background colour.

## Install

```
dotnet add package Plugin.ViewEffects
```

Supports `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, and `net10.0-windows`.

## Register in MauiProgram

Add the plugin in `MauiProgram.CreateMauiApp` (optional — only needed to change defaults):

```csharp
using Plugin.ViewEffects;

builder
    .UseMauiApp<App>()
    .UseViewEffects(options => options.DefaultDurationSeconds = 2.5); // optional; default is 3s
```

Any effect called without an explicit duration (or with `ViewRemover.Duration` left unset) then uses
this configured default.

## Usage

### XAML

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vr="clr-namespace:Plugin.ViewEffects;assembly=Plugin.ViewEffects">

    <VerticalStackLayout>
        <Label Text="Always shown" />

        <!-- Removed from the layout entirely while IsFreeTier is true -->
        <Label Text="Premium feature"
               vr:ViewRemover.RemoveWhen="{Binding IsFreeTier}" />

        <!-- With an animation: glass-shatter from the top-right over 2.5s, then remove -->
        <Border vr:ViewRemover.RemoveWhen="{Binding IsDismissed}"
                vr:ViewRemover.Animation="Shatter"
                vr:ViewRemover.ShatterOrigin="TopRight"
                vr:ViewRemover.Duration="2.5" />

        <Label Text="Also always shown" />
    </VerticalStackLayout>

</ContentPage>
```

### C# — attached properties

```csharp
var label = new Label { Text = "Premium feature" };
ViewRemover.SetAnimation(label, RemovalAnimation.Explode);
ViewRemover.SetDuration(label, 2);          // seconds
ViewRemover.SetRemoveWhen(label, true);     // vibrate → explode → removed
```

### C# — imperative (await the effect)

Each extension plays its effect and then removes the view; `MaterialiseAsync` does the reverse and
leaves the view in place. All take an optional duration in seconds (default 3).

```csharp
await myView.FreezeAsync();
await myView.MeltAsync(2);
await myView.ExplodeAsync();
await myView.ShatterAsync(ShatterOrigin.TopRight, 1.5);
await myView.PlugholeAsync();
await myView.DematerialiseAsync();
await myView.TennisDisappearAsync(TennisSide.Left);

// Reveals — bring a view in and leave it in place:
await myView.MaterialiseAsync();              // starts blank, flickers in
await myView.TennisAppearAsync(TennisSide.Right);
```

## How it works

`ViewRemover.RemoveWhen` is an attached bindable property. When it becomes `true`, the view is detached
from its parent, and enough state to restore it is remembered. When it returns to `false`, the view is
re-attached.

Supported containers:

- **Any `Layout`** (e.g. `VerticalStackLayout`, `Grid`, `FlexLayout`) — the view is removed from the
  child collection and re-inserted at its original index (clamped, in case the container changed while
  the view was detached).
- **`ContentView`** and **`ScrollView`** — the single-content hosts. Their `Content` is cleared on
  removal and restored when the condition flips back.

## License

MIT
