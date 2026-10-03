using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EnsembleRoot.SessionManager.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.SessionManager.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

[StructLayout(LayoutKind.Auto)]
public readonly record struct AddInstanceAction(int AssetId, Vector3 Position, Quaternion Rotation)
	: INetworkAction<AddInstanceAction>
{
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

		var instances = plot.Instances;

		return IsMet(instances.Count, instances.MaxCount) ||
			   (instances.GetQuota(AssetId) is [var count, var maxCount] && IsMet(count, maxCount))
			? Reject("Quota met.")
			: Accept;
	}

	public void Apply(ActionSource source) => source.Plot!.Instances.Add(AssetId, Position, Rotation);

	private static bool IsMet(int count, int maxCount) => maxCount is not Unlimited && count >= maxCount;

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<AddInstanceAction>();
}
