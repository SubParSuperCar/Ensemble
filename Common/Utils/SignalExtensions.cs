using Godot;

namespace EnsembleRoot.Common.Utils;

public static class SignalExtensions
{
	extension(GodotObject source)
	{
		/// <summary>
		///     Completes the next time <paramref name="signal" />, which must have no parameters, is emitted.
		/// </summary>
		/// <remarks>
		///     Replaces awaiting <see cref="GodotObject.ToSignal" />, which works the same way, but Rider reports its
		///     <see cref="SignalAwaiter" /> as not awaitable.
		/// </remarks>
		public SignalAwaitable ToSignalAsync(StringName signal) => new(source, signal);
	}
}
