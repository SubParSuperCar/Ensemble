namespace Root.Shared.Input;

public static class InputExtensions
{
	public static OwnershipFlag Sink { get; } = new();
	public static bool IsSunk => Sink.IsSet;
}
