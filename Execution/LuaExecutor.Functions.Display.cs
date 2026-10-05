using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using EnsembleRoot.Common.Messages;
using Godot;
using Lua;
using Serilog;

// ReSharper disable InconsistentNaming

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private static ValueTask<int> cap_fps(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		Engine.MaxFps = context.GetArgumentOrDefault<int>(0);

		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_vsync_modes(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var modes = Enum.GetValues<DisplayServer.VSyncMode>();

		Log.Information(
			"Available VSync modes:\n{Modes}",
			string.Join(
				'\n',
				modes.Select(static mode => string.Create(CultureInfo.InvariantCulture, $"{(int)mode}. {mode}"))));

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_ui_dark_theme_on(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		bool? useDarkTheme = context.HasArgument(0) ? context.GetArgument<bool>(0) : null;

		Log.Information("Setting UI dark theme to: {UseDarkTheme}", useDarkTheme);
		WeakReferenceMessenger.Default.Send(new SetUiThemeMessage(useDarkTheme));

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_ui_scale(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var scale = context.GetArgument<double>(0);

		Log.Information("Setting UI render scale to: {Scale}", scale);
		WeakReferenceMessenger.Default.Send(new SetUiRenderScaleMessage(scale));

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_vsync_mode(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var argument = context.GetArgument<LuaValue>(0);

		DisplayServer.VSyncMode? mode = null;

		// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
		switch (argument.Type)
		{
			case LuaValueType.String:
				if (Enum.TryParse<DisplayServer.VSyncMode>(argument.Read<string>(), true, out var parsed))
					mode = parsed;
				break;

			case LuaValueType.Number:
				var value = (long)argument.Read<double>();

				if (Enum.IsDefined((DisplayServer.VSyncMode)value))
					mode = (DisplayServer.VSyncMode)value;
				break;
		}

		if (mode is { } result)
		{
			DisplayServer.WindowSetVsyncMode(result);
			Log.Information("Set VSync mode to: {Mode}", result);
		}
		else
			Log.Error("Invalid VSync mode: {Mode}", argument);

		return context.ReturnNothing();
	}
}
