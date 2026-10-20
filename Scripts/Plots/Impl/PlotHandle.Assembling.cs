using System.Globalization;
using System.Runtime.InteropServices;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Plots.Impl;
using Godot;

namespace EnsembleRoot.Scripts.Plots;

public partial class PlotHandle
{
	private const uint AssemblyMask =
		PhysicsLayers.World | PhysicsLayers.Character | PhysicsLayers.StaticInstances | PhysicsLayers.DynamicInstances;

	private readonly List<Assembly> _assemblies = [];

	private Node3D _dynamicInstances = null!;

	/// <summary>The live creation's assemblies, each at the index that is its ID.</summary>
	public IReadOnlyList<Assembly> Assemblies => _assemblies;

	/// <summary>
	///     Builds the live creation: welds every instance's pieces into groups, makes one assembly per group, links
	///     the pieces' joints, and initializes the handles. Every peer builds the same assemblies, with the same IDs,
	///     from the plot's instances, so spawning sends no geometry.
	/// </summary>
	private void BuildCreation()
	{
		var instances = _plot.Instances.GetAll().OrderBy(static instance => instance.Id).ToList();
		var handles = instances.ToDictionary(
			static instance => instance.Id, static instance => CreateHandle(instance, GetTransform(instance)));

		// A handle rides the assembly of its first welded piece, its own colliders (piece 0) unless it has none
		var parts = instances
			.SelectMany(static instance => GetWeldedPieces(instance.Asset.Id)
				.Select((piece, order) => new WeldedPiece(instance, piece, order is 0)))
			.ToList();

		var groups = Welds.Group([.. parts.Select(static part => (part.Instance.Id, GetColliderBoxes(part)))]);

		for (var id = 0; id < groups.Count; id++)
			AddAssembly(id, [.. groups[id].Select(index => parts[index])], handles);

		foreach (var handle in handles.Values)
		{
			// Pieces without colliders ride along with their handle
			foreach (var piece in handle.Pieces)
				piece.Assembly ??= handle.Assembly;

			handle.ConnectJoints();
		}

		foreach (var instance in instances)
			handles[instance.Id].Initialize(this, instance);
	}

	// The pieces with colliders weld; an asset without any still gets an assembly for its handle to ride
	private static List<int> GetWeldedPieces(int assetId)
	{
		var pieces = Enumerable.Range(0, GAssetManager.GetPieceCount(assetId))
			.Where(piece => GAssetManager.GetColliders(assetId, piece).Count is not 0)
			.ToList();

		return pieces is [] ? [0] : pieces;
	}

	private static Obb[] GetColliderBoxes(WeldedPiece part)
	{
		var transform = GetTransform(part.Instance);
		var bounds = GAssetManager.GetColliderBounds(part.Instance.Asset.Id, part.Piece);

		return [.. bounds.Select(aabb => Obb.From(aabb, transform))];
	}

	private void AddAssembly(int id, WeldedPiece[] parts, Dictionary<int, AssetHandle> handles)
	{
		var assembly = new Assembly
		{
			Name = id.ToString(CultureInfo.InvariantCulture),
			Id = id,
			Transform = _staticInstances.GlobalTransform,
			Freeze = true,
			CollisionLayer = PhysicsLayers.DynamicInstances,
			CollisionMask = AssemblyMask
		};

		foreach (var (instance, piece, isRidden) in parts)
		{
			var handle = handles[instance.Id];
			var transform = GetTransform(instance);
			var material = MaterialBank.Get(instance.Properties);

			if (isRidden)
			{
				assembly.AddPiece(handle, transform, instance.Asset.Id, 0, material);
				handle.Assembly = assembly;
			}

			if (piece is 0)
				continue;

			assembly.AddPiece(handle.Pieces[piece - 1], transform, instance.Asset.Id, piece, material);
			handle.Pieces[piece - 1].Assembly = assembly;
		}

		assembly.Build();
		_dynamicInstances.AddChild(assembly);
		assembly.Assemble();
		assembly.IsSimulated = IsSimulatedLocally;
		assembly.ResetPhysicsInterpolation();

		_assemblies.Add(assembly);
	}

	// One weld part: an instance's piece, and whether its handle rides the piece's assembly
	[StructLayout(LayoutKind.Auto)]
	private readonly record struct WeldedPiece(GdInstance Instance, int Piece, bool IsRidden);
}
