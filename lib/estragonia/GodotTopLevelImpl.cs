using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Surfaces;
using Estragonia.Input;
using Godot;
using AvCompositor = Avalonia.Rendering.Composition.Compositor;
using AvKey = Avalonia.Input.Key;
using GdCursorShape = Godot.Control.CursorShape;
using GdMouseButton = Godot.MouseButton;

namespace Estragonia;

/// <summary>Implementation of Avalonia <see cref="ITopLevelImpl" /> that renders to a Godot texture.</summary>
internal sealed class GodotTopLevelImpl : ITopLevelImpl
{
	private readonly IClipboard _clipboard;

	private readonly GodotVkPlatformGraphics _platformGraphics;
	private readonly TouchDevice _touchDevice = new();
	private GdCursorShape _cursorShape;
	private bool _isDisposed;
	private int _lastMouseDeviceId = GodotDevices.EmulatedDeviceId;
	private PixelSize _renderSize;

	private GodotSkiaSurface? _surface;

	// ReSharper disable once ReplaceWithFieldKeyword
	private WindowTransparencyLevel _transparencyLevel = WindowTransparencyLevel.Transparent;

	public GodotTopLevelImpl(GodotVkPlatformGraphics platformGraphics, IClipboard clipboard, AvCompositor compositor)
	{
		_platformGraphics = platformGraphics;
		_clipboard = clipboard;
		Compositor = compositor;

		platformGraphics.AddRef();
	}

	public Action<GdCursorShape>? CursorChanged { get; set; }

	/// <summary>
	///     Exposes the current <see cref="IInputRoot" /> for use by <see cref="GodotWindowImpl" />.
	///     Avoids reflection into this class's private fields.
	/// </summary>
	internal IInputRoot? InputRoot { get; private set; }

	public double RenderScaling { get; private set; } = 1.0;

	double ITopLevelImpl.DesktopScaling => 1.0;

	IPlatformHandle? ITopLevelImpl.Handle => null;

	public AvCompositor Compositor { get; }

	public Size ClientSize { get; private set; }

	public WindowTransparencyLevel TransparencyLevel
	{
		get => _transparencyLevel;
		private set
		{
			if (_transparencyLevel.Equals(value))
				return;

			_transparencyLevel = value;
			TransparencyLevelChanged?.Invoke(value);
		}
	}

	public Action<Rect>? Paint { get; set; }

	public Action<Size, WindowResizeReason>? Resized { get; set; }

	public Action? Closed { get; set; }

	public Action<RawInputEventArgs>? Input { get; set; }

	public Action? LostFocus { get; set; }

	public Action<double>? ScalingChanged { get; set; }

	public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

	IPlatformRenderSurface[] ITopLevelImpl.Surfaces => GetOrCreateSurfaces();

	AcrylicPlatformCompensationLevels ITopLevelImpl.AcrylicCompensationLevels => new(1.0, 1.0, 1.0);

	void ITopLevelImpl.SetInputRoot(IInputRoot inputRoot) => InputRoot = inputRoot;

	Point ITopLevelImpl.PointToClient(PixelPoint point) => point.ToPoint(RenderScaling);

	PixelPoint ITopLevelImpl.PointToScreen(Point point) => PixelPoint.FromPoint(point, RenderScaling);

	void ITopLevelImpl.SetCursor(ICursorImpl? cursor)
	{
		var cursorShape = (cursor as GodotStandardCursorImpl)?.CursorShape ?? GdCursorShape.Arrow;
		if (_cursorShape == cursorShape)
			return;

		_cursorShape = cursorShape;
		CursorChanged?.Invoke(cursorShape);
	}

	IPopupImpl? ITopLevelImpl.CreatePopup() => null;

