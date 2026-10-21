using System.Globalization;
using System.Text.RegularExpressions;
using EnsembleRoot.Scripts.Assets.Materials;
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
	private readonly System.Collections.Generic.Dictionary<int, float> _densitiesByAssetId = [];
	private readonly System.Collections.Generic.Dictionary<int, string> _descriptionsByAssetId = [];
	private readonly System.Collections.Generic.Dictionary<int, PieceInfo[]> _piecesByAssetId = [];
	private readonly System.Collections.Generic.Dictionary<int, PackedScene> _scenesByAssetId = [];

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
	public string? GetDescription(int assetId) => _descriptionsByAssetId.GetValueOrDefault(assetId);

	public int GetPieceCount(int assetId) => _piecesByAssetId.GetValueOrDefault(assetId, []).Length;

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
		return ((material?.Density ?? _densitiesByAssetId.GetValueOrDefault(assetId)) * info.Volume, info.Centroid);
	}

	private PieceInfo GetPiece(int assetId, int piece) =>
		_piecesByAssetId.TryGetValue(assetId, out var pieces) && piece < pieces.Length
			? pieces[piece]
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

		// The instance is only read, so it is freed even when reading it throws
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
		var boundary = handle.BoundaryAabb;
		var pieces = Enumerable.Range(0, handle.PieceCount).Select(piece => GetPieceInfo(handle, piece)).ToArray();

		WarnAboutStructure(handle, path);

		if (!_scenesByAssetId.TryAdd(id, scene))
		{
			Log.Warning("Skipped duplicate asset ID {AssetId} at {Path}", id, path);
			return;
		}

		_categoriesByAssetId.Add(id, path.GetBaseDir().TrimPrefix(BuildAssetsDir.TrimSuffix("/")).TrimPrefix("/"));
		_boundariesByAssetId.Add(id, boundary);
		_piecesByAssetId.Add(id, pieces);
		_densitiesByAssetId.Add(id, handle.Density);

		if (handle.Description is { IsEmpty: false } description)
			_descriptionsByAssetId.Add(id, description.ToString());

		var properties = new Dictionary();

		foreach (var (key, value) in handle.Properties)
			properties.Add(key.ToString(), value);

		var maxInstanceCount = handle.MaxInstanceCount is Default ? DefaultMaxInstanceCount : handle.MaxInstanceCount;
		GAssets.Add(id, handle.AssetName, properties, maxInstanceCount);
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

	// A piece's colliders and their bounds in its handle's space, and its volume and centroid
	private sealed record PieceInfo((Shape3D, Transform3D)[] Colliders, Aabb[] Bounds, float Volume, Vector3 Centroid)
	{
		public static PieceInfo Empty { get; } = new([], [], 0, default);
	}
}
