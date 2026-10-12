using System.Runtime.CompilerServices;
using Godot;

namespace EnsembleRoot.Common.Utils;

/// <summary>Completes the next time a parameterless signal is emitted.</summary>
/// <remarks>
///     Like Godot's <see cref="SignalAwaiter" />, this resumes the awaiting method directly within the emission, on the
///     emitting (usually main) thread. A task-based awaiter would resume on a captured context (Avalonia's stops
///     running once the UI is freed at quit), or on the thread pool, since .NET doesn't inline under a custom context.
/// </remarks>
public sealed class SignalAwaitable : INotifyCompletion
{
	private Action? _continuation;

	internal SignalAwaitable(GodotObject source, StringName signal)
	{
		source.Connect(signal, Callable.From(OnSignal), (uint)GodotObject.ConnectFlags.OneShot);
	}

	public bool IsCompleted { get; private set; }

	public void OnCompleted(Action continuation) => _continuation = continuation;

	public SignalAwaitable GetAwaiter() => this;

	public void GetResult() { }

	private void OnSignal()
	{
		IsCompleted = true;
		_continuation?.Invoke();
	}
}
