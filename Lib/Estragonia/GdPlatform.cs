using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls.Platform;
using Avalonia.Dialogs;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Estragonia.Input;
using Godot;
using AvCompositor = Avalonia.Rendering.Composition.Compositor;

namespace Estragonia;

/// <summary>Contains Godot to Avalonia platform initialization.</summary>
internal static class GodotPlatform
{
	private static AvCompositor? _sCompositor;
	private static ManualRenderTimer? _sRenderTimer;
	private static ulong _sLastProcessFrame = ulong.MaxValue;

	public static AvCompositor Compositor
		=> _sCompositor ?? throw new InvalidOperationException($"{nameof(GodotPlatform)} hasn't been initialized");

	public static void Initialize()
	{
		AvaloniaSynchronizationContext.AutoInstall = false; // Godot has its own sync context, don't replace it

		var platformGraphics = GodotPlatformGraphicsFactory.Create();
		var renderTimer = new ManualRenderTimer();

		AvaloniaLocator.CurrentMutable
			.Bind<IClipboard>().ToConstant(new GodotClipboard())
			.Bind<ICursorFactory>().ToConstant(new GodotCursorFactory())
			.Bind<IDispatcherImpl>().ToConstant(new GodotDispatcherImpl(Thread.CurrentThread))
			.Bind<IKeyboardDevice>().ToConstant(GodotDevices.Keyboard)
			.Bind<IPlatformGraphics>().ToConstant(platformGraphics)
			.Bind<IPlatformIconLoader>().ToConstant(new StubPlatformIconLoader())
			.Bind<IPlatformSettings>().ToConstant(new GodotPlatformSettings())
			.Bind<IRenderTimer>().ToConstant(renderTimer)
			.Bind<IWindowingPlatform>().ToConstant(new GodotWindowingPlatform())
			.Bind<IStorageProviderFactory>().ToConstant(new GodotStorageProviderFactory())
			.Bind<PlatformHotkeyConfiguration>().ToConstant(CreatePlatformHotKeyConfiguration())
			.Bind<ManagedFileDialogOptions>()
			.ToConstant(new ManagedFileDialogOptions { AllowDirectorySelection = true });

		_sRenderTimer = renderTimer;
		_sCompositor = new AvCompositor(platformGraphics);
	}

	private static PlatformHotkeyConfiguration CreatePlatformHotKeyConfiguration() =>
		OperatingSystem.IsMacOS()
			? new PlatformHotkeyConfiguration(KeyModifiers.Meta, wholeWordTextActionModifiers: KeyModifiers.Alt)
			: new PlatformHotkeyConfiguration(KeyModifiers.Control);

	public static void TriggerRenderTick()
	{
		if (_sRenderTimer is null)
			return;

		// if we have several AvaloniaControls, ensure we tick the timer only once each frame
		var processFrame = Engine.GetProcessFrames();
		if (processFrame == _sLastProcessFrame)
			return;

		_sLastProcessFrame = processFrame;
		_sRenderTimer.TriggerTick(new TimeSpan((long)(Time.GetTicksUsec() * 10UL)));
	}
}
