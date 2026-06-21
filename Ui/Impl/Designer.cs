#if DEBUG

using Avalonia;

// ReSharper disable UnusedMember.Global

namespace Root.Ui.Impl;

// ReSharper disable once UnusedType.Global
public static class Designer
{
	public static int Main() =>
		throw new NotSupportedException("This project isn't meant to be run: it's only for Avalonia designer support.");

	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder
			.Configure<App>()
			.UseSkia();
}

#endif
