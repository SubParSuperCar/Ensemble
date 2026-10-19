using System.Globalization;
using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Assets;

[GlobalClass]
public partial class AssetManager : Node
{
	// C# collections avoid interop on the placement hot path; GDScript reads them through the methods below
	private readonly System.Collections.Generic.Dictionary<int, Aabb> _boundariesByAssetId = [];
	private readonly System.Collections.Generic.Dictionary<int, string> _categoriesByAssetId = [];
	private readonly System.Collections.Generic.Dictionary<int, Aabb[]> _colliderBoundsByAssetId = [];
	private readonly System.Collections.Generic.Dictionary<int, (Shape3D, Transform3D)[]> _collidersByAssetId = [];
	private readonly System.Collections.Generic.Dictionary<int, PackedScene> _scenesByAssetId = [];
	private readonly System.Collections.Generic.Dictionary<int, (float, Vector3, float)> _volumesByAssetId = [];

	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxInstanceCount { get; set; }

	[GeneratedRegex(@"\.t?scn$", RegexOptions.None, RegexMatchTimeoutMs)]
	private static partial Regex SceneFileRegex { get; }

	public override void _EnterTree() => GAssetManager = this;

	public override void _Ready()
	{
		if (GAssets.IsLocked)
		{
			Log.Warning("{Class} is locked", nameof(GAssets));
			return;
		}

		Log.Debug("Registering assets from {Directory}...", BuildAssetsDir);

		ScanDirectory(BuildAssetsDir);
		GAssets.Lock();

		Log.Debug("Registered {Count} asset(s)", _scenesByAssetId.Count);
	}

	public override void _ExitTree()
	{
		if (ReferenceEquals(GAssetManager, this))
			GAssetManager = null!;
	}

	public PackedScene? GetPackedOrNull(int assetId) => _scenesByAssetId.GetValueOrDefault(assetId);

	public PackedScene GetPacked(int assetId) =>
		GetPackedOrNull(assetId) ?? throw new KeyNotFoundException(
			string.Create(CultureInfo.InvariantCulture, $"Packed scene with asset ID {assetId} not found."));

	public Aabb GetBoundary(int assetId) => _boundariesByAssetId.GetValueOrDefault(assetId);
	public string GetCategory(int assetId) => _categoriesByAssetId.GetValueOrDefault(assetId, string.Empty);

	public IReadOnlyList<Aabb> GetColliderBounds(int assetId) =>
		_colliderBoundsByAssetId.GetValueOrDefault(assetId, []);

	public IReadOnlyList<(Shape3D Shape, Transform3D Transform)> GetColliders(int assetId) =>
		_collidersByAssetId.GetValueOrDefault(assetId, []);

	/// <summary>
	///     Gets an instance's mass, from its material's density or else the asset's, and its center of mass.
	/// </summary>
	public (float Mass, Vector3 CenterOfMass) GetMass(int assetId, AssetMaterial? material)
	{
		var (volume, centroid, density) = _volumesByAssetId.GetValueOrDefault(assetId);
		return ((material?.Density ?? density) * volume, centroid);
	}

	private void ScanDirectory(string path)
	{
		foreach (var entry in ResourceLoader.ListDirectory(path))
		{
			var entryPath = path.PathJoin(entry);

			if (entry.EndsWith('/'))
				ScanDirectory(entryPath);
			else if (SceneFileRegex.IsMatch(entryPath))
				try
				{
					RegisterScene(entryPath);
				}
				catch (Exception exception)
				{
					Log.Error(exception, "Failed to register asset at {Path}", entryPath);
				}
		}
	}

	private void RegisterScene(string path)
	{
		var scene = GD.Load<PackedScene>(path);
		if (scene is null)
			return;

		var instance = scene.Instantiate();
		if (instance is not AssetHandle handle)
		{
			instance.Free();
			return;
		}

		var id = handle.AssetId;
		var name = handle.AssetName;
		var properties = handle.Properties;
		var maxInstanceCount = handle.MaxInstanceCount;
		var boundary = handle.BoundaryAabb;
		var colliderBounds = handle.GetColliderBounds();

		(Shape3D, Transform3D)[] colliders =
			[.. handle.GetColliders().Select(static collider => (collider.Shape, collider.Transform))];

		var (volume, centroid) = handle.CalculateVolume();
		var density = handle.Density;

		instance.Free();

		if (!_scenesByAssetId.TryAdd(id, scene))
		{
			Log.Warning("Skipped duplicate asset ID {AssetId} at {Path}", id, path);
			return;
		}

		_categoriesByAssetId.Add(id, path.GetBaseDir().TrimPrefix(BuildAssetsDir.TrimSuffix("/")).TrimPrefix("/"));
		_boundariesByAssetId.Add(id, boundary);
		_colliderBoundsByAssetId.Add(id, colliderBounds);
		_collidersByAssetId.Add(id, colliders);
		_volumesByAssetId.Add(id, (volume, centroid, density));

		var converted = new Dictionary();

		foreach (var (key, value) in properties)
			converted.Add(key.ToString(), value);

		GAssets.Add(id, name, converted, maxInstanceCount is Default ? DefaultMaxInstanceCount : maxInstanceCount);
	}
}
