# Getting started

## Install

```
dotnet add package Plugin.ViewEffects
```

Targets `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, and `net10.0-windows`.

## Register (optional)

Plugin.ViewEffects works without any registration — the attached properties and extension methods are
self-contained. You only need to register it if you want to change the **default animation duration**:

```csharp
using Plugin.ViewEffects;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseViewEffects(options => options.DefaultDurationSeconds = 2.5); // default is 3
        // ...
        return builder.Build();
    }
}
```

See [Configuration](configuration.md) for details.

## Your first animation

### From XAML

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vfx="clr-namespace:Plugin.ViewEffects;assembly=Plugin.ViewEffects">

    <VerticalStackLayout>
        <Label Text="Premium feature"
               vfx:ViewRemover.RemoveWhen="{Binding IsFreeTier}"
               vfx:ViewRemover.Animation="Dematerialise" />
    </VerticalStackLayout>

</ContentPage>
```

When `IsFreeTier` becomes `true`, the label dematerialises and is removed from the layout. When it
returns to `false`, the label is re-inserted at its original position.

### From C#

```csharp
using Plugin.ViewEffects;

// Play an effect, then remove the view:
await myCard.MeltAsync();              // 3 s (the default)
await myCard.ExplodeAsync(1.5);        // 1.5 s
await myCard.ShatterAsync(ShatterOrigin.BottomLeft);

// Bring a view in (leaves it in place):
await myCard.MaterialiseAsync();
await myCard.TennisAppearAsync(TennisSide.Right);
```

## Next steps

- [Animations](animations.md) — what each effect looks like and its parameters
- [Usage](usage.md) — the two APIs and which containers are supported
