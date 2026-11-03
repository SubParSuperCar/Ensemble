using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Scripts.Assets.Materials;
using Godot;
using Godot.Collections;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Assets.Components;

/// <summary>
///     Paints every geometry of the handle and its pieces, except <see cref="Excluded" />, with a material override
///     from the instance's color and material (see <see cref="MaterialBank" />), following their changes.
/// </summary>
[GlobalClass]
public partial class Paintable : AssetComponent
{
	public const string ColorKey = "_colorHex";

	private (GeometryInstance3D Geometry, BaseMaterial3D.ShadingModeEnum Shading)[] _geometries = [];
	private GdProperties? _properties;

	[Export] public Array<GeometryInstance3D> Excluded { get; set; } = [];

	/// <summary>Whether the color is <c>RRGGBBAA</c>, so only assets meant to be see-through render as transparent.</summary>
	[Export]
	public bool HasAlpha { get; set; }

	public override void _ExitTree() => _properties?.Changed -= OnPropertyChanged;

	/// <summary>Paints a handle that has no instance (a preview) as its asset's default properties would.</summary>
	public void PaintDefaults(AssetHandle handle)
	{
		CollectGeometries(handle);

		handle.Properties.TryGetValue(ColorKey, out var hex);

		Paint(
			hex.AsString(),
			handle.Properties.TryGetValue(MaterialBank.PropertyKey, out var material)
				? MaterialBank.Get(material.AsInt32())
				: null);
	}

	protected override void OnInitialized()
	{
		CollectGeometries(Handle);

		_properties = Handle.Instance.Properties;

		Apply();
		_properties.Changed += OnPropertyChanged;
	}

	// Each piece is walked on its own, as a live creation moves the pieces out from under the handle
	private static IEnumerable<GeometryInstance3D> GetGeometries(Node node) =>
		node.GetChildren()
			.Where(static child => child is not AssetPiece)
			.SelectMany(static child => child is GeometryInstance3D geometry
				? GetGeometries(child).Prepend(geometry)
				: GetGeometries(child));

	// Paint keeps the shading the asset was authored with, as per-vertex shading is far cheaper for many instances
	private static BaseMaterial3D.ShadingModeEnum GetShading(GeometryInstance3D geometry) =>
		geometry is MeshInstance3D { Mesh: not null } mesh && mesh.GetActiveMaterial(0) is BaseMaterial3D material
			? material.ShadingMode
			: BaseMaterial3D.ShadingModeEnum.PerPixel;

	private void OnPropertyChanged(string key, Variant value)
	{
		if (
			IProperties.KeyComparer.Equals(key, ColorKey) ||
			IProperties.KeyComparer.Equals(key, MaterialBank.PropertyKey))
			Apply();
	}

	private void CollectGeometries(AssetHandle handle) =>
		_geometries =
		[
			.. handle.Pieces.Prepend<Node>(handle)
				.SelectMany(GetGeometries)
				.Except(Excluded)
				.Select(static geometry => (geometry, GetShading(geometry)))
		];

	private void Apply() => Paint(_properties!.GetValue(ColorKey).AsString(), MaterialBank.Get(_properties));

	private void Paint(string hex, AssetMaterial? material)
	{
		var color = Color.HtmlIsValid(hex) ? Color.FromHtml(hex) : Colors.White;

		if (!HasAlpha)
			color.A = 1;

		foreach (var (geometry, shading) in _geometries)
			geometry.MaterialOverride = Paints.Get(material, color, shading);
	}
}
