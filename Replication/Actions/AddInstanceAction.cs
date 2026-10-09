using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EnsembleRoot.Scripts.Plots.Impl;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

[StructLayout(LayoutKind.Auto)]
public readonly record struct AddInstanceAction(int AssetId, Vector3 Position, Quaternion Rotation)
	: INetworkAction<AddInstanceAction>
{
	public static int TokenCost { get; } = 2;

	public static AddInstanceAction FromPayload(Array<Variant> payload) =>
		new(payload[0].AsInt32(), payload[1].AsVector3(), payload[2].AsQuaternion());

	public Array<Variant> ToPayload() => [AssetId, Position, Rotation];

	public ActionValidation Validate(ActionSource source)
	{
		if (source.Plot is not { IsSpawned: false } plot)
			return Reject("Plot not editable.");

		if (GAssets.GetAsset(AssetId) is null)
			return Reject("Asset not found.");

		if (!Position.IsFinite() || !Rotation.IsNormalized())
			return Reject("Transform invalid.");

		if (GPlotManager.GetHandleOrNull(plot.Id) is not { } handle)
			return Reject("Plot not found.");

		return PlotPlacement.Evaluate(handle, AssetId, Position, Rotation) is var state and not PlacementState.Valid
			? Reject($"Placement invalid: {state}.")
			: Accept;
	}

	public void Apply(ActionSource source) => source.Plot!.Instances.Add(AssetId, Position, Rotation);

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<AddInstanceAction>();
}
