using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using AvaloniaEdit.Editing;

namespace Root.Common.Input;

public static class InputSink
{
	private static readonly AnonymousObserver<(object, RoutedEventArgs)> FocusObserver = new(OnFocusChanged);

	static InputSink()
	{
		InputElement.GotFocusEvent.Raised.Subscribe(FocusObserver);
		InputElement.LostFocusEvent.Raised.Subscribe(FocusObserver);
	}

	public static bool IsSunk { get; private set; }

	private static void OnFocusChanged((object Sender, RoutedEventArgs Args) value)
	{
		if (value.Args is FocusChangedEventArgs focus)
			IsSunk = focus.NewFocusedElement is TextBox or TextArea or NativeWebView;
	}
}
