using System.Runtime.CompilerServices;

namespace Root.Ui;

public static class WebViewPreparer
{
	[ModuleInitializer]
	public static void PrepareWebView()
	{
		Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS",
			"--no-sandbox --disable-gpu-sandbox --disable-features=WebAssemblyTrapHandler");
		Environment.SetEnvironmentVariable("WPE_BCM_DECLARE_BUFFERS", "0");
		Environment.SetEnvironmentVariable("WEBKIT_DISABLE_COMPOSITING_MODE", "1");
	}
}
