using EnsembleRoot.Scripts.Assets.Materials;
using EnsembleRoot.Tooling;
using EnsembleRoot.Tooling.Tools;
using Godot;
using Lua;
using Serilog;

// ReSharper disable InconsistentNaming

namespace EnsembleRoot.Execution;

// The tools as the tool bar drives them, so their state, highlights, and history stay in step with the UI
public static partial class LuaExecutor
{
	private const string ConstructToolName = "construct";
	private const string DestructToolName = "destruct";
	private const string PropertyToolName = "property";
	private const string PaintToolName = "paint";

	private const string ColorKey = "color";
	private const string MaterialKey = "material";
	private const string ApplyColorKey = "apply_color";
	private const string ApplyMaterialKey = "apply_material";

	private const string EditablePlotNeeded = "Tools need a claimed plot that isn't spawned";

	private static readonly string[] PaintKeys = [ColorKey, MaterialKey, ApplyColorKey, ApplyMaterialKey];

	private static ValueTask<int> paint_all(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GetToolManager(nameof(paint_all)) is { } tools && !tools.Paint.PaintAll())
			Log.Error(EditablePlotNeeded);

		return context.ReturnNothing();
	}

	private static ValueTask<int> redo(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GetToolManager(nameof(redo)) is not { UndoRedo: var history })
			return context.ReturnNothing();

		if (history.CanRedo)
			history.Redo();
		else
			Log.Error("Nothing to redo");

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_construct_asset(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var assetId = context.GetArgument<int>(0);

		if (GetToolManager(nameof(set_construct_asset)) is not { } tools)
			return context.ReturnNothing();

		if (GAssets.GetAsset(assetId) is null)
			Log.Error("Asset with ID {AssetId} not found (see {Function}())", assetId, nameof(dmp_assets));
		else
			tools.Construct.SetAsset(assetId);

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_construct_rot(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GetToolManager(nameof(set_construct_rot)) is { } tools)
			tools.Construct.SetRotation(context.GetRotationArguments(0));

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_paint(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var table = context.GetArgument<LuaTable>(0);

		if (
			!TryReadPaintOptions(table, out var options) ||
			GetToolManager(nameof(set_paint)) is not { Paint: var paint })
			return context.ReturnNothing();

		paint.Color = options.Color ?? paint.Color;
		paint.MaterialId = options.MaterialId ?? paint.MaterialId;
		paint.IsColorApplied = options.IsColorApplied ?? paint.IsColorApplied;
		paint.IsMaterialApplied = options.IsMaterialApplied ?? paint.IsMaterialApplied;

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_tool_on(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var name = context.GetArgument<string>(0);
		var isOn = context.GetArgument<bool>(1);

		if (GetToolManager(nameof(set_tool_on)) is not { } tools)
			return context.ReturnNothing();

		bool? isEnabled = name switch
		{
			ConstructToolName => SetToolOn<ConstructTool>(tools, isOn),
			DestructToolName => SetToolOn<DestructTool>(tools, isOn),
			PropertyToolName => SetToolOn<PropertyTool>(tools, isOn),
			PaintToolName => SetToolOn<PaintTool>(tools, isOn),
			_ => null
		};

		if (isEnabled is null)
			Log.Error(
				"Tool {Name} not found (use {Construct}, {Destruct}, {Property}, or {Paint})",
				name, ConstructToolName, DestructToolName, PropertyToolName, PaintToolName);
		else if (isEnabled != isOn)
			Log.Error(EditablePlotNeeded);

		return context.ReturnNothing();
	}

	private static ValueTask<int> undo(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (GetToolManager(nameof(undo)) is not { UndoRedo: var history })
			return context.ReturnNothing();

		if (history.CanUndo)
			history.Undo();
		else
			Log.Error("Nothing to undo");

		return context.ReturnNothing();
	}

	// Toggling creates a tool only if it can be enabled
	private static bool SetToolOn<TTool>(ToolManager tools, bool isOn) where TTool : ToolBase, new()
	{
		if (tools.IsEnabled<TTool>() != isOn)
			tools.Toggle<TTool>();

		return tools.IsEnabled<TTool>();
	}

	private static bool TryReadPaintOptions(LuaTable table, out PaintOptions options)
	{
		options = default;

		foreach (var (key, value) in table)
		{
			var name = key.Type is LuaValueType.String ? key.Read<string>() : null;
			var text = value.Type is LuaValueType.String ? value.Read<string>() : null;
			var number = ToInt(value);
			bool? flag = value.Type is LuaValueType.Boolean ? value.Read<bool>() : null;

			PaintOptions? updated = name switch
			{
				ColorKey when text is not null && Color.HtmlIsValid(text) =>
					options with { Color = Color.FromHtml(text) },
				MaterialKey when number is None || (number is { } id && MaterialBank.Get(id) is not null) =>
					options with { MaterialId = number },
				ApplyColorKey when flag is not null => options with { IsColorApplied = flag },
				ApplyMaterialKey when flag is not null => options with { IsMaterialApplied = flag },
				_ => null
			};

			if (updated is not { } valid)
			{
				LogInvalidOption(key, value, PaintKeys);
				return false;
			}

			options = valid;
		}

		return true;
	}

	private static ToolManager? GetToolManager(string function)
	{
		if (ToolManager.Instance is { } tools)
			return tools;

		Log.Error("{Function} needs the tools, which headless servers don't have", function);
		return null;
	}

	private readonly record struct PaintOptions(
		Color? Color,
		int? MaterialId,
		bool? IsColorApplied,
		bool? IsMaterialApplied);
}
