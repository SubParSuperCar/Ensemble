namespace EnsembleRoot.Common.Time;

/// <summary>Shifts another provider's current time by an offset. Timestamps and timers aren't affected.</summary>
public sealed class OffsetTimeProvider(TimeProvider source, TimeSpan offset) : TimeProvider
{
	public override TimeZoneInfo LocalTimeZone => source.LocalTimeZone;
	public override long TimestampFrequency => source.TimestampFrequency;

	public override DateTimeOffset GetUtcNow() => source.GetUtcNow() + offset;
	public override long GetTimestamp() => source.GetTimestamp();

	public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
		source.CreateTimer(callback, state, dueTime, period);
}
