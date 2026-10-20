using System.Globalization;
using System.Runtime.InteropServices;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Scripts.Assets;
using EnsembleRoot.Scripts.Assets.Materials;
using EnsembleRoot.Scripts.Creations.Impl;
using EnsembleRoot.Scripts.Creations.Packets;
using EnsembleRoot.Scripts.Plots;
using EnsembleRoot.Scripts.Plots.Placement;
using Serilog;

namespace EnsembleRoot.Scripts.Creations;

public partial class LiveCreation
{
	private const uint AssemblyMask =
		PhysicsLayers.World | PhysicsLayers.Character | PhysicsLayers.StaticInstances | PhysicsLayers.DynamicInstances;

	/// <summary>
	///     Builds the creation: welds every instance's pieces into groups, makes one assembly per group, links the
	///     pieces' joints, and initializes the handles. Every peer builds the same assemblies, with the same IDs, from
	///     the plot's instances, so spawning sends no geometry.
	/// </summary>
	/// <remarks>The creation must be in the tree, as each assembly's pieces leave the space it enters.</remarks>
	internal void Build(IEnumerable<GdInstance> plotInstances)
	{
		var instances = plotInstances.OrderBy(static instance => instance.Id).ToList();
		var handles = instances.ToDictionary(static instance => instance.Id, PlotHandle.CreateHandle);

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
			handles[instance.Id].Initialize(Plot, instance);

		// Streamed states carry 16-bit IDs, which only creations far beyond any plot's quota exceed
		if (PlotHandle.IsStreaming && _assemblies.Count > AssemblyState.MaxId + 1)
			Log.Warning(
				"Plot {PlotId} has {Count} assemblies, and other peers won't see those past the first {MaxCount} move",
				Plot.Id, _assemblies.Count, AssemblyState.MaxId + 1);
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
		var transform = PlotHandle.GetInstanceTransform(part.Instance);
		var bounds = GAssetManager.GetColliderBounds(part.Instance.Asset.Id, part.Piece);

		return [.. bounds.Select(aabb => Obb.From(aabb, transform))];
	}

	private void AddAssembly(int id, WeldedPiece[] parts, Dictionary<int, AssetHandle> handles)
	{
		var assembly = new Assembly
		{
			Name = id.ToString(CultureInfo.InvariantCulture),
			Id = id,
			Transform = Origin,
			Freeze = true,
			CollisionLayer = PhysicsLayers.DynamicInstances,
			CollisionMask = AssemblyMask
		};

		foreach (var (instance, piece, isRidden) in parts)
		{
			var handle = handles[instance.Id];
			var transform = PlotHandle.GetInstanceTransform(instance);
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
		AddChild(assembly);
		assembly.Assemble();
		assembly.IsSimulated = Plot.IsSimulatedLocally;
		assembly.ResetPhysicsInterpolation();

		_assemblies.Add(assembly);
	}

	// One weld part: an instance's piece, and whether its handle rides the piece's assembly
	[StructLayout(LayoutKind.Auto)]
	private readonly record struct WeldedPiece(GdInstance Instance, int Piece, bool IsRidden);
}
