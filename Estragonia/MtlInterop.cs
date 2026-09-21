using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Godot;
using SkiaSharp;

namespace Estragonia;

/// <summary>Native interop for the SkiaSharp Metal functions and GPU texture blitting.</summary>
internal static class MtlInterop
{
	private const string SkiaLibrary = "libSkiaSharp";
	private const string ObjcLibrary = "/usr/lib/libobjc.A.dylib";

	// Cached Objective-C selectors for the Metal operations used by BlitTexture
	private static IntPtr _selCommandBuffer;
	private static IntPtr _selBlitCommandEncoder;
	private static IntPtr _selCopyFromTexture;
	private static IntPtr _selEndEncoding;
	private static IntPtr _selCommit;
	private static IntPtr _selWaitUntilCompleted;

	/// <summary>Creates a <see cref="GRContext" /> from a native Metal device and command queue.</summary>
	/// <param name="device">The MTLDevice handle.</param>
	/// <param name="queue">The MTLCommandQueue handle.</param>
	/// <returns>The created context, or <c>null</c> on failure.</returns>
	public static GRContext? CreateMetalContext(IntPtr device, IntPtr queue)
	{
		IntPtr handle;

		try
		{
			handle = gr_direct_context_make_metal(device, queue);
		}
		catch
		{
			return null;
		}

		return handle == IntPtr.Zero ? null : CreateGrContextFromHandle(handle);
	}

	/// <summary>Creates a <see cref="GRBackendTexture" /> wrapping an existing Metal texture.</summary>
	/// <param name="width">The width of the texture in pixels.</param>
	/// <param name="height">The height of the texture in pixels.</param>
	/// <param name="mipmapped">Whether the texture is mipmapped.</param>
	/// <param name="mtlTexture">The MTLTexture handle.</param>
	/// <returns>The created backend texture, or <c>null</c> on failure.</returns>
	public static GRBackendTexture? CreateMetalBackendTexture(
		int width,
		int height,
		bool mipmapped,
		IntPtr mtlTexture
	)
	{
		var textureInfo = new GrMtlTextureInfoNative { Texture = mtlTexture };
		var handle = gr_backendtexture_new_metal(width, height, mipmapped, ref textureInfo);

		return handle == IntPtr.Zero ? null : CreateGrBackendTextureFromHandle(handle);
	}

	/// <summary>Creates a <see cref="GRBackendRenderTarget" /> wrapping an existing Metal texture.</summary>
	/// <param name="width">The width of the render target in pixels.</param>
	/// <param name="height">The height of the render target in pixels.</param>
	/// <param name="mtlTexture">The MTLTexture handle.</param>
	/// <returns>The created render target, or <c>null</c> on failure.</returns>
	// ReSharper disable once UnusedMember.Global
	public static GRBackendRenderTarget? CreateMetalRenderTarget(int width, int height, IntPtr mtlTexture)
	{
		var textureInfo = new GrMtlTextureInfoNative { Texture = mtlTexture };
		var handle = gr_backendrendertarget_new_metal(width, height, ref textureInfo);

		return handle == IntPtr.Zero ? null : CreateGrBackendRenderTargetFromHandle(handle);
	}

	/// <summary>Performs a GPU-to-GPU texture blit through a Metal command buffer.</summary>
	/// <returns><c>true</c> if the blit was submitted and completed, <c>false</c> otherwise.</returns>
	public static bool BlitTexture(IntPtr commandQueue, IntPtr sourceTexture, IntPtr destTexture, int width, int height)
	{
		if (commandQueue == IntPtr.Zero || sourceTexture == IntPtr.Zero || destTexture == IntPtr.Zero)
			return false;

		try
		{
			EnsureSelectorsInitialized();

			var commandBuffer = objc_msgSend(commandQueue, _selCommandBuffer);

			if (commandBuffer == IntPtr.Zero)
			{
				GD.PrintErr("[Estragonia Metal] Couldn't create a command buffer");
				return false;
			}

			var blitEncoder = objc_msgSend(commandBuffer, _selBlitCommandEncoder);

			if (blitEncoder == IntPtr.Zero)
			{
				GD.PrintErr("[Estragonia Metal] Couldn't create a blit encoder");
				return false;
			}

			var origin = MtlOrigin.Zero;
			var size = MtlSize.Create(width, height);

			objc_msgSend_blit(
				blitEncoder,
				_selCopyFromTexture,
				sourceTexture,
				0,
				0,
				origin,
				size,
				destTexture,
				0,
				0,
				origin
			);

			objc_msgSend_void(blitEncoder, _selEndEncoding);
			objc_msgSend_void(commandBuffer, _selCommit);
			objc_msgSend_void(commandBuffer, _selWaitUntilCompleted);

			return true;
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[Estragonia Metal] BlitTexture failed: {ex.Message}");
			return false;
		}
	}

