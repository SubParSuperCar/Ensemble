using System;
using Avalonia.Platform;

namespace Estragonia;

internal sealed class GodotWindowingPlatform : IWindowingPlatform
{
	public IWindowImpl CreateWindow() => throw CreateNotImplementedException();

	public IWindowImpl CreateEmbeddableWindow() => throw CreateNotImplementedException();

	public ITopLevelImpl CreateEmbeddableTopLevel() => throw CreateNotImplementedException();

	public ITrayIconImpl? CreateTrayIcon() => null;

	private static NotImplementedException CreateNotImplementedException() => new("Sub windows aren't implemented yet");
}
