using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Godot;
using Godot.NativeInterop;
using AvCompositor = Avalonia.Rendering.Composition.Compositor;
using AvDispatcher = Avalonia.Threading.Dispatcher;
using GdControl = Godot.Control;
using GdCursorShape = Godot.Control.CursorShape;
using GdWindow = Godot.Window;

namespace Estragonia;

/// <summary>
///     An <see cref="IWindowImpl" /> backed by a Godot Window node with native OS decorations.
///     Dragging, resizing, maximizing and minimizing are all handled by the OS through Godot,
///     while the Avalonia content is rendered inside the client area.
/// </summary>
internal sealed class GodotWindowImpl : IWindowImpl
{
	private static bool _guiEmbedSubwindowsSet;
	private readonly GdWindow _gdWindow;
	private readonly WindowHostControl _hostControl;
	private readonly bool _isManagedDialog;
	private readonly GodotScreenImpl _screenImpl;

	private readonly GodotTopLevelImpl _topLevelImpl;

	private bool _isDisposed;
	private bool _isVisible;

	// Tracks the last PixelSize pushed from _Process, to tell external size changes (a user resize or a
	// maximize) apart from Avalonia-driven ones (a SizeToContent layout). This prevents the feedback loop
	// where SetRenderSize -> Resized -> layout -> Resize makes the window grow every frame. It starts at
	// (-1, -1) so that _Process always pushes the first real size, even when the window starts at 0x0 or
	// already matches the default PixelSize.
	private PixelSize _lastProcessRenderSize = new(-1, -1);

	// SizeToContent windows have to be re-centered once the first layout pass has determined their actual
	// content size, which differs from the initial 400x300 default
	private bool _needsRecenter;
	private GodotWindowImpl? _parentImpl;
	private Vector2I _pendingSize = new(400, 300);
	private bool _popupLayerEnabled;
	private bool _unresizable;
	private WindowState _windowState = WindowState.Normal;

	public GodotWindowImpl(IGodotPlatformGraphics platformGraphics, IClipboard clipboard, AvCompositor compositor)
	{
		_isManagedDialog = GodotPlatform.IsManagedDialogWindow;
		_topLevelImpl = new GodotTopLevelImpl(platformGraphics, clipboard, compositor);
		_screenImpl = new GodotScreenImpl();
		_topLevelImpl.Paint = rect => Paint?.Invoke(rect);
		_topLevelImpl.Resized = (size, reason) => Resized?.Invoke(size, reason);
		_topLevelImpl.Input = args => Input?.Invoke(args);
		_topLevelImpl.LostFocus = () => LostFocus?.Invoke();
		_topLevelImpl.ScalingChanged = scaling => ScalingChanged?.Invoke(scaling);
		_topLevelImpl.TransparencyLevelChanged = level => TransparencyLevelChanged?.Invoke(level);

		_topLevelImpl.SetRenderSize(new PixelSize(400, 300), 1.0);

		_gdWindow = new GdWindow
		{
			Title = string.Empty,
			Visible = false,
			// Keep native OS decorations - Godot/OS handles drag, resize, maximize, minimize
			Borderless = false,
			Transparent = false,
			InitialPosition = GdWindow.WindowInitialPosition.Absolute,
			WrapControls = false,
			MinSize = new Vector2I(100, 50),
			Size = new Vector2I(400, 300)
		};
		_gdWindow.AddToGroup("avalonia_windows");
		_hostControl = new WindowHostControl(this);
		_gdWindow.AddChild(_hostControl);
		_topLevelImpl.CursorChanged = cursorShape => _hostControl.SetCursor(cursorShape);
		_gdWindow.CloseRequested += OnCloseRequested;
		_gdWindow.SizeChanged += OnSizeChanged;
		_gdWindow.WindowInput += OnWindowInput;
		_gdWindow.FilesDropped += OnFilesDropped;
	}

