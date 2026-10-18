using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Scripts.Assets.Impl;
using Godot;
using Godot.Collections;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Assets.Components;

/// <summary>
///     Paints every geometry under the handle, except <see cref="Excluded" />, with a material override from the
///     instance's color and material properties, following their changes.
/// </summary>
[GlobalClass]
public partial class Paintable : AssetComponent
{
	public const string ColorKey = "_colorHex";
	public const string MaterialKey = "_materialId";

	private GeometryInstance3D[] _geometries = [];
	private GdProperties? _properties;

	[Export] public Array<GeometryInstance3D> Excluded { get; set; } = [];

	public override void _ExitTree() => _properties?.Changed -= OnPropertyChanged;

	protected override void OnInitialized()
	{
		_geometries = [.. GetGeometries(Handle).Except(Excluded)];
		_properties = Handle.Instance.Properties;

		Apply();
		_properties.Changed += OnPropertyChanged;
	}

	private static IEnumerable<GeometryInstance3D> GetGeometries(Node node) =>
		node.GetChildren().SelectMany(static child => child is GeometryInstance3D geometry
			? GetGeometries(child).Prepend(geometry)
			: GetGeometries(child));

	private void OnPropertyChanged(string key, Variant value)
	{
		if (string.Equals(key, ColorKey, StringComparison.OrdinalIgnoreCase) ||
			string.Equals(key, MaterialKey, StringComparison.OrdinalIgnoreCase))
			Apply();
	}

	private void Apply()
	{
		var hex = _properties!.GetValue(ColorKey).AsString();
		var color = Color.HtmlIsValid(hex) ? Color.FromHtml(hex) : Colors.White;
		var material = AssetMaterials.Get(_properties.GetValue(MaterialKey).AsInt32(), color);

		foreach (var geometry in _geometries)
			geometry.MaterialOverride = material;
	}
}
