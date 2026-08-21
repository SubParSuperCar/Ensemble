using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Platform;

namespace Estragonia;

/// <summary>Godot Vulkan-based <see cref="IPlatformGraphics" /> implementation.</summary>
internal sealed class GodotVkPlatformGraphics : IPlatformGraphics, IDisposable
{
	private GodotVkSkiaGpu? _context;
	private int _refCount;


	public void Dispose()
	{
		if (_context is null) return;
		_context.Dispose();
		_context = null;
	}

	bool IPlatformGraphics.UsesSharedContext => true;

	IPlatformGraphicsContext IPlatformGraphics.CreateContext() => throw new NotSupportedException();

	IPlatformGraphicsContext IPlatformGraphics.GetSharedContext() => GetSharedContext();

	public GodotVkSkiaGpu GetSharedContext()
	{
		if (Volatile.Read(ref _refCount) == 0)
			ThrowDisposed();

		if (_context is not null && !_context.IsLost) return _context;
		_context?.Dispose();
		_context = null;
		_context = new GodotVkSkiaGpu();

		return _context;
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ThrowDisposed() => throw new ObjectDisposedException(nameof(GodotVkPlatformGraphics));

	public void AddRef() => Interlocked.Increment(ref _refCount);

	public void Release()
	{
		if (Interlocked.Decrement(ref _refCount) == 0)
			Dispose();
	}
}
