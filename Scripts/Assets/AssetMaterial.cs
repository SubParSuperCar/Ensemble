using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Assets;

/// <summary>
///     What an instance is made of, chosen by its material ID property (see <see cref="MaterialBank" />): how it looks
///     and how it behaves physically.
/// </summary>
[GlobalClass]
public partial class AssetMaterial : Resource
{
	public const float DefaultFriction = 1f;

	/// <summary>Painted geometry uses a copy of it, with the albedo tinted by the instance's color.</summary>
	[Export]
	public BaseMaterial3D? Look { get; set; }

	[Export(PropertyHint.Range, "0,0,0.001,or_greater,hide_slider,suffix:kg/m\u00b3")]
	public float Density { get; set; } = AssetHandle.DefaultDensity;

	[Export(PropertyHint.Range, "0,1,0.001")]
	public float Friction { get; set; } = DefaultFriction;

	[Export(PropertyHint.Range, "0,1,0.001")]
	public float Bounce { get; set; }
}
