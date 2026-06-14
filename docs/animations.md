# Animations

Every animation has a **bound duration in seconds** (default 3). **Removal** animations play and then
remove the view from its parent; **reveal** animations bring a view in and leave it in place.

| Animation | Kind | Extra parameter |
|-----------|------|-----------------|
| [Freeze](#freeze) | removal | — |
| [Melt](#melt) | removal | — |
| [Explode](#explode) | removal | — |
| [Shatter](#shatter) | removal | `ShatterOrigin` |
| [Dematerialise](#dematerialise) | removal | — |
| [Plughole](#plughole) | removal | — |
| [Tennis Disappear](#tennis-disappear) | removal | `TennisSide` |
| [Materialise](#materialise) | reveal | — |
| [Tennis Appear](#tennis-appear) | reveal | `TennisSide` |
| [Unblur](#unblur) | reveal | `TapEnable`; default 6 s |

Removal animations are available through both the [attached-property API](usage.md#xaml--attached-properties)
(`RemovalAnimation`) and the [imperative API](usage.md#c--imperative-api). The two reveals are imperative
only.

---

## Freeze

The contents tint from light blue to dark blue (freezing over), then shatter into a grid of ice shards
that scatter outward and fall away under gravity while fading.

```csharp
await view.FreezeAsync(seconds: 3);
```

`RemovalAnimation.Freeze`

## Melt

The view is sliced into vertical strips that drip downward at varying speeds, stretching as they go,
past the bottom of the view, then fade out.

```csharp
await view.MeltAsync();
```

`RemovalAnimation.Melt`

## Explode

The view vibrates from slow to fast, then bursts outward in a spray of coloured particles. Renders on a
**full-window overlay** so the particles fly across the whole screen rather than being clipped to the
view's bounds.

```csharp
await view.ExplodeAsync(2);
```

`RemovalAnimation.Explode`

## Shatter

The view tints blue and a web of cracks radiates from a **`ShatterOrigin`** impact point, then it breaks
into a spray of irregular angular glass shards that fly outward from that point, spin, and fall. Renders
on a **full-window overlay**.

```csharp
await view.ShatterAsync(ShatterOrigin.TopRight, seconds: 2.5);
```

`RemovalAnimation.Shatter` · origin defaults to `ShatterOrigin.Centre`.

`ShatterOrigin` is one of the nine anchor points:

```
TopLeft      TopCentre      TopRight
Left         Centre         Right
BottomLeft   BottomCentre   BottomRight
```

## Dematerialise

TARDIS-style: the view flickers translucently in and out — the strobe accelerating and hardening as it
goes — behind a fading blue-white ghost, until it has faded away entirely.

```csharp
await view.DematerialiseAsync();
```

`RemovalAnimation.Dematerialise`

## Plughole

The view is sliced into concentric rings that swirl around its centre — inner rings spinning faster —
while being pulled inward and shrinking to nothing, like water spiralling down a drain.

```csharp
await view.PlugholeAsync();
```

`RemovalAnimation.Plughole`

## Tennis Disappear

The view volleys back and forth across the screen in arcs (entering from a **`TennisSide`**), four times,
and on the final volley flies off-screen and is removed.

```csharp
await view.TennisDisappearAsync(TennisSide.Left, seconds: 3);
```

`RemovalAnimation.TennisDisappear` · `TennisSide` is `Left` or `Right`.

---

## Materialise

The reverse of *Dematerialise*. The view **starts blank** and flickers into existence at its final
laid-out size, ending fully present. (Reveal — leaves the view in place.)

```csharp
await view.MaterialiseAsync();
```

## Tennis Appear

The view volleys in from a `TennisSide`, arcing back and forth across the screen four times, then lands
in place. (Reveal — leaves the view in place.)

```csharp
await view.TennisAppearAsync(TennisSide.Right);
```

## Unblur

The view is captured and shown **fully blurred**, then the blur is gradually removed over the duration
until the sharp original is revealed in place. The sharp original is never shown until the very end.
(Reveal — leaves the view in place.)

- **Default duration is 6 seconds** (not 3). Configurable via
  [`UseViewEffects`](configuration.md) (`DefaultUnblurSeconds`).
- **Tap to skip is opt-in.** Pass `TapEnable.On` to let a tap on the view jump straight to the
  unblurred result; the default is `TapEnable.Off`.
- **Stepping is optional.** Pass a `timestep` (seconds) to snap the blur through discrete steps held for
  ~that long each, instead of dissolving smoothly. The default (`0`) gives the smooth animation.

```csharp
await view.UnblurAsync();                           // 6 s, smooth, tap disabled
await view.UnblurAsync(4, TapEnable.On);            // 4 s, tap-to-skip enabled
await view.UnblurAsync(6, TapEnable.Off, 0.5);      // steps every ~0.5 s
```

> Blur is approximated by progressively downsizing the snapshot and letting the platform upscale it
> (more downsizing = blurrier), cross-dissolving toward the sharp original. There is no portable
> Gaussian-blur primitive in MAUI Graphics, so this is the cross-platform approach.

---

## A note on snapshots

The snapshot-based effects (Freeze, Melt, Explode, Shatter, Dematerialise, Plughole) capture an image of
the live view and animate that. If snapshotting is unavailable on a given platform/state, they fall back
to animating a solid rectangle of the view's background colour. Materialise and the Tennis animations
animate the **live view** directly (opacity / translation), so they always reflect the real view.
