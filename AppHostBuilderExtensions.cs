using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

namespace Plugin.ViewEffects;

/// <summary>Options for <see cref="AppHostBuilderExtensions.UseViewEffects"/>.</summary>
public sealed class ViewEffectsOptions
{
    /// <summary>
    /// The default animation length, in seconds, used whenever a duration is not specified. Defaults
    /// to <see cref="ViewEffects.DefaultSeconds"/> (3).
    /// </summary>
    public double DefaultDurationSeconds { get; set; } = ViewEffects.DefaultSeconds;
}

/// <summary>
/// Registers Plugin.ViewEffects with a MAUI app. Call <see cref="UseViewEffects"/> from
/// <c>MauiProgram.CreateMauiApp</c>:
/// <code>
/// builder
///     .UseMauiApp&lt;App&gt;()
///     .UseViewEffects(options => options.DefaultDurationSeconds = 2.5);
/// </code>
/// </summary>
public static class AppHostBuilderExtensions
{
    /// <summary>
    /// Initialises Plugin.ViewEffects and applies optional <see cref="ViewEffectsOptions"/>.
    /// </summary>
    /// <param name="builder">The MAUI app builder.</param>
    /// <param name="configure">Optional configuration callback.</param>
    /// <returns>The same <paramref name="builder"/>, for chaining.</returns>
    public static MauiAppBuilder UseViewEffects(this MauiAppBuilder builder,
                                                Action<ViewEffectsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new ViewEffectsOptions();
        configure?.Invoke(options);

        if (options.DefaultDurationSeconds > 0)
            ViewEffects.ConfiguredDefaultSeconds = options.DefaultDurationSeconds;

        builder.Services.AddSingleton(options);
        return builder;
    }
}
