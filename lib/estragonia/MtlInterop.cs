using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;
using SkiaSharp;

namespace Estragonia;

/// <summary>
///     Native interop for SkiaSharp Metal functions and GPU texture blitting.
/// </summary>
internal static partial class MtlInterop
{
	private const string SkiaLibrary = "libSkiaSharp";
	private const string ObjcLibrary = "/usr/lib/libobjc.A.dylib";

	/// <summary>
	///     Creates a GRContext for Metal.
	/// </summary>
	/// <param name="device">The MTLDevice handle.</param>
	/// <param name="queue">The MTLCommandQueue handle.</param>
	/// <returns>A handle to the GRContext, or IntPtr.Zero on failure.</returns>
	[LibraryImport(SkiaLibrary)]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	private static partial IntPtr gr_direct_context_make_metal(IntPtr device, IntPtr queue);

	/// <summary>
	///     Creates a GRBackendTexture for Metal.
	/// </summary>
	[LibraryImport(SkiaLibrary)]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	private static partial IntPtr gr_backendtexture_new_metal(int width, int height,
		[MarshalAs(UnmanagedType.Bool)] bool mipmapped,
		ref GrMtlTextureInfoNative mtlInfo);

	/// <summary>
	///     Deletes a GRBackendTexture.
	/// </summary>
	[LibraryImport(SkiaLibrary)]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	private static partial void gr_backendtexture_delete(IntPtr handle);

	/// <summary>
	///     Creates a GRBackendTexture for a Metal texture.
	/// </summary>
	public static GRBackendTexture? CreateMetalBackendTexture(int width, int height, bool mipmapped, IntPtr mtlTexture)
	{
		var textureInfo = new GrMtlTextureInfoNative { Texture = mtlTexture };
		var handle = gr_backendtexture_new_metal(width, height, mipmapped, ref textureInfo);
		return handle == IntPtr.Zero ? null : CreateGrBackendTextureFromHandle(handle);
	}

