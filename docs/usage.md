# Usage

There are two ways to use Plugin.ViewEffects: **XAML attached properties** (declarative, driven by a
bound condition) and the **imperative extension methods** (`await` an effect).

## XAML / attached properties

The `ViewRemover` attached properties drive removal from a bound condition:

```xml
<ContentPage xmlns:vfx="clr-namespace:Plugin.ViewEffects;assembly=Plugin.ViewEffects">
    <VerticalStackLayout>

        <!-- Instant removal (no animation) -->
        <Label Text="Premium feature"
               vfx:ViewRemover.RemoveWhen="{Binding IsFreeTier}" />

        <!-- Animated removal -->
        <Border vfx:ViewRemover.RemoveWhen="{Binding IsDismissed}"
                vfx:ViewRemover.Animation="Shatter"
                vfx:ViewRemover.ShatterOrigin="TopRight"
                vfx:ViewRemover.Duration="2.5" />

    </VerticalStackLayout>
</ContentPage>
```

| Attached property | Type | Default | Purpose |
|-------------------|------|---------|---------|
| `RemoveWhen` | `bool` | `false` | When `true`, removes the view (after `Animation`, if any). Back to `false` re-inserts it at its original index. |
| `Animation` | `RemovalAnimation` | `None` | The effect to play before removing. |
| `ShatterOrigin` | `ShatterOrigin` | `Centre` | Impact point for `Shatter`. |
| `TennisSide` | `TennisSide` | `Left` | Entry side for `TennisDisappear`. |
| `Duration` | `double` | `0` (use configured default) | Animation length in seconds. `0` means "use the configured default" (3 s, or whatever `UseViewEffects` set). |

> The reveal animations (Materialise, Tennis Appear) have no attached-property form — they're triggered
> imperatively because they bring a view *in* rather than reacting to a removal condition.

### Toggling back

Because removal detaches the view (rather than collapsing it), setting `RemoveWhen` back to `false`
re-attaches the view at the index it originally occupied. If a removal animation is still mid-flight when
the condition flips back, it's cancelled and the view is restored in place.

## C# / imperative API

Every effect is an extension method on `View`. Each plays its effect and then removes the view; the two
reveals leave it in place. All take an optional `seconds` argument (default 3).

```csharp
using Plugin.ViewEffects;

// Removals (view ends up detached from its parent):
await view.FreezeAsync();
await view.MeltAsync(2);
await view.ExplodeAsync();
await view.ShatterAsync(ShatterOrigin.Centre, 1.5);
await view.PlugholeAsync();
await view.DematerialiseAsync();
await view.TennisDisappearAsync(TennisSide.Left);

// Reveals (view stays in place):
await view.MaterialiseAsync();
await view.TennisAppearAsync(TennisSide.Right);

// Or play any removal animation generically:
await view.PlayAsync(RemovalAnimation.Shatter, ShatterOrigin.BottomLeft, seconds: 2);
```

You can also set the attached properties from code:

```csharp
ViewRemover.SetAnimation(card, RemovalAnimation.Explode);
ViewRemover.SetDuration(card, 2);
ViewRemover.SetRemoveWhen(card, true);   // vibrate → explode → removed
```

## Supported containers

A view can be animated/removed when it lives in one of these containers:

- **Any `Layout`** (e.g. `VerticalStackLayout`, `Grid`, `FlexLayout`, `AbsoluteLayout`) — the view is
  removed from the child collection and re-inserted at its original index (clamped, in case the
  container changed while it was detached). Grid row/column and AbsoluteLayout bounds are preserved on
  the transient overlay.
- **`ContentView`** and **`ScrollView`** — the single-content hosts. Their `Content` is cleared on
  removal and restored on re-attach.

If the view isn't in a supported container, the imperative methods are a no-op (they return a completed
task).

## Duration

- Pass `seconds` explicitly to any method, or set `ViewRemover.Duration`.
- Omitting it (or passing `ViewEffects.UseDefault`, which is `0`) uses the **configured default** — 3
  seconds, unless changed via [`UseViewEffects`](configuration.md).
