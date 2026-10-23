using EnsembleCoreRoot.Api.Assets;
using EnsembleRoot.GdCore.Utils;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Scripts.Plots;
using EnsembleRoot.Scripts.Plots.Placement;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using Lua;
using Serilog;

namespace EnsembleRoot.Execution;

// Scripted building on the local plot, through the same actions (and validation) as the tools
public static partial class LuaExecutor
{
	private static ValueTask<int> add_inst(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GetLocalPlot(nameof(add_inst)) is not var (_, handle))
			return context.ReturnNothing();

		var assetId = context.GetArgument<int>(0);

		if (GAssets.GetAsset(assetId) is null)
		{
			Log.Error("Asset with ID {AssetId} not found", assetId);
			return context.ReturnNothing();
		}

		var rotation = Quaternion.FromEuler(new Vector3(
			float.DegreesToRadians(context.GetArgumentOrDefault(4, 0f)),
			float.DegreesToRadians(context.GetArgumentOrDefault(5, 0f)),
			float.DegreesToRadians(context.GetArgumentOrDefault(6, 0f)))).Normalized();

		var position = new Vector3(
			context.GetArgumentOrDefault(1, 0f),
			context.GetArgumentOrDefault(2, GetRestingHeight(handle, assetId, rotation)),
			context.GetArgumentOrDefault(3, 0f));

		if (PlotPlacement.Evaluate(handle, assetId, position, rotation) is var state and not PlacementState.Valid)
		{
			Log.Error("Cannot place asset {AssetId} at {Position}: {State}", assetId, position, state);
			return context.ReturnNothing();
		}

		new AddInstanceAction(assetId, position, rotation).Submit();
		return context.ReturnNothing();
	}

	private static ValueTask<int> clr_insts(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GetLocalPlot(nameof(clr_insts)) is not null)
			new ClearInstancesAction().Submit();

		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_insts(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GetLocalPlot(nameof(dmp_insts)) is not var (plot, _))
			return context.ReturnNothing();

		var instances = plot.Instances.Source.All.OrderBy(static instance => instance.Id);

		Log.Information(
			"Plot {PlotId}: {Count} instance(s)\n{Instances}",
			plot.Id, plot.Instances.Count, string.Join('\n', instances));

		return context.ReturnNothing();
	}

	private static ValueTask<int> rm_insts(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GetLocalPlot(nameof(rm_insts)) is not var (plot, _))
			return context.ReturnNothing();

		List<InstanceReference> references = [];

		for (var i = 0; i < context.ArgumentCount; i++)
		{
			var id = context.GetArgument<int>(i);

			if (plot.Instances.GetInstance(id) is { } instance)
				references.Add(InstanceReference.From(instance));
			else
				Log.Warning("Instance with ID {InstanceId} not found", id);
		}

		foreach (var chunk in references.Chunk(RemoveInstancesAction.MaxCount))
			new RemoveInstancesAction(chunk).Submit();

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_inst_prop(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GetLocalPlot(nameof(set_inst_prop)) is not var (plot, _))
			return context.ReturnNothing();

		var id = context.GetArgument<int>(0);
		var key = context.GetArgument<string>(1);

		if (plot.Instances.GetInstance(id) is not { } instance)
		{
			Log.Error("Instance with ID {InstanceId} not found", id);
			return context.ReturnNothing();
		}

		if (!instance.Properties.Source.All.TryGetValue(key, out var current))
		{
			Log.Error("Instance {InstanceId} has no property {Key}", id, key);
			return context.ReturnNothing();
		}

		// Nil resets it to the asset's default
		var value = context.IsArgumentOmitted(2)
			? instance.Asset.Source.Properties[key].ToGodot()
			: ToVariant(context.GetArgument<LuaValue>(2), current.Type);

		if (value.VariantType is Variant.Type.Nil)
		{
			Log.Error("Property {Key} must stay {Type}", key, current.Type);
			return context.ReturnNothing();
		}

		new SetPropertiesAction(InstanceReference.From(instance), new Dictionary { [key] = value })
			.Submit();

		return context.ReturnNothing();
	}

	// The grid height at which an asset rests on the plot's floor
	private static float GetRestingHeight(PlotHandle plot, int assetId, Quaternion rotation)
	{
		var box = PlotPlacement.GetBox(assetId, Vector3.Zero, rotation);
		var floor = PlotPlacement.GetBounds(plot).Position.Y;

		return (floor - (box.Center.Y - box.Envelope.Y)) / PlotHandle.GridToWorldScale;
	}

	// Lua numbers are doubles, so integer properties only take whole numbers; Nil means the types don't match
	private static Variant ToVariant(LuaValue value, CoreVariantType type) =>
		(type, value.Type) switch
		{
			(CoreVariantType.Boolean, LuaValueType.Boolean) => Variant.CreateFrom(value.Read<bool>()),
			(CoreVariantType.Int64, LuaValueType.Number) when double.IsInteger(value.Read<double>()) =>
				Variant.CreateFrom((long)value.Read<double>()),
			(CoreVariantType.Double, LuaValueType.Number) => Variant.CreateFrom(value.Read<double>()),
			(CoreVariantType.String, LuaValueType.String) => Variant.CreateFrom(value.Read<string>()),
			_ => default
		};
}
