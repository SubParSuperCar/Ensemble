namespace Root.Common.Time;

public sealed class WrappedTimeProvider : TimeProvider
{
	public TimeProvider Source { get; set; } = System;

	public override DateTimeOffset GetUtcNow() => Source.GetUtcNow();
	public new DateTimeOffset GetLocalNow() => Source.GetLocalNow();
}
