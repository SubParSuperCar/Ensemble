using System;
using System.Threading;
using Avalonia;
using Avalonia.Dialogs;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Estragonia.Input;
using Godot;
using AvCompositor = Avalonia.Rendering.Composition.Compositor;
using AvWindow = Avalonia.Controls.Window;

namespace Estragonia;

/// <summary>Contains Godot to Avalonia platform initialization.</summary>
internal static class GodotPlatform
{
	private static AvCompositor? _compositor;
	private static ManualRenderTimer? _renderTimer;
	private static ulong _lastProcessFrame = ulong.MaxValue;
	private static GodotApplicationLifetime? _lifetime;

	/// <summary>
	///     Set to <c>true</c> while <c>ManagedFileDialogOptions.ContentRootFactory</c> is invoked, so that
	///     <see cref="GodotWindowImpl" /> can detect that the resulting window is a managed file dialog and
	///     configure its modal behavior accordingly.
	///     <para>
	///         This flag relies on <see cref="ThreadStaticAttribute" /> and must only be read and written on the
	///         thread that set it. The <c>ContentRootFactory</c> delegate runs synchronously, so the try/finally
	///         below is safe. Don't introduce an <c>await</c> between setting and clearing the flag, as the
	///         continuation may run on a different thread.
	///     </para>
	/// </summary>
	[ThreadStatic] internal static bool IsManagedDialogWindow;

	public static AvCompositor Compositor =>
		_compositor ?? throw new InvalidOperationException($"{nameof(GodotPlatform)} hasn't been initialized");

	/// <summary>
	///     Creates and initializes the <see cref="GodotApplicationLifetime" /> used with
	///     <see cref="AppBuilder.SetupWithLifetime" />.
	/// </summary>
	public static GodotApplicationLifetime CreateApplicationLifetime()
	{
		_lifetime = new GodotApplicationLifetime();
		_lifetime.Initialize();
		return _lifetime;
	}

	public static void Initialize()
	{
		AvaloniaSynchronizationContext.AutoInstall = false; // Godot has its own sync context, don't replace it

		var platformGraphics = GodotPlatformGraphicsFactory.Create();
		var renderTimer = new ManualRenderTimer();

		// Force managed file dialogs down the Window path instead of the Popup path. In Godot mode the parent
		// TopLevel is a GodotTopLevel rather than a Window, so ManagedStorageProvider.ShowAsPopup() would fail
		// for lack of a Panel in the visual tree. Returning a Window from ContentRootFactory makes PrepareRoot()
		// yield a Window, so Show() takes the ShowAsWindow() path and creates an overlay window through
		// GodotWindowingPlatform.CreateWindow(). IsManagedDialogWindow is set while the factory runs so that
		// GodotWindowImpl can set up Godot's Transient and Exclusive flags for modal blocking. The size is fixed
		// because SizeToContent.WidthAndHeight makes the dialog grow along the Y axis every time it's reopened,
		// as Avalonia's ManagedFileChooser measures a larger height each time from its accumulated quick links
		// and volumes state.
		var fileDialogOptions = new ManagedFileDialogOptions
		{
			AllowDirectorySelection = true,
			ContentRootFactory = () =>
			{
				IsManagedDialogWindow = true;

				try
				{
					return new AvWindow
					{
						Width = 900,
						Height = 563
					};
				}
				finally
				{
					IsManagedDialogWindow = false;
				}
			}
		};

		AvaloniaLocator.CurrentMutable
			.Bind<IClipboard>().ToConstant(new GodotClipboard())
			.Bind<ICursorFactory>().ToConstant(new GodotCursorFactory())
			.Bind<IDispatcherImpl>().ToConstant(new GodotDispatcherImpl(Thread.CurrentThread))
			.Bind<IKeyboardDevice>().ToConstant(GodotDevices.Keyboard)
			.Bind<IPlatformGraphics>().ToConstant(platformGraphics)
			.Bind<IPlatformIconLoader>().ToConstant(new StubPlatformIconLoader())
			.Bind<IPlatformSettings>().ToConstant(new GodotPlatformSettings())
			.Bind<IRenderTimer>().ToConstant(renderTimer)
			.Bind<IRenderLoop>().ToConstant(RenderLoop.FromTimer(renderTimer))
			.Bind<IWindowingPlatform>().ToConstant(new GodotWindowingPlatform())
			.Bind<ManagedFileDialogOptions>().ToConstant(fileDialogOptions);

		_renderTimer = renderTimer;
		_compositor = new AvCompositor(platformGraphics);
	}

	public static void TriggerRenderTick()
	{
		if (_renderTimer is null)
			return;

		// If several AvaloniaControls exist, ensure the timer only ticks once per frame
		var processFrame = Engine.GetProcessFrames();

		if (processFrame == _lastProcessFrame)
			return;

		_lastProcessFrame = processFrame;
		_renderTimer.TriggerTick(new TimeSpan((long)(Time.GetTicksUsec() * 10UL)));
	}
}
