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

	public override void _ExitTree() => _properties?.Changed -= OnPropertyChanged;

	protected override void OnInitialized()
	{
		_geometries =
		[
			.. Handle.Pieces.Prepend<Node>(Handle)
				.SelectMany(GetGeometries)
				.Except(Excluded)
				.Select(static geometry => (geometry, GetShading(geometry)))
		];
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
			string.Equals(key, ColorKey, StringComparison.OrdinalIgnoreCase) ||
			string.Equals(key, MaterialBank.PropertyKey, StringComparison.OrdinalIgnoreCase))
			Apply();
	}

	private void Apply()
	{
		var hex = _properties!.GetValue(ColorKey).AsString();
		var color = Color.HtmlIsValid(hex) ? Color.FromHtml(hex) : Colors.White;
		var material = MaterialBank.Get(_properties);

		foreach (var (geometry, shading) in _geometries)
			geometry.MaterialOverride = Paints.Get(material, color, shading);
	}
}
