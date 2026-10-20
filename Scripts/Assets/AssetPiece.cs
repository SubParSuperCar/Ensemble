using EnsembleRoot.Scripts.Creations;
using Godot;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EnsembleRoot.Scripts.Assets;

/// <summary>
///     A part of an asset that moves on its own while live, like a suspension's top or a motor's rotor, placed in the
///     asset's scene as a direct child of its <see cref="AssetHandle" />, with its own colliders and meshes as direct
///     children. The handle's own colliders form piece 0, and each piece welds to its neighbors apart from the rest of
///     the asset; joints between pieces are ordinary Godot joints, children of the handle.
/// </summary>
[GlobalClass]
public partial class AssetPiece : StaticBody3D
{
	public AssetHandle Handle { get; internal set; } = null!;

	/// <summary>The piece's index within its asset, from 1, as the handle's own colliders are piece 0.</summary>
	public int Index { get; internal set; }

	/// <summary>The assembly carrying this piece while its plot is spawned, set for the piece's whole life.</summary>
	public Assembly? Assembly { get; internal set; }
}
