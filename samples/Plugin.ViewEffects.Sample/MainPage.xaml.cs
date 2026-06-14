using Plugin.ViewEffects;

namespace Plugin.ViewEffects.Sample;

public partial class MainPage : ContentPage
{
    bool _busy;

    public MainPage()
    {
        InitializeComponent();
        AnimationPicker.SelectedIndex = 3;   // Shatter (so the origin picker is relevant by default)
        OriginPicker.SelectedIndex = 4;      // Centre
        SidePicker.SelectedIndex = 0;        // Left
        UpdateExtraControls();
    }

    bool IsTennis => AnimationPicker.SelectedIndex == 6;

    RemovalAnimation SelectedAnimation => AnimationPicker.SelectedIndex switch
    {
        0 => RemovalAnimation.Freeze,
        1 => RemovalAnimation.Melt,
        2 => RemovalAnimation.Explode,
        4 => RemovalAnimation.Dematerialise,
        5 => RemovalAnimation.Plughole,
        6 => RemovalAnimation.TennisDisappear,
        _ => RemovalAnimation.Shatter,
    };

    ShatterOrigin SelectedOrigin => OriginPicker.SelectedIndex switch
    {
        0 => ShatterOrigin.TopLeft,
        1 => ShatterOrigin.TopCentre,
        2 => ShatterOrigin.TopRight,
        3 => ShatterOrigin.Left,
        5 => ShatterOrigin.Right,
        6 => ShatterOrigin.BottomLeft,
        7 => ShatterOrigin.BottomCentre,
        8 => ShatterOrigin.BottomRight,
        _ => ShatterOrigin.Centre,
    };

    TennisSide SelectedSide => SidePicker.SelectedIndex == 1 ? TennisSide.Right : TennisSide.Left;

    double Seconds => DurationStepper.Value;

    void OnAnimationPickerChanged(object? sender, EventArgs e) => UpdateExtraControls();

    void UpdateExtraControls()
    {
        OriginSection.IsVisible = SelectedAnimation == RemovalAnimation.Shatter;
        SideSection.IsVisible = IsTennis;
        MaterialiseButton.Text = IsTennis ? "Appear ▶" : "Materialise ✨";
    }

    void OnDurationChanged(object? sender, ValueChangedEventArgs e)
        => DurationLabel.Text = $"{e.NewValue:0.0}s";

    async void OnAnimateClicked(object? sender, EventArgs e)
    {
        if (_busy) return;
        if (!Stage.Contains(DemoCard))
        {
            Status("Stage is empty — Reset or Appear first.");
            return;
        }

        if (IsTennis)
        {
            await RunAsync($"Tennis disappear from {SelectedSide} ({Seconds:0.0}s)…",
                DemoCard.TennisDisappearAsync(SelectedSide, Seconds));
            Status($"Tennis disappear done — view removed.");
            return;
        }

        var animation = SelectedAnimation;
        await RunAsync($"Playing {animation} ({Seconds:0.0}s)…", animation switch
        {
            RemovalAnimation.Freeze => DemoCard.FreezeAsync(Seconds),
            RemovalAnimation.Melt => DemoCard.MeltAsync(Seconds),
            RemovalAnimation.Explode => DemoCard.ExplodeAsync(Seconds),
            RemovalAnimation.Dematerialise => DemoCard.DematerialiseAsync(Seconds),
            RemovalAnimation.Plughole => DemoCard.PlugholeAsync(Seconds),
            _ => DemoCard.ShatterAsync(SelectedOrigin, Seconds),
        });
        Status($"{animation} done — view removed.");
    }

    async void OnMaterialiseClicked(object? sender, EventArgs e)
    {
        if (_busy) return;
        if (!Stage.Contains(DemoCard))
            Stage.Add(DemoCard);   // must be in the tree first

        if (IsTennis)
        {
            await RunAsync($"Tennis appear from {SelectedSide} ({Seconds:0.0}s)…",
                DemoCard.TennisAppearAsync(SelectedSide, Seconds));
            Status("Tennis appear done.");
            return;
        }

        await RunAsync($"Materialising ({Seconds:0.0}s)…", DemoCard.MaterialiseAsync(Seconds));
        Status("Materialised.");
    }

    void OnResetClicked(object? sender, EventArgs e)
    {
        if (_busy) return;
        if (!Stage.Contains(DemoCard))
            Stage.Add(DemoCard);
        DemoCard.Opacity = 1;
        DemoCard.TranslationX = DemoCard.TranslationY = 0;
        Status("Reset.");
    }

    async Task RunAsync(string status, Task work)
    {
        _busy = true;
        SetButtons(false);
        Status(status);
        try { await work; }
        catch (Exception ex) { Status($"Error: {ex.Message}"); }
        finally
        {
            _busy = false;
            SetButtons(true);
        }
    }

    void SetButtons(bool enabled)
        => AnimateButton.IsEnabled = MaterialiseButton.IsEnabled = ResetButton.IsEnabled = enabled;

    void Status(string text) => StatusLabel.Text = text;
}
