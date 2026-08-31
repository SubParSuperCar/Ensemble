using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using AvaloniaEdit.Editing;

namespace Root.Common.Input;

public static class InputSink
{
	private static readonly object Token = new();

	static InputSink()
	{
		var observer = new AnonymousObserver<(object, RoutedEventArgs)>(OnFocusChanged);
		InputElement.GotFocusEvent.Raised.Subscribe(observer);
		InputElement.LostFocusEvent.Raised.Subscribe(observer);
	}

	public static OwnershipFlag Sink { get; } = new();
	public static bool IsSunk => Sink.IsSet;

	private static void OnFocusChanged((object Sender, RoutedEventArgs Args) value)
	{
		if (value.Args is not FocusChangedEventArgs focus)
			return;

		if (focus.NewFocusedElement is TextBox or TextArea)
			Sink.Acquire(Token);
		else
			Sink.Release(Token);
	}
}
