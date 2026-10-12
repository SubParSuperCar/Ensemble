using System.Runtime.CompilerServices;
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
		///     <see cref="SignalAwaiter" /> as not awaitable. Like it, this resumes within the emission, not on a
		///     captured context, which may never run again (e.g., Avalonia's once the UI is freed at quit).
		/// </remarks>
		public ConfiguredTaskAwaitable ToSignalAsync(StringName signal)
		{
			var completion = new TaskCompletionSource();

			source.Connect(signal, Callable.From(completion.TrySetResult), (uint)GodotObject.ConnectFlags.OneShot);

			return completion.Task.ConfigureAwait(false);
		}
	}
}
