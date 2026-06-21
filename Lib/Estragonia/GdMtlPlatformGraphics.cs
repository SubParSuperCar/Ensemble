using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Platform;

namespace Estragonia;

/// <summary>Godot Metal-based <see cref="IPlatformGraphics" /> implementation.</summary>
internal sealed class GodotMtlPlatformGraphics : IGodotPlatformGraphics
{
	private GodotMtlSkiaGpu? _context;
	private int _refCount;

	bool IPlatformGraphics.UsesSharedContext
		=> true;

	public IGodotSkiaGpu GetSharedContext()
	{
		if (Volatile.Read(ref _refCount) == 0)
			ThrowDisposed();

		if (_context is not null && !_context.IsLost) return _context;
		_context?.Dispose();
		_context = null;
		_context = new GodotMtlSkiaGpu();

		return _context;
	}

	IPlatformGraphicsContext IPlatformGraphics.CreateContext() => throw new NotSupportedException();

	IPlatformGraphicsContext IPlatformGraphics.GetSharedContext() => GetSharedContext();

	public void AddRef() => Interlocked.Increment(ref _refCount);

	public void Release()
	{
		if (Interlocked.Decrement(ref _refCount) == 0)
			Dispose();
	}

	public void Dispose()
	{
		if (_context is null) return;
		_context.Dispose();
		_context = null;
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowDisposed() => throw new ObjectDisposedException(nameof(GodotMtlPlatformGraphics));
}