	/// <summary>Gets the Metal texture handle from a Skia surface's backend texture.</summary>
	/// <param name="surface">The Skia surface.</param>
	/// <returns>The MTLTexture handle, or <see cref="IntPtr.Zero" /> if it couldn't be retrieved.</returns>
	public static IntPtr GetSurfaceMetalTexture(SKSurface surface)
	{
		try
		{
			var surfaceHandle = GetSkObjectHandle(surface);

			if (surfaceHandle == IntPtr.Zero)
				return IntPtr.Zero;

			// Mode 1 is kFlushRead: flush and submit the pending work before reading the texture
			var backendTexture = sk_surface_get_backend_texture(surfaceHandle, 1);

			return backendTexture == IntPtr.Zero ? IntPtr.Zero : GetMetalTextureFromBackend(backendTexture);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[Estragonia Metal] GetSurfaceMetalTexture failed: {ex.Message}");
			return IntPtr.Zero;
		}
	}

	private static void EnsureSelectorsInitialized()
	{
		if (_selCommandBuffer != IntPtr.Zero)
			return;

		const string copyFromTexture = "copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:"
			+ "toTexture:destinationSlice:destinationLevel:destinationOrigin:";

		_selCommandBuffer = sel_registerName("commandBuffer");
		_selBlitCommandEncoder = sel_registerName("blitCommandEncoder");
		_selCopyFromTexture = sel_registerName(copyFromTexture);
		_selEndEncoding = sel_registerName("endEncoding");
		_selCommit = sel_registerName("commit");
		_selWaitUntilCompleted = sel_registerName("waitUntilCompleted");
	}

	private static IntPtr GetMetalTextureFromBackend(IntPtr backendTexture) =>
		gr_backendtexture_get_mtl_textureinfo(backendTexture, out var info) ? info.Texture : IntPtr.Zero;

	private static IntPtr GetSkObjectHandle(SKObject obj)
	{
		var handleProperty = typeof(SKObject).GetProperty("Handle", BindingFlags.Public | BindingFlags.Instance);

		return handleProperty?.GetValue(obj) as IntPtr? ?? IntPtr.Zero;
	}

