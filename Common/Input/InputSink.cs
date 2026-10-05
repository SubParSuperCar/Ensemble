namespace EnsembleRoot.Common.Input;

public static class InputSink
{
	public static readonly OwnershipFlag Sink = new();

	public static bool IsSunk => Sink.IsSet;
}
