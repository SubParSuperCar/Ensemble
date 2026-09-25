#if ENSEMBLE_DEBUG
using Avalonia;

namespace EnsembleRoot.Ui.Impl;

public static class Designer
{
	public static int Main() =>
		throw new NotSupportedException(
			"This project is not meant to be run; it exists only for Avalonia designer support.");

	// ReSharper disable once UnusedMember.Global
	public static AppBuilder BuildAvaloniaApp() =>
		AppBuilder
			.Configure<App>()
			.UseSkia()
			.UseHarfBuzz();
}

#endif
