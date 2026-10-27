using EnsembleRoot.Tooling;
using EnsembleRoot.Tooling.Tools;
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
			_ => null
		};

		if (isEnabled is null)
			Log.Error(
				"Tool {Name} not found (use {Construct}, {Destruct}, or {Property})",
				name, ConstructToolName, DestructToolName, PropertyToolName);
		else if (isEnabled != isOn)
			Log.Error("Tools need a claimed plot that isn't spawned");

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

	// Headless servers have no tools
	private static ToolManager? GetToolManager(string function)
	{
		if (ToolManager.Instance is { } tools)
			return tools;

		Log.Error("{Function} needs the tools, which headless servers don't have", function);
		return null;
	}
}
