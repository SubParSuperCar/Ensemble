using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using EnsembleRoot.Common.Logging;
using EnsembleRoot.Ui.Impl.Abstractions;
using Serilog.Events;

namespace EnsembleRoot.Ui.Impl.ViewModels;

/// <summary>Shows warnings and worse as toasts, newest first, merging repeats of the same message.</summary>
public sealed class ToastListViewModel : ViewModelBase
{
	private const LogEventLevel MinimumLevel = LogEventLevel.Warning;
	private const int MaxCount = 32;

	private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(10);

	public ToastListViewModel()
	{
		VolatileLogHistorySink.Emitted += OnLogEmitted;
	}

	public ObservableCollection<ToastViewModel> Toasts { get; } = [];

	protected override void OnDispose()
	{
		VolatileLogHistorySink.Emitted -= OnLogEmitted;

		foreach (var toast in Toasts)
			toast.Dispose();

		Toasts.Clear();
	}

	// Dismissed toasts stay shown while they fade out, then are removed and disposed
	internal void Dismiss(ToastViewModel toast)
	{
		if (toast.IsDismissing)
			return;

		toast.IsDismissing = true;
		toast.Dispose();

		if (Application.Current?.FindResource("TransitionDuration") is TimeSpan duration)
			DispatcherTimer.RunOnce(() => Toasts.Remove(toast), duration);
		else
			Toasts.Remove(toast);
	}

	private void OnLogEmitted(LogEntry entry)
	{
		if (entry.Level >= MinimumLevel)
			Dispatcher.UIThread.Post(() => Show(entry));
	}

	private void Show(LogEntry entry)
	{
		if (IsDisposed)
			return;

		if (Toasts.FirstOrDefault(other => !other.IsDismissing && other.IsRepeatOf(entry)) is { } repeated)
		{
			repeated.Repeat();
			Toasts.Move(Toasts.IndexOf(repeated), 0);
			return;
		}

		Toasts.Insert(0, new ToastViewModel(this, entry, Lifetime));

		foreach (var oldest in Toasts.Where(static other => !other.IsDismissing).Skip(MaxCount).ToArray())
			Dismiss(oldest);
	}
}
