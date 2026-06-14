# API reference

Namespace: `Plugin.ViewEffects`

## ViewEffects (extension methods)

`static class ViewEffects` — imperative entry points. Each is an extension on
`Microsoft.Maui.Controls.View`.

| Member | Signature | Notes |
|--------|-----------|-------|
| `FreezeAsync` | `Task FreezeAsync(this View view, double seconds = UseDefault)` | Removal. |
| `MeltAsync` | `Task MeltAsync(this View view, double seconds = UseDefault)` | Removal. |
| `ExplodeAsync` | `Task ExplodeAsync(this View view, double seconds = UseDefault)` | Removal (full-window). |
| `ShatterAsync` | `Task ShatterAsync(this View view, ShatterOrigin origin = ShatterOrigin.Centre, double seconds = UseDefault)` | Removal (full-window). |
| `PlugholeAsync` | `Task PlugholeAsync(this View view, double seconds = UseDefault)` | Removal. |
| `DematerialiseAsync` | `Task DematerialiseAsync(this View view, double seconds = UseDefault)` | Removal. |
| `TennisDisappearAsync` | `Task TennisDisappearAsync(this View view, TennisSide side, double seconds = UseDefault)` | Removal. |
| `MaterialiseAsync` | `Task MaterialiseAsync(this View view, double seconds = UseDefault)` | Reveal — starts blank, leaves the view in place. |
| `TennisAppearAsync` | `Task TennisAppearAsync(this View view, TennisSide side, double seconds = UseDefault)` | Reveal. |
| `UnblurAsync` | `Task UnblurAsync(this View view, double seconds = UseDefault, TapEnable tap = TapEnable.Off, double timestep = 0)` | Reveal. Default duration 6 s; `tap` enables tap-to-skip; `timestep` > 0 steps discretely. |
| `PlayAsync` | `Task PlayAsync(this View view, RemovalAnimation animation, ShatterOrigin origin = ShatterOrigin.Centre, TennisSide tennisSide = TennisSide.Left, double seconds = UseDefault)` | Plays any removal animation. |

Constants:

| Member | Value | Meaning |
|--------|-------|---------|
| `ViewEffects.DefaultSeconds` | `3` | Built-in default duration. |
| `ViewEffects.DefaultUnblurSeconds` | `6` | Built-in default duration for `UnblurAsync`. |
| `ViewEffects.UseDefault` | `0` | Sentinel for `seconds` meaning "use the configured default". |

All methods are no-ops (return a completed task) if the view is not in a [supported
container](usage.md#supported-containers).

## ViewRemover (attached properties)

`static class ViewRemover` — XAML-friendly attached properties.

| Property | Type | Default | Get / Set |
|----------|------|---------|-----------|
| `RemoveWhen` | `bool` | `false` | `GetRemoveWhen` / `SetRemoveWhen` |
| `Animation` | `RemovalAnimation` | `None` | `GetAnimation` / `SetAnimation` |
| `ShatterOrigin` | `ShatterOrigin` | `Centre` | `GetShatterOrigin` / `SetShatterOrigin` |
| `TennisSide` | `TennisSide` | `Left` | `GetTennisSide` / `SetTennisSide` |
| `Duration` | `double` | `UseDefault` (0) | `GetDuration` / `SetDuration` |

Each property also exposes the corresponding `BindableProperty` (e.g. `RemoveWhenProperty`).

## Enums

### RemovalAnimation

`None`, `Freeze`, `Melt`, `Explode`, `Shatter`, `Dematerialise`, `Plughole`, `TennisDisappear`.

### ShatterOrigin

`Centre` (default), `TopLeft`, `TopCentre`, `TopRight`, `Left`, `Right`, `BottomLeft`, `BottomCentre`,
`BottomRight`.

### TennisSide

`Left`, `Right`.

### TapEnable

`Off` (default), `On`. Whether tapping the view during `UnblurAsync` skips to the end.

## Host builder

### AppHostBuilderExtensions

```csharp
static MauiAppBuilder UseViewEffects(this MauiAppBuilder builder,
                                     Action<ViewEffectsOptions>? configure = null)
```

Registers the plugin and applies optional configuration. Returns the same builder for chaining. See
[Configuration](configuration.md).

### ViewEffectsOptions

```csharp
sealed class ViewEffectsOptions
{
    double DefaultDurationSeconds { get; set; } // default: 3
    double DefaultUnblurSeconds   { get; set; } // default: 6
}
```
