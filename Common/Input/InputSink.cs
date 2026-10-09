using EnsembleRoot.Common.Utils;

namespace EnsembleRoot.Common.Input;

/// <summary>
///     Holds game input back (e.g., while a text box has focus), so typing doesn't also move the character.
/// </summary>
public static class InputSink
{
	public static readonly OwnershipFlag Sink = new();

	public static bool IsSunk => Sink.IsSet;
}
