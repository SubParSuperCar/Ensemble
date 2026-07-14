using System.IO;
using Avalonia.Platform;

namespace Estragonia;

/// <summary>A fake window icon that can't be displayed but can still be saved.</summary>
internal sealed class StubWindowIconImpl(MemoryStream stream) : IWindowIconImpl
{
	public void Save(Stream outputStream)
	{
		stream.Position = 0L;
		stream.CopyTo(outputStream);
	}
}
