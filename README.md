# Plugin.ViewEffects

A .NET MAUI plugin that **removes a `View` from its parent's visual tree** when a condition is met
(rather than just collapsing it like `IsVisible`), optionally playing a removal animation first — plus
matching **reveal** animations to bring a view in.

Animations: **Freeze**, **Melt**, **Explode**, **Shatter** (glass, 9-point origin), **Dematerialise**,
**Plughole**, **Tennis Disappear** — and the reveals **Materialise** and **Tennis Appear**.

## Repository layout

| Path | What |
|------|------|
| [`src/Plugin.ViewEffects`](src/Plugin.ViewEffects) | The plugin (NuGet package). See its [README](src/Plugin.ViewEffects/README.md) for full usage. |
| [`samples/Plugin.ViewEffects.Sample`](samples/Plugin.ViewEffects.Sample) | A MAUI test harness to try every animation. |

## Build

```
dotnet build src/Plugin.ViewEffects/Plugin.ViewEffects.csproj -c Release
```

Targets `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, and `net10.0-windows`.

## License

MIT
