using System.Runtime.CompilerServices;
using EnsembleRoot.Sessions.Snapshots;
using Godot;
using Godot.Collections;
using Serilog;

namespace EnsembleRoot.Replication.Snapshots;

internal static class PlotsSnapshot
{
	private static readonly (string Key, Variant.Type Type)[] PlotFields =
	[
		("id", Variant.Type.Int), ("ownerId", Variant.Type.String), ("occupantIds", Variant.Type.PackedStringArray),
		("instances", Variant.Type.Array), ("isSpawned", Variant.Type.Bool), ("epoch", Variant.Type.Int),
		("simulator", Variant.Type.Int), ("creation", Variant.Type.PackedByteArray)
	];

	private static readonly (string Key, Variant.Type Type)[] InstanceFields =
	[
		("id", Variant.Type.Int), ("assetId", Variant.Type.Int), ("position", Variant.Type.Vector3),
		("rotation", Variant.Type.Quaternion), ("properties", Variant.Type.Dictionary)
	];

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

			// Every plot's epoch, spawned or not, or this peer's would drift from the others' at the next spawn
			var (epoch, simulator, creation) = GPlotManager.GetHandle(plot.Id).CaptureCreation();
			dict["epoch"] = epoch;
			dict["simulator"] = simulator;
			dict["creation"] = creation;

			payload.Add(dict);
		}

		return payload;
	}

	private static void Restore(Array<Variant> payload)
	{
		foreach (var dict in payload.Select(static entry => entry.AsGodotDictionary()))
		{
			if (!HasFields(dict, PlotFields) || GPlots.GetPlot(dict["id"].AsInt32()) is not { } plot)
			{
				Log.Warning("Skipped malformed or unknown plot in snapshot");
				continue;
			}

			foreach (var playerId in dict["occupantIds"].AsStringArray())
				GPlots.SetPlot(playerId, plot.Id, false, false);

			plot.Occupants.SetOwner(dict["ownerId"].AsString());

			foreach (var instance in dict["instances"].AsGodotArray().Select(static entry => entry.AsGodotDictionary()))
			{
				if (
					!HasFields(instance, InstanceFields) ||
					IsLimitReached(plot.Instances.Count, plot.Instances.MaxCount))
				{
					Log.Warning("Skipped malformed or excess instance in snapshot of plot with id {PlotId}", plot.Id);
					continue;
				}

				plot.Instances
					.AddAt(
						instance["assetId"].AsInt32(),
						instance["position"].AsVector3(),
						instance["rotation"].AsQuaternion(),
						instance["id"].AsInt32())
					.Properties.UpdateAll(instance["properties"].AsGodotDictionary());
			}

			if (dict["isSpawned"].AsBool())
				plot.Spawn();

			GPlotManager.GetHandleOrNull(plot.Id)
				?.RestoreCreation(
					(ushort)dict["epoch"].AsInt32(), dict["simulator"].AsInt32(), dict["creation"].AsByteArray());
		}
	}

	private static bool HasFields(Dictionary dict, ReadOnlySpan<(string Key, Variant.Type Type)> fields)
	{
		foreach (var (key, type) in fields)
			if (!dict.TryGetValue(key, out var value) || value.VariantType != type)
				return false;

		return true;
	}

	[ModuleInitializer]
	internal static void Register() => NetworkSnapshotRegistry.Register(nameof(PlotsSnapshot), Capture, Restore);
}
