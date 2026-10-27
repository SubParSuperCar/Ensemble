using System.Globalization;
using System.Text.RegularExpressions;
using EnsembleRoot.Scripts.Assets.Materials;
using Godot;
using Godot.Collections;
using Serilog;

namespace EnsembleRoot.Scripts.Assets;

[GlobalClass]
public partial class AssetManager : Node
{
	// A C# dictionary avoids interop on the placement hot path; GDScript reads it through the methods below
	private readonly System.Collections.Generic.Dictionary<int, AssetInfo> _assetsById = [];

	// ReSharper disable once MemberCanBePrivate.Global
	[Export(PropertyHint.Range, "-1,0,1,or_greater,hide_slider")]
	public int DefaultMaxInstanceCount { get; set; }

	[GeneratedRegex(@"\.t?scn$", RegexOptions.None, RegexMatchTimeoutMs)]
	private static partial Regex SceneFileRegex { get; }

	public override void _EnterTree() => GAssetManager = this;

	public override void _Ready()
	{
		// A locked registry outlived an earlier world, so its assets are adopted rather than added
		if (GAssets.IsLocked)
			Log.Warning("{Class} is locked; adopting its assets", nameof(GAssets));

		Log.Debug("Registering assets from {Directory}...", BuildAssetsDir);

		ScanDirectory(BuildAssetsDir);
		GAssets.Lock();

		Log.Debug("Registered {Count} asset(s)", _assetsById.Count);
	}

	public override void _ExitTree()
	{
		if (ReferenceEquals(GAssetManager, this))
			GAssetManager = null!;
	}

	public PackedScene? GetPackedOrNull(int assetId) => _assetsById.GetValueOrDefault(assetId)?.Scene;

	public PackedScene GetPacked(int assetId) =>
		GetPackedOrNull(assetId) ?? throw new KeyNotFoundException(
			string.Create(CultureInfo.InvariantCulture, $"Packed scene with asset ID {assetId} not found."));

	public Aabb GetBoundary(int assetId) => _assetsById.GetValueOrDefault(assetId)?.Boundary ?? default;
	public string GetCategory(int assetId) => _assetsById.GetValueOrDefault(assetId)?.Category ?? string.Empty;
	public string? GetDescription(int assetId) => _assetsById.GetValueOrDefault(assetId)?.Description;

	public int GetPieceCount(int assetId) => _assetsById.GetValueOrDefault(assetId)?.Pieces.Length ?? 0;

	/// <summary>Gets a piece's collider bounds, in its handle's space.</summary>
	public IReadOnlyList<Aabb> GetColliderBounds(int assetId, int piece) => GetPiece(assetId, piece).Bounds;

	/// <summary>Gets a piece's collider shapes, with their transforms in its handle's space.</summary>
	public IReadOnlyList<(Shape3D Shape, Transform3D Transform)> GetColliders(int assetId, int piece) =>
		GetPiece(assetId, piece).Colliders;

	/// <summary>
	///     Gets a piece's mass, from its instance's material's density or else the asset's, and its center of mass, in
	///     its handle's space.
	/// </summary>
	public (float Mass, Vector3 CenterOfMass) GetMass(int assetId, int piece, AssetMaterial? material)
	{
		var info = GetPiece(assetId, piece);
		var density = material?.Density ?? _assetsById.GetValueOrDefault(assetId)?.Density ?? 0f;

		return (density * info.Volume, info.Centroid);
	}

	private PieceInfo GetPiece(int assetId, int piece) =>
		_assetsById.TryGetValue(assetId, out var asset) && piece < asset.Pieces.Length
			? asset.Pieces[piece]
			: PieceInfo.Empty;

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

		try
		{
			if (instance is AssetHandle handle)
				Register(handle, scene, path);
		}
		finally
		{
			instance.Free();
		}
	}

	private void Register(AssetHandle handle, PackedScene scene, string path)
	{
		var id = handle.AssetId;
		var category = path.GetBaseDir().TrimPrefix(BuildAssetsDir.TrimSuffix("/")).TrimPrefix("/");
		var description = handle.Description is { IsEmpty: false } text ? text.ToString() : null;
		var pieces = Enumerable.Range(0, handle.PieceCount).Select(piece => GetPieceInfo(handle, piece)).ToArray();
		var asset = new AssetInfo(scene, category, description, handle.BoundaryAabb, handle.Density, pieces);

		WarnAboutStructure(handle, path);

		if (!_assetsById.TryAdd(id, asset))
		{
			Log.Warning("Skipped duplicate asset ID {AssetId} at {Path}", id, path);
			return;
		}

		var properties = new Dictionary();

		foreach (var (key, value) in handle.Properties)
			properties.Add(key.ToString(), value);

		var maxInstanceCount = handle.MaxInstanceCount is Default ? DefaultMaxInstanceCount : handle.MaxInstanceCount;

		if (!GAssets.IsLocked)
			GAssets.Add(id, handle.AssetName, properties, maxInstanceCount);
		else if (GAssets.GetAsset(id) is null)
			Log.Warning("Asset with ID {AssetId} is missing from the locked {Class}", id, nameof(GAssets));
	}

	private static PieceInfo GetPieceInfo(AssetHandle handle, int piece)
	{
		var (volume, centroid) = handle.CalculateVolume(piece);

		return new PieceInfo(
			[.. handle.GetColliders(piece).Select(static collider => (collider.Collider.Shape, collider.Transform))],
			handle.GetColliderBounds(piece), volume, centroid);
	}

	// Ensemble only moves the handle's own colliders and its AssetPiece children, and links only their joints, so
	// anything else would quietly misbehave once spawned
	private static void WarnAboutStructure(AssetHandle handle, string path)
	{
		foreach (var (joint, pieceA, pieceB) in handle.Joints)
			if (pieceA is null || pieceB is null || pieceA == pieceB)
				Log.Warning(
					"Asset at {Path} has joint {Joint}, which must link two of its pieces (the handle or its pieces)",
					path, joint.Name);

		foreach (var node in handle.FindChildren("*", nameof(PhysicsBody3D)))
			if (node is not AssetPiece || node.GetParent() != handle)
				Log.Warning(
					"Asset at {Path} has body {Body}, which is ignored: moving parts must be AssetPiece children",
					path, handle.GetPathTo(node));

		foreach (var node in handle.FindChildren("*", nameof(Joint3D)))
			if (node.GetParent() != handle)
				Log.Warning(
					"Asset at {Path} has joint {Joint}, which is ignored: joints must be children of the handle",
					path, handle.GetPathTo(node));
	}

	private sealed record AssetInfo(
		PackedScene Scene,
		string Category,
		string? Description,
		Aabb Boundary,
		float Density,
		PieceInfo[] Pieces);

	private sealed record PieceInfo((Shape3D, Transform3D)[] Colliders, Aabb[] Bounds, float Volume, Vector3 Centroid)
	{
		public static PieceInfo Empty { get; } = new([], [], 0, default);
	}
}
