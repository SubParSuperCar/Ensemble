using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.GdCore.Utils;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using Location = (int AssetId, Godot.Vector3 Position, Godot.Quaternion Rotation);

namespace EnsembleRoot.Tooling.Tools;

/// <summary>
///     The tools' instance edits, submitted and recorded in the history. Undoing a removal recreates instances under
///     new IDs, so the history finds them by asset and transform instead, which placement keeps unique.
/// </summary>
internal static class InstanceEdits
{
	extension(UndoRedo history)
	{
		public void Add(int assetId, Vector3 position, Quaternion rotation)
		{
			InstanceSpec[] specs = [new(assetId, position, rotation, new Dictionary())];
			history.Do(() => SubmitAdd(specs), () => SubmitRemove(specs));
		}

		public void Remove(IEnumerable<GdInstance> instances)
		{
			InstanceSpec[] specs = [.. instances.Select(Capture)];
			history.Do(() => SubmitRemove(specs), () => SubmitAdd(specs));
		}

		public void SetProperties(
			IEnumerable<(GdInstance Instance, IReadOnlyDictionary<string, CoreVariant> Properties)> edits)
		{
			List<InstanceSpec> before = [];
			List<InstanceSpec> after = [];

			foreach (var (instance, properties) in edits)
			{
				var current = instance.Properties.Source.All;
				var previous = new Dictionary();

				foreach (var key in properties.Keys)
					previous[key] = current[key].ToGodot();

				before.Add(At(instance, previous));
				after.Add(At(instance, Converter.ToGodotProperties(properties)));
			}

			history.Do(() => SubmitSet(after), () => SubmitSet(before));
		}
	}

	private static void SubmitAdd(IReadOnlyList<InstanceSpec> specs)
	{
		foreach (var chunk in specs.Chunk(AddInstancesBulkAction.MaxCount))
			new AddInstancesBulkAction(chunk).Submit();
	}

	private static void SubmitRemove(IReadOnlyList<InstanceSpec> specs)
	{
		InstanceReference[] references = [.. Find(specs).Select(static match => match.Reference)];

		foreach (var chunk in references.Chunk(RemoveInstancesAction.MaxCount))
			new RemoveInstancesAction(chunk).Submit();
	}

	private static void SubmitSet(IReadOnlyList<InstanceSpec> specs)
	{
		PropertyEdit[] edits =
			[.. Find(specs).Select(static match => new PropertyEdit(match.Reference, match.Spec.Properties))];

		foreach (var chunk in edits.Chunk(SetPropertiesAction.MaxCount))
			new SetPropertiesAction(chunk).Submit();
	}

	private static List<(InstanceReference Reference, InstanceSpec Spec)> Find(IEnumerable<InstanceSpec> specs)
	{
		List<(InstanceReference, InstanceSpec)> matches = [];

		if (LocalPlot is not { } plot)
			return matches;

		var specsByLocation = specs.DistinctBy(GetLocation).ToDictionary(GetLocation);

		foreach (var instance in plot.Instances.Source.All)
			if (specsByLocation.TryGetValue(GetLocation(instance), out var spec))
				matches.Add((InstanceReference.From(GdInstance.From(instance)), spec));

		return matches;
	}

	// Specs carry only the properties that differ from the asset's defaults
	private static InstanceSpec Capture(GdInstance instance)
	{
		var defaults = instance.Asset.Source.Properties;
		var properties = new Dictionary();

		foreach (var (key, value) in instance.Properties.Source.All)
			if (!defaults.TryGetValue(key, out var initial) || value != initial)
				properties[key] = value.ToGodot();

		return At(instance, properties);
	}

	private static InstanceSpec At(GdInstance instance, Dictionary properties) =>
		new(instance.Asset.Id, instance.Position, instance.Rotation, properties);

	private static Location GetLocation(InstanceSpec spec) => (spec.AssetId, spec.Position, spec.Rotation);

	private static Location GetLocation(IInstance instance) =>
		(instance.Asset.Id, instance.Position.ToGodot(), instance.Rotation.ToGodot());
}
