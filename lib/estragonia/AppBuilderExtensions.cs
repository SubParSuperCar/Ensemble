using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Dialogs;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Estragonia;

/// <summary>Contains extensions methods for <see cref="AppBuilder" /> related to Godot.</summary>
public static class AppBuilderExtensions
{
	extension(AppBuilder builder)
	{
		/// <summary>
		///     Configures Avalonia to use the Godot platform backend.
		///     Call <see cref="SetupWithGodot" /> instead of <see cref="AppBuilder.SetupWithoutStarting" />
		///     to enable <see cref="IClassicDesktopStyleApplicationLifetime" /> support.
		/// </summary>
		public AppBuilder UseGodot()
		{
			// Register PlatformHotkeyConfiguration early so it's available
			// when UrsaSemiTheme XAML is loaded during App.Initialize().
			AvaloniaLocator.CurrentMutable
				.Bind<PlatformHotkeyConfiguration>()
				.ToConstant(OperatingSystem.IsMacOS()
					? new PlatformHotkeyConfiguration(KeyModifiers.Meta, wholeWordTextActionModifiers: KeyModifiers.Alt)
					: new PlatformHotkeyConfiguration(KeyModifiers.Control));

#pragma warning disable CA1416
			return builder
				.UseManagedSystemDialogs()
#pragma warning restore CA1416
				.UseStandardRuntimePlatformSubsystem()
				.UseSkia()
				.UseHarfBuzz()
				.UseWindowingSubsystem(GodotPlatform.Initialize);
		}

		/// <summary>
		///     Sets up the Godot platform with <see cref="IClassicDesktopStyleApplicationLifetime" /> support.
		///     This enables <c>Application.Current.ApplicationLifetime</c> to return a valid desktop lifetime,
		///     which is required for <c>Window.ShowDialog()</c> to find an owner window.
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public AppBuilder SetupWithGodot() => builder.SetupWithLifetime(GodotPlatform.CreateApplicationLifetime());
	}
}
