using Avalonia.Interactivity;

namespace Estragonia.Input;

/// <summary>Provides information about a joypad button event.</summary>
public class JoypadButtonEventArgs(RoutedEvent? routedEvent, object? source) : RoutedEventArgs(routedEvent, source);
