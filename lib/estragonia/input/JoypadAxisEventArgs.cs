using Avalonia.Interactivity;

namespace Estragonia.Input;

/// <summary>Provides information about a joypad axis event.</summary>
public class JoypadAxisEventArgs(RoutedEvent? routedEvent, object? source) : RoutedEventArgs(routedEvent, source);
