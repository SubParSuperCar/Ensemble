using System;

namespace Estragonia;

/// <summary>A reusable <see cref="IDisposable" /> implementation that does nothing when disposed.</summary>
internal sealed class EmptyDisposable : IDisposable
{
	private EmptyDisposable()
	{
	}

	public static EmptyDisposable Instance { get; } = new();

	public void Dispose()
	{
	}
}
