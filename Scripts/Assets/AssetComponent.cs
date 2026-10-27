using Godot;

namespace EnsembleRoot.Scripts.Assets;

/// <summary>
///     A reusable asset behavior, placed in an asset's scene as a direct child of its <see cref="AssetHandle" />. A
///     handle lives in one mode for its whole life (build or live), so components never handle spawning themselves.
/// </summary>
public abstract partial class AssetComponent : Node
{
	// ReSharper disable once MemberCanBeProtected.Global
	public AssetHandle Handle { get; private set; } = null!;

	internal void Initialize(AssetHandle handle)
	{
		Handle = handle;
		OnInitialized();
	}

	/// <summary>
	///     Called once the handle's plot, instance, and assembly (if live) are set, and its creation exists.
	/// </summary>
	protected virtual void OnInitialized() { }
}
