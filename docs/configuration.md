# Configuration

Plugin.ViewEffects needs no setup to work. The only thing you can configure is the **default animation
duration**, via the `UseViewEffects` host-builder extension.

## UseViewEffects

```csharp
using Plugin.ViewEffects;

var builder = MauiApp.CreateBuilder();
builder
    .UseMauiApp<App>()
    .UseViewEffects(options =>
    {
        options.DefaultDurationSeconds = 2.5;   // default is 3
    });
```

- The callback is optional: `.UseViewEffects()` with no arguments is valid and simply keeps the built-in
  default of 3 seconds.
- It registers `ViewEffectsOptions` as a singleton in the app's service container, and returns the same
  `MauiAppBuilder` for chaining.

## How the default duration is applied

Animation durations resolve in this order:

1. An explicit `seconds` argument on a method call (e.g. `view.MeltAsync(2)`), or an explicit
   `ViewRemover.Duration` in XAML.
2. Otherwise — when the value is `ViewEffects.UseDefault` (`0`) — the **configured default**.
3. The configured default is `ViewEffectsOptions.DefaultDurationSeconds`, or `ViewEffects.DefaultSeconds`
   (3) if `UseViewEffects` was never called.

```csharp
public const double DefaultSeconds = 3;   // built-in fallback
public const double UseDefault    = 0;    // sentinel: "use the configured default"
```

So with `DefaultDurationSeconds = 2.5` configured:

```csharp
await view.MeltAsync();        // 2.5 s  (uses the configured default)
await view.MeltAsync(4);       // 4 s    (explicit wins)
```