	/// <summary>
	///     Creates a GRBackendTexture from a native handle using reflection.
	/// </summary>
	private static GRBackendTexture? CreateGrBackendTextureFromHandle(IntPtr handle)
	{
		try
		{
			var ctors = typeof(GRBackendTexture).GetConstructors(
				BindingFlags.NonPublic | BindingFlags.Instance
			);

			// Try (IntPtr, bool) constructor
			var ctor = ctors.FirstOrDefault(c =>
			{
				var ps = c.GetParameters();
				return ps.Length == 2 &&
					   ps[0].ParameterType == typeof(IntPtr) &&
					   ps[1].ParameterType == typeof(bool);
			});

			if (ctor is not null)
				return ctor.Invoke([handle, true]) as GRBackendTexture;

			// Try single IntPtr constructor
			ctor = ctors.FirstOrDefault(c =>
			{
				var ps = c.GetParameters();
				return ps.Length == 1 && ps[0].ParameterType == typeof(IntPtr);
			});

			if (ctor is not null)
				return ctor.Invoke([handle]) as GRBackendTexture;

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
	///     Creates a GRContext from a native Metal context handle.
	/// </summary>
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

		return handle == IntPtr.Zero
			? null
			:
			// Use reflection to create GRContext from handle
			// GRContext has an internal constructor that takes IntPtr
			CreateGrContextFromHandle(handle);
	}

	/// <summary>
	///     Creates a GRContext from a native handle using reflection.
	/// </summary>
	private static GRContext? CreateGrContextFromHandle(IntPtr handle)
	{
		try
		{
			// Try to find a constructor on GRContext that takes (IntPtr, bool)
			var ctors = typeof(GRContext).GetConstructors(
				BindingFlags.NonPublic | BindingFlags.Instance
			);

			// Try (IntPtr, bool) constructor
			var ctor = ctors.FirstOrDefault(c =>
			{
				var ps = c.GetParameters();
				return ps.Length == 2 &&
					   ps[0].ParameterType == typeof(IntPtr) &&
					   ps[1].ParameterType == typeof(bool);
			});

			if (ctor is not null)
				return ctor.Invoke([handle, true]) as GRContext;

			// Try (IntPtr) constructor
			ctor = ctors.FirstOrDefault(c =>
			{
				var ps = c.GetParameters();
				return ps.Length == 1 && ps[0].ParameterType == typeof(IntPtr);
			});

			if (ctor is not null)
				return ctor.Invoke([handle]) as GRContext;

			// Last resort: check base class SKObject for useful patterns
			var owned = typeof(SKObject).GetMethod("Owned",
				BindingFlags.NonPublic | BindingFlags.Static);
			if (owned is null || !owned.IsGenericMethod) return null;
			var typedOwned = owned.MakeGenericMethod(typeof(GRContext));
			if (typedOwned.Invoke(null, [handle]) is GRContext result)
				return result;

			return null;
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	///     Native structure for Metal texture info.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct GrMtlTextureInfoNative
	{
		public IntPtr Texture;
	}

	#region Objective-C Runtime

	[LibraryImport(ObjcLibrary, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
	private static partial IntPtr sel_registerName(string name);

	[LibraryImport(ObjcLibrary, EntryPoint = "objc_msgSend")]
	private static partial IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

	[LibraryImport(ObjcLibrary, EntryPoint = "objc_msgSend")]
	private static partial void objc_msgSend_void(IntPtr receiver, IntPtr selector);

	[LibraryImport(ObjcLibrary, EntryPoint = "objc_msgSend")]
	private static partial void objc_msgSend_blit(
		IntPtr receiver, IntPtr selector,
		IntPtr sourceTexture, ulong sourceSlice, ulong sourceLevel,
		MtlOrigin sourceOrigin, MtlSize sourceSize,
		IntPtr destTexture, ulong destSlice, ulong destLevel,
		MtlOrigin destOrigin);

	[StructLayout(LayoutKind.Sequential)]
	public struct MtlOrigin
	{
		public ulong x, y, z;
		public static MtlOrigin Zero => new() { x = 0, y = 0, z = 0 };
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct MtlSize
	{
		public ulong width, height, depth;

		public static MtlSize Create(int w, int h) => new() { width = (ulong)w, height = (ulong)h, depth = 1 };
	}

	// Cached selectors for Metal operations
	private static IntPtr _selCommandBuffer;
	private static IntPtr _selBlitCommandEncoder;
	private static IntPtr _selCopyFromTexture;
	private static IntPtr _selEndEncoding;
	private static IntPtr _selCommit;
	private static IntPtr _selWaitUntilCompleted;

	private static void EnsureSelectorsInitialized()
	{
		if (_selCommandBuffer != IntPtr.Zero) return;

		_selCommandBuffer = sel_registerName("commandBuffer");
		_selBlitCommandEncoder = sel_registerName("blitCommandEncoder");
		_selCopyFromTexture =
			sel_registerName(
				"copyFromTexture:sourceSlice:sourceLevel:sourceOrigin:sourceSize:toTexture:destinationSlice:destinationLevel:destinationOrigin:");
		_selEndEncoding = sel_registerName("endEncoding");
		_selCommit = sel_registerName("commit");
		_selWaitUntilCompleted = sel_registerName("waitUntilCompleted");
	}

	/// <summary>
	///     Performs GPU-to-GPU texture blit using Metal command buffer.
	/// </summary>
	public static bool BlitTexture(IntPtr commandQueue, IntPtr sourceTexture, IntPtr destTexture, int width, int height)
	{
		if (commandQueue == IntPtr.Zero || sourceTexture == IntPtr.Zero || destTexture == IntPtr.Zero)
			return false;

		try
		{
			EnsureSelectorsInitialized();

			// Get command buffer from queue
			var commandBuffer = objc_msgSend(commandQueue, _selCommandBuffer);
			if (commandBuffer == IntPtr.Zero)
			{
				GD.PrintErr("[Estragonia Metal] Failed to create command buffer");
				return false;
			}

			// Get blit command encoder
			var blitEncoder = objc_msgSend(commandBuffer, _selBlitCommandEncoder);
			if (blitEncoder == IntPtr.Zero)
			{
				GD.PrintErr("[Estragonia Metal] Failed to create blit encoder");
				return false;
			}

			// Copy texture
			var origin = MtlOrigin.Zero;
			var size = MtlSize.Create(width, height);

			objc_msgSend_blit(
				blitEncoder, _selCopyFromTexture,
				sourceTexture, 0, 0, origin, size,
				destTexture, 0, 0, origin
			);

			// End encoding
			objc_msgSend_void(blitEncoder, _selEndEncoding);

			// Commit and wait
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

	#endregion

	#region SkiaSharp Backend Texture

	[LibraryImport(SkiaLibrary)]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	private static partial IntPtr sk_surface_get_backend_texture(IntPtr surface, int mode);

	/// <summary>
	///     Gets the Metal texture handle from a Skia surface's backend texture.
	/// </summary>
	public static IntPtr GetSurfaceMetalTexture(SKSurface surface)
	{
		try
		{
			// Get the native handle of the surface
			var surfaceHandle = GetSkObjectHandle(surface);
			if (surfaceHandle == IntPtr.Zero)
				return IntPtr.Zero;

			// FlushAndSubmit mode = 1 (kFlushRead_
			var backendTexture = sk_surface_get_backend_texture(surfaceHandle, 1);
			return backendTexture == IntPtr.Zero
				? IntPtr.Zero
				:
				// The backend texture contains the Metal texture info
				// For Metal, we need to extract the texture handle
				// This is stored at a specific offset in the GrBackendTexture structure
				GetMetalTextureFromBackend(backendTexture);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[Estragonia Metal] GetSurfaceMetalTexture failed: {ex.Message}");
			return IntPtr.Zero;
		}
	}

	private static IntPtr GetSkObjectHandle(SKObject obj)
	{
		// SKObject.Handle property
		var handleProp = typeof(SKObject).GetProperty("Handle",
			BindingFlags.Public | BindingFlags.Instance);
		return handleProp?.GetValue(obj) as IntPtr? ?? IntPtr.Zero;
	}

	[LibraryImport(SkiaLibrary)]
	[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool gr_backendtexture_get_mtl_textureinfo(IntPtr texture, out GrMtlTextureInfoNative info);

	private static IntPtr GetMetalTextureFromBackend(IntPtr backendTexture) =>
		gr_backendtexture_get_mtl_textureinfo(backendTexture, out var info) ? info.Texture : IntPtr.Zero;

	#endregion
}
