# Plugin.ViewEffects — Documentation

A .NET MAUI plugin that **removes a `View` from its parent's visual tree** when a condition is met —
rather than merely collapsing it like `IsVisible` — optionally playing a **removal animation** first,
plus matching **reveal** animations that bring a view in.

Unlike `IsVisible="false"` (which keeps the element in the visual tree — still measured, still bound,
still holding resources), Plugin.ViewEffects detaches the view from its container so it no longer
participates in layout at all, and re-attaches it when the condition flips back.

## Contents

- [Getting started](getting-started.md) — install, register, first animation
- [Animations](animations.md) — the full catalogue, with parameters and behaviour
- [Usage](usage.md) — XAML attached properties vs. the imperative API, supported containers
- [Configuration](configuration.md) — `UseViewEffects`, default duration
- [API reference](api-reference.md) — every public type and member
- [How it works](how-it-works.md) — snapshots, overlays, the full-window path

## At a glance

```xml
xmlns:vfx="clr-namespace:Plugin.ViewEffects;assembly=Plugin.ViewEffects"

<!-- Shatter this card out of the layout (from the top-right) when IsDismissed becomes true -->
<Border vfx:ViewRemover.RemoveWhen="{Binding IsDismissed}"
        vfx:ViewRemover.Animation="Shatter"
        vfx:ViewRemover.ShatterOrigin="TopRight"
        vfx:ViewRemover.Duration="2.5" />
```

```csharp
using Plugin.ViewEffects;

await myView.ExplodeAsync();                       // play, then remove
await myView.ShatterAsync(ShatterOrigin.Centre, 2);
await myView.MaterialiseAsync();                   // flicker a view in
```

## Supported platforms

`net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows`.

## License

MIT
