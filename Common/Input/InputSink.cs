using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using AvaloniaEdit.Editing;

namespace Root.Common.Input;

public static class InputSink
{
	private static readonly object Token = new();
	private static readonly AnonymousObserver<(object, RoutedEventArgs)> FocusObserver = new(OnFocusChanged);

	static InputSink()
	{
		InputElement.GotFocusEvent.Raised.Subscribe(FocusObserver);
		InputElement.LostFocusEvent.Raised.Subscribe(FocusObserver);
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