	/// <summary>Wraps a native handle in a <see cref="GRContext" /> through its non-public constructors.</summary>
	private static GRContext? CreateGrContextFromHandle(IntPtr handle)
	{
		try
		{
			var constructors = typeof(GRContext).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

			if (FindConstructor(constructors, typeof(IntPtr), typeof(bool)) is { } ownedConstructor)
				return ownedConstructor.Invoke([handle, true]) as GRContext;

			if (FindConstructor(constructors, typeof(IntPtr)) is { } constructor)
				return constructor.Invoke([handle]) as GRContext;

			return null;
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	///     Wraps a native handle in a <see cref="GRBackendTexture" /> through its non-public constructors.
	/// </summary>
	private static GRBackendTexture? CreateGrBackendTextureFromHandle(IntPtr handle)
	{
		try
		{
			var constructors = typeof(GRBackendTexture).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

			if (FindConstructor(constructors, typeof(IntPtr), typeof(bool)) is { } ownedConstructor)
				return ownedConstructor.Invoke([handle, true]) as GRBackendTexture;

			if (FindConstructor(constructors, typeof(IntPtr)) is { } constructor)
				return constructor.Invoke([handle]) as GRBackendTexture;

			gr_backendtexture_delete(handle);
			return null;
		}
		catch
		{
			gr_backendtexture_delete(handle);
			return null;
		}
	}

	/// <summary>
	///     Wraps a native handle in a <see cref="GRBackendRenderTarget" /> through its non-public constructors.
	/// </summary>
	private static GRBackendRenderTarget? CreateGrBackendRenderTargetFromHandle(IntPtr handle)
	{
		var constructors = typeof(GRBackendRenderTarget).GetConstructors(
			BindingFlags.NonPublic | BindingFlags.Instance
		);

		if (FindConstructor(constructors, typeof(IntPtr), typeof(bool)) is { } ownedConstructor)
			return ownedConstructor.Invoke([handle, true]) as GRBackendRenderTarget;

		if (FindConstructor(constructors, typeof(IntPtr)) is { } constructor)
			return constructor.Invoke([handle]) as GRBackendRenderTarget;

		gr_backendrendertarget_delete(handle);
		return null;
	}

	private static ConstructorInfo? FindConstructor(ConstructorInfo[] constructors, params Type[] parameterTypes) =>
		constructors.FirstOrDefault(constructor => HasParameterTypes(constructor, parameterTypes));

	private static bool HasParameterTypes(ConstructorInfo constructor, Type[] parameterTypes) =>
		constructor.GetParameters().Select(static parameter => parameter.ParameterType).SequenceEqual(parameterTypes);

	/// <summary>The native GrMtlTextureInfo structure.</summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct GrMtlTextureInfoNative
	{
		public IntPtr Texture;
	}

	/// <summary>The native MTLOrigin structure.</summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct MtlOrigin
	{
		public ulong X;
		public ulong Y;
		public ulong Z;

		public static MtlOrigin Zero => new() { X = 0, Y = 0, Z = 0 };
	}

	/// <summary>The native MTLSize structure.</summary>
	[StructLayout(LayoutKind.Sequential)]
	private struct MtlSize
	{
		public ulong Width;
		public ulong Height;
		public ulong Depth;

		public static MtlSize Create(int width, int height) =>
			new() { Width = (ulong)width, Height = (ulong)height, Depth = 1 };
	}

	// These stay on DllImport rather than LibraryImport: converting the objc_msgSend overloads and the
	// by-value struct arguments needs a per-signature review that can't be validated without a macOS machine
#pragma warning disable SYSLIB1054

	[DllImport(SkiaLibrary, CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr gr_direct_context_make_metal(IntPtr device, IntPtr queue);

	[DllImport(SkiaLibrary, CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr gr_backendtexture_new_metal(
		int width,
		int height,
		bool mipmapped,
		ref GrMtlTextureInfoNative mtlInfo
	);

	[DllImport(SkiaLibrary, CallingConvention = CallingConvention.Cdecl)]
	private static extern void gr_backendtexture_delete(IntPtr handle);

	[DllImport(SkiaLibrary, CallingConvention = CallingConvention.Cdecl)]
	private static extern bool gr_backendtexture_get_mtl_textureinfo(IntPtr texture, out GrMtlTextureInfoNative info);

	[DllImport(SkiaLibrary, CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr gr_backendrendertarget_new_metal(
		int width,
		int height,
		ref GrMtlTextureInfoNative mtlInfo
	);

	[DllImport(SkiaLibrary, CallingConvention = CallingConvention.Cdecl)]
	private static extern void gr_backendrendertarget_delete(IntPtr handle);

	[DllImport(SkiaLibrary, CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr sk_surface_get_backend_texture(IntPtr surface, int mode);

	[DllImport(ObjcLibrary, EntryPoint = "sel_registerName", CharSet = CharSet.Ansi, BestFitMapping = false)]
	private static extern IntPtr sel_registerName(string name);

	[DllImport(ObjcLibrary, EntryPoint = "objc_msgSend")]
	private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

	[DllImport(ObjcLibrary, EntryPoint = "objc_msgSend")]
	private static extern void objc_msgSend_void(IntPtr receiver, IntPtr selector);

	[DllImport(ObjcLibrary, EntryPoint = "objc_msgSend")]
	private static extern void objc_msgSend_blit(
		IntPtr receiver,
		IntPtr selector,
		IntPtr sourceTexture,
		ulong sourceSlice,
		ulong sourceLevel,
		MtlOrigin sourceOrigin,
		MtlSize sourceSize,
		IntPtr destTexture,
		ulong destSlice,
		ulong destLevel,
		MtlOrigin destOrigin
	);

#pragma warning restore SYSLIB1054
}
