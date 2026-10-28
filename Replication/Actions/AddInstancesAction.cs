using System.Runtime.CompilerServices;
using EnsembleRoot.GdCore.Assets;
using EnsembleRoot.Scripts.Plots;
using EnsembleRoot.Scripts.Plots.Placement;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

/// <summary>
///     Adds instances, with their properties, in one request (e.g., to place, paste, or load a build). They're placed
///     in order under the placement rules, so one that doesn't fit (anymore) is skipped and the rest still go.
/// </summary>
public readonly record struct AddInstancesAction(IReadOnlyList<InstanceSpec> Instances)
	: INetworkAction<AddInstancesAction>
{
	// Callers split larger builds into batches of this size
	public const int MaxCount = 256;

	// Every peer builds each instance's scene, so a full batch costs about a third of a peer's token bucket
	private const int CountPerToken = 8;

	public static int GetTokenCost(Array<Variant> payload, ActionSource source) =>
		(payload.Count + CountPerToken - 1) / CountPerToken;

	public static AddInstancesAction FromPayload(Array<Variant> payload) =>
		new([.. payload.Select(static spec => InstanceSpec.FromPayload(spec.AsGodotArray<Variant>()))]);

	public Array<Variant> ToPayload() => [.. Instances.Select(static spec => (Variant)spec.ToPayload())];

	public ActionValidation Validate(ActionSource source)
	{
		if (source.Plot is not { IsSpawned: false } plot)
			return Reject("Plot not editable.");

		if (Instances.Count is 0 or > MaxCount)
			return Reject("Instance count invalid.");

		if (GPlotManager.GetHandleOrNull(plot.Id) is not { } handle)
			return Reject("Plot not found.");

		foreach (var spec in Instances)
		{
			if (GAssets.GetAsset(spec.AssetId) is not { } asset)
				return Reject("Asset not found.");

			if (!spec.Position.IsFinite() || !spec.Rotation.IsNormalized())
				return Reject("Transform invalid.");

			if (GdProperties.Check(asset.Source.Properties, spec.Properties) is { } reason)
				return Reject(reason);
		}

		// Others may build first; whatever still fits goes
		return Instances.Any(spec => IsPlaceable(handle, spec)) ? Accept : Reject("No placement valid.");
	}

	public void Apply(ActionSource source)
	{
		var plot = source.Plot!;
		var handle = GPlotManager.GetHandle(plot.Id);

		// Each one is placed against those before it, so overlaps and quotas within the batch hold too
		foreach (var spec in Instances)
		{
			if (!IsPlaceable(handle, spec))
				continue;

			var properties = plot.Instances.Add(spec.AssetId, spec.Position, spec.Rotation).Properties;

			foreach (var (key, value) in spec.Properties)
				properties.TryUpdate(key.AsString(), value);
		}
	}

	private static bool IsPlaceable(PlotHandle handle, InstanceSpec spec) =>
		PlotPlacement.Evaluate(handle, spec.AssetId, spec.Position, spec.Rotation) is PlacementState.Valid;

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<AddInstancesAction>();
}
