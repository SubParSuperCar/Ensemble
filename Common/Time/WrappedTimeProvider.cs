namespace Root.Common.Time;

public class WrappedTimeProvider : TimeProvider
{
	public TimeProvider Source { get; set; } = System;

	public override DateTimeOffset GetUtcNow() => Source.GetUtcNow();
	public new DateTimeOffset GetLocalNow() => Source.GetLocalNow();
}