	void ITopLevelImpl.SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels) =>
		// Overlay windows are always composited onto the host control's texture,
		// so we force Transparent level regardless of what Avalonia requests.
		// This prevents PART_TransparencyFallback from showing an opaque white background.
		TransparencyLevel = WindowTransparencyLevel.Transparent;

	void ITopLevelImpl.SetFrameThemeVariant(PlatformThemeVariant? themeVariant)
	{
	}

	object? IOptionalFeatureProvider.TryGetFeature(Type featureType) =>
		featureType == typeof(IClipboard) ? _clipboard : null;

	public void Dispose()
	{
		if (_isDisposed)
			return;

		_isDisposed = true;

		if (_surface is not null)
		{
			_surface.Dispose();
			_surface = null;
		}

		Closed?.Invoke();

		_platformGraphics.Release();
	}

	private GodotSkiaSurface CreateSurface()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, nameof(GodotTopLevelImpl));
		return _platformGraphics.GetSharedContext().CreateSurface(_renderSize, RenderScaling);
	}

	// ReSharper disable once UnusedMember.Global
	public GodotSkiaSurface? TryGetSurface() => _surface;

	public GodotSkiaSurface GetOrCreateSurface() => _surface ??= CreateSurface();

	private IPlatformRenderSurface[] GetOrCreateSurfaces() => [GetOrCreateSurface()];

	[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "Doesn't affect correctness")]
	public void SetRenderSize(PixelSize renderSize, double renderScaling)
	{
		var hasScalingChanged = RenderScaling != renderScaling;
		if (_renderSize == renderSize && !hasScalingChanged)
			return;

		var oldClientSize = ClientSize;
		var unclampedClientSize = renderSize.ToSize(renderScaling);

		ClientSize = new Size(Math.Max(unclampedClientSize.Width, 0.0), Math.Max(unclampedClientSize.Height, 0.0));
		RenderScaling = renderScaling;

		if (_renderSize != renderSize)
		{
			_renderSize = renderSize;

			if (_surface is not null)
			{
				_surface.Dispose();
				_surface = null;
			}

			if (_isDisposed)
				return;

			_surface = CreateSurface();
		}

		if (hasScalingChanged)
		{
			_surface?.RenderScaling = RenderScaling;
			ScalingChanged?.Invoke(RenderScaling);
		}

		if (oldClientSize != ClientSize)
			Resized?.Invoke(ClientSize,
				hasScalingChanged ? WindowResizeReason.DpiChange : WindowResizeReason.Unspecified);
	}

	public void OnDraw(Rect rect)
	{
		if (_isDisposed)
			return;

		Paint?.Invoke(rect);
	}

	public bool OnMouseMotion(InputEventMouseMotion inputEvent, ulong timestamp)
	{
		_lastMouseDeviceId = inputEvent.Device;

		if (InputRoot is null || Input is not { } input)
			return false;

		var args = new RawPointerEventArgs(
			GodotDevices.GetMouse(inputEvent.Device),
			timestamp,
			InputRoot,
			RawPointerEventType.Move,
			CreateRawPointerPoint(inputEvent.Position, inputEvent.Pressure, inputEvent.Tilt),
			inputEvent.GetRawInputModifiers()
		);

		input(args);

		return args.Handled;
	}

	public bool OnMouseButton(InputEventMouseButton inputEvent, ulong timestamp)
	{
		_lastMouseDeviceId = inputEvent.Device;

		if (InputRoot is null || Input is not { } input)
			return false;

		var args = (inputEvent.ButtonIndex, inputEvent.Pressed) switch
		{
			(GdMouseButton.Left, true) => CreateButtonArgs(RawPointerEventType.LeftButtonDown),
			(GdMouseButton.Left, false) => CreateButtonArgs(RawPointerEventType.LeftButtonUp),
			(GdMouseButton.Right, true) => CreateButtonArgs(RawPointerEventType.RightButtonDown),
			(GdMouseButton.Right, false) => CreateButtonArgs(RawPointerEventType.RightButtonUp),
			(GdMouseButton.Middle, true) => CreateButtonArgs(RawPointerEventType.MiddleButtonDown),
			(GdMouseButton.Middle, false) => CreateButtonArgs(RawPointerEventType.MiddleButtonUp),
			(GdMouseButton.Xbutton1, true) => CreateButtonArgs(RawPointerEventType.XButton1Down),
			(GdMouseButton.Xbutton1, false) => CreateButtonArgs(RawPointerEventType.XButton1Up),
			(GdMouseButton.Xbutton2, true) => CreateButtonArgs(RawPointerEventType.XButton2Down),
			(GdMouseButton.Xbutton2, false) => CreateButtonArgs(RawPointerEventType.XButton2Up),
			(GdMouseButton.WheelUp, _) => CreateWheelArgs(new Vector(0.0, inputEvent.Factor)),
			(GdMouseButton.WheelDown, _) => CreateWheelArgs(new Vector(0.0, -inputEvent.Factor)),
			(GdMouseButton.WheelLeft, _) => CreateWheelArgs(new Vector(inputEvent.Factor, 0.0)),
			(GdMouseButton.WheelRight, _) => CreateWheelArgs(new Vector(-inputEvent.Factor, 0.0)),
			_ => null
		};

		if (args is null)
			return false;

		input(args);

		return args.Handled;

		RawPointerEventArgs CreateButtonArgs(RawPointerEventType type)
		{
			return new RawPointerEventArgs(
				GodotDevices.GetMouse(inputEvent.Device),
				timestamp,
				InputRoot,
				type,
				inputEvent.Position.ToAvaloniaPoint() / RenderScaling,
				inputEvent.GetRawInputModifiers()
			);
		}

		RawMouseWheelEventArgs CreateWheelArgs(Vector delta)
		{
			return new RawMouseWheelEventArgs(
				GodotDevices.GetMouse(inputEvent.Device),
				timestamp,
				InputRoot,
				inputEvent.Position.ToAvaloniaPoint() / RenderScaling,
				delta,
				inputEvent.GetRawInputModifiers()
			);
		}
	}

	public bool OnScreenTouch(InputEventScreenTouch inputEvent, ulong timestamp)
	{
		if (InputRoot is null || Input is not { } input)
			return false;

		var args = new RawTouchEventArgs(
			_touchDevice,
			timestamp,
			InputRoot,
			inputEvent.Pressed ? RawPointerEventType.TouchBegin : RawPointerEventType.TouchEnd,
			inputEvent.Position.ToAvaloniaPoint() / RenderScaling,
			InputModifiersProvider.GetRawInputModifiers(),
			inputEvent.Index
		);

		input(args);

		return args.Handled;
	}

	public bool OnScreenDrag(InputEventScreenDrag inputEvent, ulong timestamp)
	{
		if (InputRoot is null || Input is not { } input)
			return false;

		var args = new RawTouchEventArgs(
			_touchDevice,
			timestamp,
			InputRoot,
			RawPointerEventType.TouchUpdate,
			CreateRawPointerPoint(inputEvent.Position, inputEvent.Pressure, inputEvent.Tilt),
			inputEvent.GetRawInputModifiers(),
			inputEvent.Index
		);

		input(args);

		return args.Handled;
	}

	private RawPointerPoint CreateRawPointerPoint(Vector2 position, float pressure, Vector2 tilt) =>
		new()
		{
			Position = position.ToAvaloniaPoint() / RenderScaling,
			Twist = 0.0f,
			Pressure = pressure,
			XTilt = tilt.X * 90.0f,
			YTilt = tilt.Y * 90.0f
		};

	public bool OnKey(InputEventKey inputEvent, ulong timestamp)
	{
		if (InputRoot is null || Input is not { } input)
			return false;

		var keyCode = inputEvent.Keycode;
		var pressed = inputEvent.Pressed;
		var key = keyCode.ToAvaloniaKey();

		if (key != AvKey.None)
		{
			var args = new RawKeyEventArgs(
				GodotDevices.Keyboard,
				timestamp,
				InputRoot,
				pressed ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
				key,
				inputEvent.GetRawInputModifiers(),
				inputEvent.PhysicalKeycode.ToAvaloniaPhysicalKey(),
				OS.GetKeycodeString(inputEvent.KeyLabel)
			);

			input(args);

			if (args.Handled)
				return true;
		}

		if (!pressed || !OS.IsKeycodeUnicode((long)keyCode)) return false;
		{
			var text = char.ConvertFromUtf32((int)inputEvent.Unicode);
			var args = new RawTextInputEventArgs(GodotDevices.Keyboard, timestamp, InputRoot, text);

			input(args);

			if (args.Handled)
				return true;
		}

		return false;
	}

	public bool OnJoypadButton(InputEventJoypadButton inputEvent, ulong timestamp)
	{
		if (InputRoot is null || Input is not { } input)
			return false;

		var args = new RawJoypadButtonEventArgs(
			GodotDevices.GetJoypad(inputEvent.Device),
			timestamp,
			InputRoot,
			inputEvent.IsPressed() ? RawJoypadButtonEventType.ButtonDown : RawJoypadButtonEventType.ButtonUp,
			inputEvent.ButtonIndex
		);

		input(args);

		return args.Handled;
	}

	public bool OnJoypadMotion(InputEventJoypadMotion inputEvent, ulong timestamp)
	{
		if (InputRoot is null || Input is not { } input)
			return false;

		var args = new RawJoypadAxisEventArgs(
			GodotDevices.GetJoypad(inputEvent.Device),
			timestamp,
			InputRoot,
			inputEvent.Axis,
			inputEvent.AxisValue
		);

		input(args);

		return args.Handled;
	}

	public void OnLostFocus() => LostFocus?.Invoke();

	// ReSharper disable once UnusedMethodReturnValue.Global
	public bool OnMouseExited(ulong timestamp)
	{
		if (InputRoot is null || Input is not { } input)
			return false;

		var args = new RawPointerEventArgs(
			GodotDevices.GetMouse(_lastMouseDeviceId),
			timestamp,
			InputRoot,
			RawPointerEventType.LeaveWindow,
			new Point(-1, -1),
			InputModifiersProvider.GetRawInputModifiers()
		);

		input(args);

		return args.Handled;
	}

	/// <summary>
	///     Handles files dropped from the OS onto the Godot window.
	///     First sends DragLeave to end any hover session, then
	///     synthesizes DragEnter → DragOver → Drop with real file data.
	/// </summary>
	// ReSharper disable once UnusedParameter.Global
	public bool OnFilesDropped(string[] files, Vector2 position, ulong timestamp)
	{
		if (InputRoot is null || Input is not { } input)
			return false;

		var point = position.ToAvaloniaPoint() / RenderScaling;
		var modifiers = InputModifiersProvider.GetRawInputModifiers();
		var device = AvaloniaLocator.Current.GetRequiredService<IDragDropDevice>();

		// Build IDataTransfer from the dropped file paths.
		// Validate each path exists on the filesystem before creating storage items
		// to prevent processing invalid or potentially malicious paths.
		var dataTransfer = new DataTransfer();
		foreach (var filePath in files)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				continue;

			// Only accept absolute paths from OS drag-drop
			if (!Path.IsPathRooted(filePath))
				continue;

			try
			{
				IStorageItem storageItem = Directory.Exists(filePath)
					? new BclStorageFolder(new DirectoryInfo(filePath))
					: File.Exists(filePath)
						? new BclStorageFile(new FileInfo(filePath))
						: null!; // Skip paths that no longer exist
				dataTransfer.Add(DataTransferItem.CreateFile(storageItem));
			}
			catch (ArgumentException)
			{
				// Invalid path characters — skip
			}
			catch (SecurityException)
			{
				// No access to path — skip
			}
			catch (NotSupportedException)
			{
				// Path format not supported — skip
			}
		}

		if (dataTransfer.Items.Count == 0)
			return false;

		// Synthesize DragEnter → DragOver → Drop sequence
		var enterArgs = new RawDragEvent(device, RawDragEventType.DragEnter, InputRoot, point, dataTransfer,
			DragDropEffects.Copy | DragDropEffects.Link, modifiers);
		input(enterArgs);

		var overArgs = new RawDragEvent(device, RawDragEventType.DragOver, InputRoot, point, dataTransfer,
			DragDropEffects.Copy | DragDropEffects.Link, modifiers);
		input(overArgs);

		var dropArgs = new RawDragEvent(device, RawDragEventType.Drop, InputRoot, point, dataTransfer,
			DragDropEffects.Copy | DragDropEffects.Link, modifiers);
		input(dropArgs);

		return dropArgs.Handled;
	}
}
