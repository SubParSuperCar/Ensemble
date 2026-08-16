using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Root.Ui.Impl;

public static class WebViewInitializer
{
	// TODO: Set experimental environment variables
	[SuppressMessage("Usage", "CA2255")]
	[ModuleInitializer]
	public static void InitializeWebView()
	{
		// Ignore
	}
}
