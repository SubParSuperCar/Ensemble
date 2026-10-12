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
		///     Replaces awaiting <see cref="GodotObject.ToSignal" />, which works, but Rider reports its
		///     <see cref="SignalAwaiter" /> as not awaitable.
		/// </remarks>
		public Task ToSignalAsync(StringName signal)
		{
			var completion = new TaskCompletionSource();

			source.Connect(signal, Callable.From(completion.TrySetResult), (uint)GodotObject.ConnectFlags.OneShot);

			return completion.Task;
		}
	}
}
