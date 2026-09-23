namespace Root.Common.Time;

public sealed class WrappedTimeProvider : TimeProvider
{
	public TimeProvider Source { get; set; } = System;

	public override TimeZoneInfo LocalTimeZone => Source.LocalTimeZone;
	public override long TimestampFrequency => Source.TimestampFrequency;

	public override DateTimeOffset GetUtcNow() => Source.GetUtcNow();
	public override long GetTimestamp() => Source.GetTimestamp();

	public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
		Source.CreateTimer(callback, state, dueTime, period);
}
