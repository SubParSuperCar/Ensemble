namespace Root.Common.Time;

public static class DateTime
{
	public static WrappedTimeProvider TimeProvider { get; } = new();
}
