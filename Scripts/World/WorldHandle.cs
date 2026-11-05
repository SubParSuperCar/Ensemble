using Godot;
using Serilog;
#if DEBUG
using EnsembleRoot.Scripts.Plots.Placement;
using EnsembleRoot.Sessions.Api;
#endif

namespace EnsembleRoot.Scripts.World;

[GlobalClass]
public partial class WorldHandle : Node3D
{
	public override void _Ready()
	{
#if DEBUG
		if (GSessionManager.Mode is SessionMode.SinglePlayer)
			AddTestData();
#endif

		GSessionManager.SnapshotsFinished += OnSnapshotsFinished;
	}

	public override void _ExitTree() => GSessionManager.SnapshotsFinished -= OnSnapshotsFinished;

	// Clients only have the real state once their snapshots are restored
	private static void OnSnapshotsFinished()
	{
		Log.Debug("{Member} ({Count}):", nameof(GPlayers), GPlayers.Count);
		foreach (var player in GPlayers.GetAll().OrderBy(static player => player.Id, StringComparer.Ordinal))
			Log.Debug("{$Player}", player.ToDict());

		Log.Debug("{Member} ({Count}):", nameof(GAssets), GAssets.Count);
		foreach (var asset in GAssets.GetAll().OrderBy(static asset => asset.Id))
			Log.Debug("{$Asset}", asset.ToDict());

		Log.Debug("{Member} ({Count}):", nameof(GPlots), GPlots.Count);
		foreach (var plot in GPlots.GetAll().OrderBy(static plot => plot.Id))
		{
			var dict = plot.ToDict();
			dict.Add("occupants", plot.Occupants.GetAll().Select(static occupant => occupant.Player.Id).ToArray());
			dict.Add("maxOccupantCount", plot.Occupants.MaxCount);
			dict.Add("maxTotalInstanceCount", plot.Instances.MaxCount);

			Log.Debug("{$Plot}", dict);
		}
	}

#if DEBUG
	private const int TestPlotId = 2;

	// Grid units left clear between test assets
	private const float TestAssetGap = 2f;

	// In rows from one corner, so any number of them fit
	private static void AddTestData()
	{
		var plot = GPlotManager.GetHandle(TestPlotId);
		var instances = GPlots.GetPlot(TestPlotId)!.Instances;
		var halfSize = plot.GridBoundarySize / 2;

		var x = -halfSize.X;
		var z = -halfSize.Z;
		var rowDepth = 0f;

		foreach (var asset in GAssets.GetAll())
		{
			var bounds = GAssetManager.GetBoundary(asset.Id);
			var position = bounds.Position / GridToWorldScale;
			var size = bounds.Size / GridToWorldScale;

			if (x + size.X > halfSize.X)
			{
				x = -halfSize.X;
				z += rowDepth + TestAssetGap;
				rowDepth = 0;
			}

			var height = PlotPlacement.GetRestingHeight(plot, asset.Id, Quaternion.Identity);
			instances.Add(asset.Id, new Vector3(x - position.X, height, z - position.Z), Quaternion.Identity);

			x += size.X + TestAssetGap;
			rowDepth = MathF.Max(rowDepth, size.Z);
		}

		GPlayers.Add(string.Empty, "Foo - Larpje139 (Test)");
		GPlayers.Add(string.Empty, "Bar - Diet Dr. Thunder Enjoyer (Test)");
		GPlayers.Add(string.Empty, "Baz - Dr. Jr. (Test)");
	}
#endif
}
