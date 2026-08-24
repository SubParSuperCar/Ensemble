using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Reactive;

namespace Root.Common.Input;

public static class InputSink
{
	static InputSink()
	{
		InputElement.GotFocusEvent.Raised.Subscribe(new AnonymousObserver<(object, RoutedEventArgs)>(OnFocusChanged));
		InputElement.LostFocusEvent.Raised.Subscribe(new AnonymousObserver<(object, RoutedEventArgs)>(OnFocusChanged));
	}

	public static bool IsSunk { get; private set; }

	private static void OnFocusChanged((object Sender, RoutedEventArgs Args) value)
	{
		if (value.Args is FocusChangedEventArgs focus)
			IsSunk = focus.NewFocusedElement is TextBox;
	}
}
