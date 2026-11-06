using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using EnsembleRoot.Common.Messages;
using Godot;
using Lua;
using Serilog;

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private static readonly Viewport.DebugDrawEnum[] DrawModes =
	[
		Viewport.DebugDrawEnum.Disabled, Viewport.DebugDrawEnum.Unshaded, Viewport.DebugDrawEnum.Lighting,
		Viewport.DebugDrawEnum.Overdraw, Viewport.DebugDrawEnum.Wireframe
	];

	private static ValueTask<int> cap_fps(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		Engine.MaxFps = context.GetArgumentOrDefault<int>(0);
		Log.Information("Set max FPS to {MaxFps}", Engine.MaxFps);

		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_draw_modes(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		// Godot's other modes need Forward+ or only show their own effects, so they'd draw nothing here
		Log.Information("Available draw modes:\n{Modes}", FormatEnum(DrawModes));
		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_vsync_modes(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		Log.Information("Available VSync modes:\n{Modes}", FormatEnum(Enum.GetValues<DisplayServer.VSyncMode>()));
		return context.ReturnNothing();
	}

	private static ValueTask<int> set_draw_mode(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var argument = context.GetArgumentOrDefault<LuaValue>(0);
		var mode = argument.Type is LuaValueType.Nil ? Viewport.DebugDrawEnum.Disabled : ToEnum(argument, DrawModes);

		if (mode is { } result)
		{
			((SceneTree)Engine.GetMainLoop()).Root.DebugDraw = result;
			Log.Information("Set draw mode to {Mode}", result);
		}
		else
			Log.Error("Invalid draw mode: {Mode} (see {Function}())", argument, nameof(dmp_draw_modes));

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_ui_dark_theme_on(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var useDarkTheme = context.GetArgumentOrNull<bool>(0);

		WeakReferenceMessenger.Default.Send(new SetUiThemeMessage(useDarkTheme));
		Log.Information("Set UI dark theme to {UseDarkTheme}", useDarkTheme);

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_ui_scale(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var scale = context.GetArgument<double>(0);

		if (!double.IsFinite(scale) || scale <= 0)
		{
			Log.Error("Invalid UI render scale: {Scale}", scale);
			return context.ReturnNothing();
		}

		WeakReferenceMessenger.Default.Send(new SetUiRenderScaleMessage(scale));
		Log.Information("Set UI render scale to {Scale}", scale);

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_vsync_mode(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var argument = context.GetArgument<LuaValue>(0);

		if (ToEnum(argument, Enum.GetValues<DisplayServer.VSyncMode>()) is { } result)
		{
			DisplayServer.WindowSetVsyncMode(result);
			Log.Information("Set VSync mode to {Mode}", result);
		}
		else
			Log.Error("Invalid VSync mode: {Mode}", argument);

		return context.ReturnNothing();
	}

	// By name, ignoring case, or by number
	private static TEnum? ToEnum<TEnum>(LuaValue value, TEnum[] values) where TEnum : struct, Enum
	{
		var name = value.Type is LuaValueType.String ? value.Read<string>() : null;
		var number = ToInt(value);

		foreach (var candidate in values)
			if (
				string.Equals(candidate.ToString(), name, StringComparison.OrdinalIgnoreCase) ||
				Convert.ToInt64(candidate, CultureInfo.InvariantCulture) == number)
				return candidate;

		return null;
	}

	private static string FormatEnum<TEnum>(TEnum[] values) where TEnum : struct, Enum =>
		string.Join(
			'\n',
			values.Select(static value => string.Create(
				CultureInfo.InvariantCulture, $"{Convert.ToInt64(value, CultureInfo.InvariantCulture)}. {value}")));
}
