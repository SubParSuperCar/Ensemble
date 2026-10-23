using System.Runtime.CompilerServices;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Sessions.Snapshots;
using Godot;
using Godot.Collections;
using Serilog;

namespace EnsembleRoot.Replication.Snapshots;

internal static class PlotsSnapshot
{
	private static readonly (string Key, Variant.Type Type)[] InstanceFields =
	[
		("id", Variant.Type.Int), ("assetId", Variant.Type.Int), ("position", Variant.Type.Vector3),
		("rotation", Variant.Type.Quaternion)
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

	// Each field restores on its own, so a malformed one only costs itself
	private static void Restore(Array<Variant> payload)
	{
		foreach (var dict in payload.Select(static entry => entry.AsGodotDictionary()))
		{
			if (
				!dict.TryGetValue("id", out var id) || id.VariantType is not Variant.Type.Int ||
				GPlots.GetPlot(id.AsInt32()) is not { } plot)
			{
				Log.Warning("Skipped malformed or unknown plot in snapshot");
				continue;
			}

			if (TryGet(plot, dict, "occupantIds", Variant.Type.PackedStringArray, out var occupantIds))
				foreach (var playerId in occupantIds.AsStringArray())
					Try(plot, "occupant", () => GPlots.SetPlot(playerId, plot.Id, false, false));

			if (TryGet(plot, dict, "ownerId", Variant.Type.String, out var ownerId))
				Try(plot, "owner", () => plot.Occupants.SetOwner(ownerId.AsString()));

			if (TryGet(plot, dict, "instances", Variant.Type.Array, out var instances))
				foreach (var instance in instances.AsGodotArray())
					Try(plot, "instance", () => RestoreInstance(plot, instance.AsGodotDictionary()));

			if (TryGet(plot, dict, "isSpawned", Variant.Type.Bool, out var isSpawned) && isSpawned.AsBool())
				Try(plot, "spawn state", plot.Spawn);

			if (
				TryGet(plot, dict, "epoch", Variant.Type.Int, out var epoch) &&
				TryGet(plot, dict, "simulator", Variant.Type.Int, out var simulator) &&
				TryGet(plot, dict, "creation", Variant.Type.PackedByteArray, out var creation))
				Try(
					plot, "creation",
					() => GPlotManager.GetHandleOrNull(plot.Id)
						?.RestoreCreation((ushort)epoch.AsInt32(), simulator.AsInt32(), creation.AsByteArray()));
		}
	}

	private static void RestoreInstance(GdPlot plot, Dictionary dict)
	{
		if (!HasFields(dict, InstanceFields) || IsLimitReached(plot.Instances.Count, plot.Instances.MaxCount))
		{
			Log.Warning("Skipped malformed or excess instance in snapshot of plot with ID {PlotId}", plot.Id);
			return;
		}

		var properties = plot.Instances
			.AddAt(
				dict["assetId"].AsInt32(), dict["position"].AsVector3(), dict["rotation"].AsQuaternion(),
				dict["id"].AsInt32())
			.Properties;

		if (!dict.TryGetValue("properties", out var values) || values.VariantType is not Variant.Type.Dictionary)
			return;

		foreach (var (key, value) in values.AsGodotDictionary())
			if (key.VariantType is not Variant.Type.String || !properties.TryUpdate(key.AsString(), value))
				Log.Warning(
					"Skipped malformed property {Key} in snapshot of plot with ID {PlotId}", key.ToString(), plot.Id);
	}

	private static bool TryGet(GdPlot plot, Dictionary dict, string key, Variant.Type type, out Variant value)
	{
		if (dict.TryGetValue(key, out value) && value.VariantType == type)
			return true;

		Log.Warning("Skipped malformed {Field} in snapshot of plot with ID {PlotId}", key, plot.Id);
		return false;
	}

	private static void Try(GdPlot plot, string part, Action restore)
	{
		try
		{
			restore();
		}
		catch (Exception exception)
		{
			Log.Warning(exception, "Failed to restore {Part} in snapshot of plot with ID {PlotId}", part, plot.Id);
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
