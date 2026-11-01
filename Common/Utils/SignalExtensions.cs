using Godot;

namespace EnsembleRoot.Common.Utils;

public static class SignalExtensions
{
	/// <summary>
	///     Completes the next time <paramref name="signal" />, which must have no parameters, is emitted.
	/// </summary>
	/// <remarks>
	///     Replaces awaiting <see cref="GodotObject.ToSignal" />, which works the same way, but Rider reports its
	///     <see cref="SignalAwaiter" /> as not awaitable.
	/// </remarks>
	public static SignalAwaitable ToSignalAsync(this GodotObject source, StringName signal) => new(source, signal);
}
