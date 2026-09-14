using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Platform;

namespace Estragonia;

/// <summary>Godot Vulkan-based <see cref="IPlatformGraphics" /> implementation.</summary>
internal sealed class GodotVkPlatformGraphics : IGodotPlatformGraphics
{
	private GodotVkSkiaGpu? _context;
	private int _refCount;

	bool IPlatformGraphics.UsesSharedContext => true;

	IPlatformGraphicsContext IPlatformGraphics.CreateContext() => throw new NotSupportedException();

	IPlatformGraphicsContext IPlatformGraphics.GetSharedContext() => GetSharedContext();

	public IGodotSkiaGpu GetSharedContext()
	{
		if (Volatile.Read(ref _refCount) == 0)
			ThrowDisposed();

		// ReSharper disable once InvertIf -- inverting would duplicate the return of the cached context
		if (_context is null || _context.IsLost)
		{
			_context?.Dispose();
			_context = null;
			_context = new GodotVkSkiaGpu();
		}

		return _context;
	}

	public void AddRef() => Interlocked.Increment(ref _refCount);

	public void Release()
	{
		if (Interlocked.Decrement(ref _refCount) == 0)
			Dispose();
	}

	public void Dispose()
	{
		if (_context is null)
			return;

		_context.Dispose();
		_context = null;
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowDisposed() => throw new ObjectDisposedException(nameof(GodotVkPlatformGraphics));
}
