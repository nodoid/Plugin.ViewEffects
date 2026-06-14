# Plugin.ViewEffects

A .NET MAUI plugin that **removes a `View` from its parent's visual tree** when a condition is met
(rather than just collapsing it like `IsVisible`), optionally playing a removal animation first — plus
matching **reveal** animations to bring a view in.

Animations: **Freeze**, **Melt**, **Explode**, **Shatter** (glass, 9-point origin), **Dematerialise**,
**Plughole**, **Tennis Disappear** — and the reveals **Materialise** and **Tennis Appear**.

## Documentation

Full docs are in [`docs/`](docs/index.md):

- [Getting started](docs/getting-started.md)
- [Animations](docs/animations.md)
- [Usage](docs/usage.md)
- [Configuration](docs/configuration.md)
- [API reference](docs/api-reference.md)
- [How it works](docs/how-it-works.md)

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

DILLIGAF — see [LICENSE.txt](https://github.com/nodoid/Plugin.ViewEffects/blob/main/src/Plugin.ViewEffects/LICENSE.txt). Do whatever you like; no warranty.
