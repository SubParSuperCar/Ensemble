using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Threading;

namespace EnsembleRoot.Ui.Controls;

/// <summary>Briefly swaps a button's foreground and background, so each use shows, by click or keybind alike.</summary>
public sealed class PressFlash
{
	private static readonly TimeSpan Duration = TimeSpan.FromSeconds(1 / 8d);

	private readonly Button _button;
	private readonly DispatcherTimer _timer = new() { Interval = Duration };

	private IDisposable? _background;
	private IDisposable? _foreground;

	public PressFlash(Button button)
	{
		_button = button;
		_timer.Tick += OnTimerTick;
	}

	public void Flash()
	{
		_timer.Stop();
		_timer.Start();

		// Already swapped, which a second swap would undo
		if (_background is not null)
			return;

		var (background, foreground) = (_button.Background, _button.Foreground);

		_background = _button.SetValue(TemplatedControl.BackgroundProperty, foreground, BindingPriority.Animation);
		_foreground = _button.SetValue(TemplatedControl.ForegroundProperty, background, BindingPriority.Animation);
	}

	private void OnTimerTick(object? sender, EventArgs e)
	{
		_timer.Stop();

		_background?.Dispose();
		_foreground?.Dispose();

		_background = null;
		_foreground = null;
	}
}