	public Action<Rect>? Paint { get; set; }
	public Action<Size, WindowResizeReason>? Resized { get; set; }
	public Action? Closed { get; set; }
	public Action<RawInputEventArgs>? Input { get; set; }
	public Action? LostFocus { get; set; }
	public Action<double>? ScalingChanged { get; set; }
	public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }
	public Action<PixelPoint>? PositionChanged { get; set; }
	public Action? Activated { get; set; }
	public Action? Deactivated { get; set; }
	public Action<WindowState>? WindowStateChanged { get; set; }
	public Action? GotInputWhenDisabled { get; set; }
	public Func<WindowCloseReason, bool>? Closing { get; set; }
	public Action<bool>? ExtendClientAreaToDecorationsChanged { get; set; }
	public Action<PlatformAllowedWindowActions>? AllowedWindowActionsChanged { get; set; }

	public Size ClientSize => _topLevelImpl.ClientSize;
	public double RenderScaling => _topLevelImpl.RenderScaling;
	public WindowTransparencyLevel TransparencyLevel => _topLevelImpl.TransparencyLevel;
	public AvCompositor Compositor => _topLevelImpl.Compositor;
	double ITopLevelImpl.DesktopScaling => 1.0;
	IPlatformHandle? ITopLevelImpl.Handle => null;
	AcrylicPlatformCompensationLevels ITopLevelImpl.AcrylicCompensationLevels => new(1.0, 1.0, 1.0);
	IPlatformRenderSurface[] ITopLevelImpl.Surfaces => ((ITopLevelImpl)_topLevelImpl).Surfaces;
	public Size? FrameSize => null;

	public PixelPoint Position { get; private set; }

	// Finite constraint prevents Infinity propagation to PopupOverlayLayer.MeasureOverride.
	public Size MaxAutoSizeHint { get; } = new(8192, 8192);

	public WindowState WindowState
	{
		get => _windowState;
		set
		{
			_windowState = value;
			_gdWindow.Mode = value switch
			{
				WindowState.Normal => GdWindow.ModeEnum.Windowed,
				WindowState.Maximized => GdWindow.ModeEnum.Maximized,
				WindowState.Minimized => GdWindow.ModeEnum.Minimized,
				WindowState.FullScreen => GdWindow.ModeEnum.Fullscreen,
				_ => GdWindow.ModeEnum.Windowed
			};
		}
	}

	// OS handles decorations - no managed decorations needed
	public bool WindowStateGetterIsUsable => false;
	public bool IsClientAreaExtendedToDecorations => false;
	public bool NeedsManagedDecorations => false;
	public PlatformRequestedDrawnDecoration RequestedDrawnDecorations => PlatformRequestedDrawnDecoration.None;
	public Thickness ExtendedMargins => default;
	public Thickness OffScreenMargin => default;
	public PlatformAllowedWindowActions AllowedWindowActions => PlatformAllowedWindowActions.All;

	public void Show(bool activate, bool isDialog)
	{
		if (_isDisposed || _isVisible)
			return;

		var sceneTree = (SceneTree)Engine.GetMainLoop();

		// Only set GuiEmbedSubwindows once: it's a global property on the root viewport
		// that affects every Godot sub-window, not just this one
		if (!_guiEmbedSubwindowsSet)
		{
			sceneTree.Root.GuiEmbedSubwindows = false;
			_guiEmbedSubwindowsSet = true;
		}

		// Determine whether this window should be modal, that is, block input to its parent:
		// - isDialog is set by Avalonia's ShowDialog()
		// - _isManagedDialog is set when the window comes from ManagedFileDialogOptions.ContentRootFactory,
		//   as managed file dialogs use Show() rather than ShowDialog() when the parent TopLevel is a
		//   GodotTopLevel instead of an Avalonia Window
		// - _unresizable with no parent is a fallback heuristic for other dialog-like windows
		var modal = isDialog || (_isManagedDialog && _parentImpl is null) || (_unresizable && _parentImpl is null);

		// Always add sub-windows as siblings under the root viewport: Godot's Transient and Exclusive
		// flags provide the modal semantics through window IDs rather than the node hierarchy, so
		// nesting would only produce an incorrect scene tree structure
		sceneTree.Root.AddChild(_gdWindow);

		// Transient keeps the window on top of its parent and returns focus when it closes, while
		// Exclusive blocks all input to the parent, which is Godot's modal mechanism
		if (modal)
		{
			_gdWindow.Transient = true;
			_gdWindow.Exclusive = true;
		}

		// Apply the pending size after AddChild, so that the window is registered in the
		// DisplayServer before OnSizeChanged fires
		_gdWindow.Size = _pendingSize;

		// Defer the initial positioning (Godot bug #89372) and center the window relative to the main
		// window's actual screen position rather than the screen origin
		var mainWinId = sceneTree.Root.GetWindowId();
		var mainWinPos = DisplayServer.WindowGetPosition(mainWinId);
		var mainWinSize = sceneTree.Root.Size;
		var subWinSize = _gdWindow.Size;
		var centerPos = new Vector2I(
			mainWinPos.X + Math.Max((mainWinSize.X - subWinSize.X) / 2, 0),
			mainWinPos.Y + Math.Max((mainWinSize.Y - subWinSize.Y) / 2, 0)
		);
		_gdWindow.CallDeferred(GdWindow.MethodName.SetPosition, centerPos);

		var size = _gdWindow.Size;
		_lastProcessRenderSize = new PixelSize(Math.Max(size.X, 1), Math.Max(size.Y, 1));
		_topLevelImpl.SetRenderSize(_lastProcessRenderSize, 1.0);

		// For SizeToContent windows such as managed file dialogs, Avalonia replaces the initial 400x300
		// with its layout-determined size on the first _Process tick, so flag a re-center for afterwards
		if (_isManagedDialog)
			_needsRecenter = true;

		_gdWindow.Visible = true;
		_isVisible = true;

		if (activate)
			_gdWindow.GrabFocus();
	}

	public void Hide()
	{
		_gdWindow.Visible = false;
		_isVisible = false;
	}

	public void Activate() => _gdWindow.GrabFocus();

	public void SetTopmost(bool value)
	{
	}

	public void SetTitle(string? title) => _gdWindow.Title = title ?? string.Empty;

	public void SetParent(IWindowImpl? parent) => _parentImpl = parent as GodotWindowImpl;

	public void SetEnabled(bool enable) => _hostControl.SetEnabled(enable);

	public void SetWindowDecorations(WindowDecorations enabled) =>
		_gdWindow.Borderless = enabled == WindowDecorations.None;

	public void SetIcon(IWindowIconImpl? icon)
	{
	}

	public void ShowTaskbarIcon(bool value)
	{
	}

	public void CanResize(bool value)
	{
		_unresizable = !value;
		_gdWindow.Unresizable = !value;
	}

	public void SetCanMinimize(bool value)
	{
	}

	public void SetCanMaximize(bool value)
	{
	}

	// OS handles drag/resize natively - these are no-ops
	public void BeginMoveDrag(PointerPressedEventArgs e)
	{
	}

	public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e)
	{
	}

	public void Resize(Size clientSize, WindowResizeReason reason = WindowResizeReason.Application)
	{
		var pixelSize = new Vector2I(Math.Max((int)clientSize.Width, 1), Math.Max((int)clientSize.Height, 1));
		var pxSize = new PixelSize(pixelSize.X, pixelSize.Y);
		_pendingSize = pixelSize;
		if (_isVisible && _gdWindow.IsInsideTree())
			_gdWindow.Size = pixelSize;

		// Record the size so that _Process doesn't push it back to Avalonia
		_lastProcessRenderSize = pxSize;
		_topLevelImpl.SetRenderSize(pxSize, 1.0);
	}

	public void Move(PixelPoint point)
	{
		if (_isVisible && _gdWindow.IsInsideTree())
			DisplayServer.WindowSetPosition(new Vector2I(point.X, point.Y), _gdWindow.GetWindowId());

		Position = point;
	}

	public void SetMinMaxSize(Size minSize, Size maxSize)
	{
		_gdWindow.MinSize = new Vector2I(Math.Max((int)minSize.Width, 0), Math.Max((int)minSize.Height, 0));
		if (maxSize is { Width: > 0, Height: > 0 })
			_gdWindow.MaxSize = new Vector2I((int)maxSize.Width, (int)maxSize.Height);
	}

	public void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint)
	{
	}

	public void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight)
	{
	}

	void ITopLevelImpl.SetInputRoot(IInputRoot inputRoot) => ((ITopLevelImpl)_topLevelImpl).SetInputRoot(inputRoot);

	Point ITopLevelImpl.PointToClient(PixelPoint point) => ((ITopLevelImpl)_topLevelImpl).PointToClient(point);

	PixelPoint ITopLevelImpl.PointToScreen(Point point) => ((ITopLevelImpl)_topLevelImpl).PointToScreen(point);

	void ITopLevelImpl.SetCursor(ICursorImpl? cursor) => ((ITopLevelImpl)_topLevelImpl).SetCursor(cursor);

	// Returning null makes Avalonia use an OverlayPopupHost
	IPopupImpl? ITopLevelImpl.CreatePopup() => null;

	void ITopLevelImpl.SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels) =>
		((ITopLevelImpl)_topLevelImpl).SetTransparencyLevelHint(transparencyLevels);

	void ITopLevelImpl.SetFrameThemeVariant(PlatformThemeVariant? themeVariant)
	{
	}

	object? IOptionalFeatureProvider.TryGetFeature(Type featureType) =>
		featureType == typeof(IScreenImpl)
			? _screenImpl
			: ((ITopLevelImpl)_topLevelImpl).TryGetFeature(featureType);

	public void Dispose()
	{
		if (_isDisposed)
			return;

		_isDisposed = true;
		_isVisible = false;

		// Unsubscribe from the events before removing the window from the tree, so that
		// OnSizeChanged can't fire on an invalid window
		if (GodotObject.IsInstanceValid(_gdWindow))
		{
			_gdWindow.CloseRequested -= OnCloseRequested;
			_gdWindow.SizeChanged -= OnSizeChanged;
			_gdWindow.WindowInput -= OnWindowInput;
			_gdWindow.FilesDropped -= OnFilesDropped;
			if (_gdWindow.IsInsideTree())
			{
				// Hide the window immediately to stop visual updates, then defer RemoveChild and QueueFree
				// to the end of the frame to avoid a _push_unhandled_input_internal !is_inside_tree()
				// error: Dispose runs during input processing, such as a close button click, while Godot's
				// input pipeline still holds a reference to this viewport, so removing the child right
				// away would make the engine push unhandled input to a node that has left the tree
				_gdWindow.Visible = false;
				var parent = _gdWindow.GetParent();
				parent?.CallDeferred(Node.MethodName.RemoveChild, _gdWindow);
				_gdWindow.CallDeferred(Node.MethodName.QueueFree);
			}
		}

		_topLevelImpl.Dispose();
		Closed?.Invoke();
	}

	/// <summary>
	///     Enables PopupOverlayLayer on the VisualLayerManager so that popups
	///     (ComboBox dropdowns, context menus, tooltips) render in-window.
	///     Called from _Process after layout completes with a valid size.
	/// </summary>
	/// <remarks>
	///     This method reflects into Avalonia's internal APIs (RootVisual, VisualChildren and
	///     EnablePopupOverlayLayer) because no public API exists for this purpose. They're guarded by
	///     AvaloniaAccessUnstablePrivateApis and may break when Avalonia is upgraded. The reflection
	///     results are cached to keep the AOT trimming risk to a minimum.
	/// </remarks>
	private void TryEnablePopupOverlayLayer()
	{
		if (_popupLayerEnabled || _isDisposed)
			return;

		// Use the internal property rather than reflecting into this class
		var inputRoot = _topLevelImpl.InputRoot;

		if (inputRoot is null)
			return;

		// RootVisual is an Avalonia internal property on PresentationSource, which is itself internal,
		// so it's resolved lazily from the inputRoot instance type
		var rootVisualProperty = CachedReflection.GetRootVisualProperty(inputRoot);
		var rootVisual = rootVisualProperty?.GetValue(inputRoot) as InputElement;

		if ((rootVisual as ILogical)?.LogicalParent is not TopLevel topLevel)
			return;

		// Walk the visual tree to find the, possibly unnamed, VisualLayerManager
		if (FindVisualChild<VisualLayerManager>(topLevel) is not { } visualLayerManager)
			return;

		visualLayerManager.EnableOverlayLayer = true;
		visualLayerManager.EnableTextSelectorLayer = true;

		// EnablePopupOverlayLayer is an Avalonia internal property
		CachedReflection.EnablePopupOverlayLayerProperty?.SetValue(visualLayerManager, true);

		_popupLayerEnabled = true;
	}

	private static T? FindVisualChild<T>(Visual parent) where T : Visual
	{
		if (CachedReflection.VisualChildrenProperty?.GetValue(parent) is not IEnumerable<Visual> children)
			return null;

		foreach (var child in children)
		{
			if (child is T typed)
				return typed;

			if (FindVisualChild<T>(child) is { } result)
				return result;
		}

		return null;
	}

	private void OnCloseRequested()
	{
		if (Closing?.Invoke(WindowCloseReason.WindowClosing) == true)
			return;

		Dispose();
	}

	private void OnSizeChanged()
	{
		if (_isDisposed)
			return;

		var size = _gdWindow.Size;
		var pixelSize = new PixelSize(Math.Max(size.X, 1), Math.Max(size.Y, 1));

		// Only push the size to Avalonia when it changed externally, through a user resize or a maximize.
		// Skip it when Resize() already updated _lastProcessRenderSize to match, which prevents the
		// feedback loop: Resize() -> _gdWindow.Size = X -> OnSizeChanged -> SetRenderSize -> Resized ->
		// layout -> Resize() -> _gdWindow.Size = X -> OnSizeChanged -> ...
		if (pixelSize != _lastProcessRenderSize)
		{
			_lastProcessRenderSize = pixelSize;
			_topLevelImpl.SetRenderSize(pixelSize, 1.0);
		}

		// DisplayServer.WindowGetPosition fails when the window isn't registered yet, for instance when
		// Size is set before AddChild, or when it has already been removed during Dispose
		if (_isVisible && _gdWindow.IsInsideTree())
		{
			var windowId = _gdWindow.GetWindowId();
			var actualPos = DisplayServer.WindowGetPosition(windowId);
			Position = new PixelPoint(actualPos.X, actualPos.Y);
			PositionChanged?.Invoke(Position);
		}

		var newAvState = _gdWindow.Mode switch
		{
			GdWindow.ModeEnum.Maximized => WindowState.Maximized,
			GdWindow.ModeEnum.Minimized => WindowState.Minimized,
			GdWindow.ModeEnum.Fullscreen => WindowState.FullScreen,
			_ => WindowState.Normal
		};
		if (newAvState == _windowState)
			return;

		_windowState = newAvState;
		WindowStateChanged?.Invoke(newAvState);
	}

	private void OnWindowInput(InputEvent @event)
	{
		if (_isDisposed)
			return;

		_ = @event switch
		{
			InputEventMouseMotion motion => _topLevelImpl.OnMouseMotion(motion, Time.GetTicksMsec()),
			InputEventMouseButton button => _topLevelImpl.OnMouseButton(button, Time.GetTicksMsec()),
			InputEventScreenTouch st => _topLevelImpl.OnScreenTouch(st, Time.GetTicksMsec()),
			InputEventScreenDrag sd => _topLevelImpl.OnScreenDrag(sd, Time.GetTicksMsec()),
			InputEventKey k => _topLevelImpl.OnKey(k, Time.GetTicksMsec()),
			InputEventJoypadButton jb => _topLevelImpl.OnJoypadButton(jb, Time.GetTicksMsec()),
			InputEventJoypadMotion jm => _topLevelImpl.OnJoypadMotion(jm, Time.GetTicksMsec()),
			_ => false
		};
	}

	private void OnFilesDropped(string[] files)
	{
		if (_isDisposed || files.Length == 0)
			return;

		// Get the mouse position relative to the window's content area
		var mousePos = _gdWindow.GetMousePosition();
		_topLevelImpl.OnFilesDropped(files, mousePos);
	}

	/// <summary>
	///     Caches the reflection lookups for Avalonia's internal APIs, which avoids the reflection
	///     overhead on every _Process tick and keeps all internal API access in a single place.
	/// </summary>
	private static class CachedReflection
	{
		private const BindingFlags PropertyFlags =
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

		private static PropertyInfo? _rootVisualProperty;
		private static bool _rootVisualResolved;

		public static PropertyInfo? VisualChildrenProperty { get; } =
			typeof(Visual).GetProperty("VisualChildren", PropertyFlags);

		public static PropertyInfo? EnablePopupOverlayLayerProperty { get; } =
			typeof(VisualLayerManager).GetProperty("EnablePopupOverlayLayer", PropertyFlags);

		/// <summary>
		///     Lazily resolves and caches the RootVisual property from the PresentationSource type,
		///     which is internal in Avalonia. Returns <c>null</c> when the property isn't found, for
		///     instance because AOT trimmed it away.
		/// </summary>
		public static PropertyInfo? GetRootVisualProperty(IInputRoot inputRoot)
		{
			if (_rootVisualResolved)
				return _rootVisualProperty;

			_rootVisualResolved = true;

			// A RootVisual that AOT trimmed away is handled by returning null
#pragma warning disable IL2075
			_rootVisualProperty = inputRoot.GetType().GetProperty("RootVisual", PropertyFlags);
#pragma warning restore IL2075

			return _rootVisualProperty;
		}
	}

	private sealed class WindowHostControl : GdControl
	{
		private readonly GodotWindowImpl _owner;
		private bool _isEnabled = true;

		public WindowHostControl(GodotWindowImpl owner)
		{
			_owner = owner;
			SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		}

		public void SetEnabled(bool enabled) => _isEnabled = enabled;

		protected override bool InvokeGodotClassMethod(
			in godot_string_name method,
			NativeVariantPtrArgs args,
			out godot_variant ret
		)
		{
			if (method == Node.MethodName._Ready && args.Count == 0)
			{
				_Ready();
				ret = default;
				return true;
			}

			if (method == Node.MethodName._Process && args.Count == 1)
			{
				_Process(VariantUtils.ConvertTo<double>(args[0]));
				ret = default;
				return true;
			}

			if (method == CanvasItem.MethodName._Draw && args.Count == 0)
			{
				_Draw();
				ret = default;
				return true;
			}

			// ReSharper disable once InvertIf -- keeps this branch symmetric with the ones above
			if (method == MethodName._GuiInput && args.Count == 1)
			{
				_GuiInput(VariantUtils.ConvertTo<InputEvent>(args[0]));
				ret = default;
				return true;
			}

			return base.InvokeGodotClassMethod(method, args, out ret);
		}

		protected override bool HasGodotClassMethod(in godot_string_name method) =>
			method == Node.MethodName._Ready
			|| method == Node.MethodName._Process
			|| method == CanvasItem.MethodName._Draw
			|| method == MethodName._GuiInput
			|| base.HasGodotClassMethod(method);

		public override void _Ready()
		{
			if (Engine.IsEditorHint())
				return;

			Material = new CanvasItemMaterial
			{
				BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha,
				LightMode = CanvasItemMaterial.LightModeEnum.Unshaded
			};
		}

		public override void _Process(double delta)
		{
			if (_owner._isDisposed)
				return;

			GodotPlatform.TriggerRenderTick();
			AvDispatcher.UIThread.RunJobs();

			var window = _owner._gdWindow;
			var winSize = window.Size;
			var pixelSize = new PixelSize(Math.Max(winSize.X, 1), Math.Max(winSize.Y, 1));

			// Only push Godot's window size to Avalonia when it changed externally, through a user
			// resize, a maximize or a fullscreen toggle. Skip it when the size matches what Avalonia
			// already set through Resize(), which prevents the SizeToContent feedback loop where
			// SetRenderSize -> Resized -> layout -> Resize grows the window every frame.
			if (pixelSize != _owner._lastProcessRenderSize)
			{
				_owner._lastProcessRenderSize = pixelSize;
				_owner._topLevelImpl.SetRenderSize(pixelSize, 1.0);
				// Run the layout jobs queued by SetRenderSize before drawing. Without this, maximizing
				// or going fullscreen shows a blurry texture, as OnDraw would render the old layout
				// onto the new surface.
				AvDispatcher.UIThread.RunJobs();
			}

			// Enable the popup overlay after the first layout pass, which needs a valid size
			_owner.TryEnablePopupOverlayLayer();

			// Re-center SizeToContent windows once Avalonia's layout has determined their actual
			// content size, which differs from the initial 400x300 default
			if (_owner._needsRecenter)
			{
				_owner._needsRecenter = false;
				var sceneTree = (SceneTree)Engine.GetMainLoop();
				var mainWinId = sceneTree.Root.GetWindowId();
				var mainWinPos = DisplayServer.WindowGetPosition(mainWinId);
				var mainWinSize = sceneTree.Root.Size;
				var subWinSize = window.Size;
				var centerPos = new Vector2I(
					mainWinPos.X + Math.Max((mainWinSize.X - subWinSize.X) / 2, 0),
					mainWinPos.Y + Math.Max((mainWinSize.Y - subWinSize.Y) / 2, 0)
				);
				window.CallDeferred(GdWindow.MethodName.SetPosition, centerPos);
			}

			_owner._topLevelImpl.OnDraw(new Rect(pixelSize.ToSize(1.0)));
			QueueRedraw();
		}

		public override void _Draw()
		{
			if (_owner._isDisposed)
				return;

			var surface = _owner._topLevelImpl.GetOrCreateSurface();
			DrawTexture(surface.GdTexture, Vector2.Zero);
		}

		public override void _GuiInput(InputEvent @event)
		{
			if (_owner._isDisposed || !_isEnabled)
			{
				if (!_isEnabled)
					_owner.GotInputWhenDisabled?.Invoke();

				return;
			}

			var handled = @event switch
			{
				InputEventMouseMotion motion => _owner._topLevelImpl.OnMouseMotion(motion, Time.GetTicksMsec()),
				InputEventMouseButton button => _owner._topLevelImpl.OnMouseButton(button, Time.GetTicksMsec()),
				InputEventScreenTouch touch => _owner._topLevelImpl.OnScreenTouch(touch, Time.GetTicksMsec()),
				InputEventScreenDrag drag => _owner._topLevelImpl.OnScreenDrag(drag, Time.GetTicksMsec()),
				InputEventKey key => _owner._topLevelImpl.OnKey(key, Time.GetTicksMsec()),
				InputEventJoypadButton jb => _owner._topLevelImpl.OnJoypadButton(jb, Time.GetTicksMsec()),
				InputEventJoypadMotion jm => _owner._topLevelImpl.OnJoypadMotion(jm, Time.GetTicksMsec()),
				_ => false
			};
			if (handled)
				AcceptEvent();
		}

		public void SetCursor(GdCursorShape cursorShape) => MouseDefaultCursorShape = cursorShape;
	}
}
