#if DEBUG

using Avalonia;

namespace Root.Ui.Impl;

public static class Designer
{
	public static int Main() =>
		throw new NotSupportedException("This project isn't meant to be run; it's only for Avalonia designer support.");

	// ReSharper disable once UnusedMember.Global
	// TODO
	public static AppBuilder BuildAvaloniaApp() =>
		AppBuilder
			.Configure<App>()
	/*.UseSkia()
	.UseHarfBuzz()*/;
}

#endif
