# How it works

This page explains the internals — useful if you're extending the plugin or debugging an effect.

## Removing vs. collapsing

`IsVisible="false"` leaves an element in the visual tree: it's still measured, still bound, still holds
platform resources. Plugin.ViewEffects instead **detaches** the view from its container. The plugin
records the view's *slot* (its parent and child index, or that it was a `ContentView`/`ScrollView`'s
`Content`) so it can re-attach the view at the same place when the condition flips back.

## The two rendering paths

### Contained (in-slot) — Freeze, Melt, Dematerialise, Plughole

1. The live view is **snapshotted** to an image (`CaptureAsync` → `PlatformImage`).
2. A transient, transparent `GraphicsView` of the same size is swapped into the view's layout slot, and
   the live view is detached. Grid row/column and AbsoluteLayout bounds are copied to the overlay so it
   sits exactly where the view was.
3. An `IDrawable` renders the effect frame-by-frame from a `Progress` value (0 → 1) that an `Animation`
   tweens; the `GraphicsView` is invalidated each frame.
4. On completion the overlay is removed, leaving the slot empty (the view is removed). If snapshotting
   isn't available, the drawable falls back to a solid rectangle of the view's background colour.

### Full-window (spanning) — Shatter, Explode

Shards and particles need to fly **beyond** the view's bounds, so these draw on a window-wide
`WindowOverlay` instead of an in-slot `GraphicsView`:

1. The view is snapshotted, then hidden with `Opacity = 0` (kept in its slot — no layout reflow).
2. The snapshot is drawn on the overlay at the view's **window-space rectangle**, obtained from the
   platform view (`PlatformGeometry`) so it lines up exactly, including any navigation-bar / safe-area
   offset.
3. On completion the overlay is removed and the view is actually detached from its slot.

### Direct-view — Materialise, Tennis

These animate the **live view** itself rather than a snapshot:

- **Materialise** sets `Opacity = 0` (starts blank) and tweens opacity with a TARDIS flicker envelope up
  to fully present. Because it's the real view, it's inherently at the view's final laid-out size.
- **Tennis** tweens `TranslationX/Y` along an arc path that volleys between the screen edges. MAUI
  layouts don't clip children by default, so the view sweeps across the whole screen. Swing extents are
  sized from the platform window bounds.

## Cancellation & restore

While an effect runs, an "active effect" is tracked on the view. If `RemoveWhen` flips back to `false`
(or another effect starts), the running animation is aborted and the view is restored to its place —
overlay removed, opacity/translation reset.

## Duration resolution

`seconds = 0` (`ViewEffects.UseDefault`) is a sentinel meaning "use the configured default", which is
`ViewEffectsOptions.DefaultDurationSeconds` (set by `UseViewEffects`) or `3` otherwise. Any explicit
value wins. See [Configuration](configuration.md).
