using System.Runtime.CompilerServices;
using EnsembleRoot.SessionManager.Snapshots;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.Replication.Snapshots;

internal static class PlotsSnapshot
{
	private static Array<Variant> Capture()
	{
		var payload = new Array<Variant>();

		foreach (var plot in GPlots.GetAll())
		{
			var occupants = plot.Occupants;
			var dict = plot.ToDict();

			dict["ownerId"] = occupants.Owner?.Player.Id ?? string.Empty;
			dict["occupantIds"] = occupants.GetAll().Select(static occupant => occupant.Player.Id).ToArray();
			dict["instances"] = Variant.From(plot.Instances.GetAllDicts());

			payload.Add(dict);
		}

		return payload;
	}

	private static void Restore(Array<Variant> payload)
	{
		foreach (var dict in payload.Select(static entry => entry.AsGodotDictionary()))
		{
			if (GPlots.GetPlot(dict["id"].AsInt32()) is not { } plot)
				continue;

			foreach (var playerId in dict["occupantIds"].AsStringArray())
				GPlots.SetPlot(
					playerId,
					plot.Id,
					shouldResolveOwnerIfNullOrRelinquishing: false,
					shouldDespawnAndClearInstancesIfLastToLeave: false);

			plot.Occupants.SetOwner(dict["ownerId"].AsString());

			foreach (var instance in dict["instances"].AsGodotArray<Dictionary>())
				plot.Instances
					.AddAt(
						instance["assetId"].AsInt32(),
						instance["position"].AsVector3(),
						instance["rotation"].AsQuaternion(),
						instance["id"].AsInt32())
					.Properties.UpdateAll(instance["properties"].AsGodotDictionary());

			if (dict["isSpawned"].AsBool())
				plot.Spawn();
		}
	}

	[ModuleInitializer]
	internal static void Register() => NetworkSnapshotRegistry.Register(nameof(PlotsSnapshot), Capture, Restore);
}
