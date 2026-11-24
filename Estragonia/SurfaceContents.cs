using Avalonia.Platform;

namespace Estragonia;

/// <summary>Identifies what a surface holds from the frames drawn into it, until a frame fails partway.</summary>
internal sealed class SurfaceContents : IPartialPresentationBuffer
{
	public bool IsValid { get; private set; } = true;

	public bool Equals(IPartialPresentationBuffer? other) => ReferenceEquals(this, other);

	public void Invalidate() => IsValid = false;
}
