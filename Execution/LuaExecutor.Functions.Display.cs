using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using EnsembleRoot.Common.Messages;
using EnsembleRoot.Scripts.Cameras;
using Godot;
using Lua;
using Serilog;

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	// Camera3D.Fov's own limits
	private const double MinFov = 1d;
	private const double MaxFov = 179d;

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

	private static ValueTask<int> dmp_window_modes(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		Log.Information("Available window modes:\n{Modes}", FormatEnum(Enum.GetValues<DisplayServer.WindowMode>()));

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_cam_fov(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var degrees = context.GetArgument<double>(0);

		if (((SceneTree)Engine.GetMainLoop()).Root.GetCamera3D() is not { } camera)
		{
			Log.Error("{Function} needs a camera", nameof(set_cam_fov));
			return context.ReturnNothing();
		}

		if (degrees is not (>= MinFov and <= MaxFov))
		{
			Log.Error("Invalid camera FOV: {Degrees} degree(s) (takes {Min} to {Max})", degrees, MinFov, MaxFov);
			return context.ReturnNothing();
		}

		camera.Fov = (float)degrees;
		Log.Information("Set camera FOV to {Degrees} degree(s)", degrees);

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_mouse_sens(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var sensitivity = context.GetArgumentOrDefault(0, 1f);

		if (sensitivity is not (>= PopperCam.MinMouseSensitivity and <= PopperCam.MaxMouseSensitivity))
		{
			Log.Error(
				"Invalid mouse sensitivity: {Sensitivity} (takes {Min} to {Max})", sensitivity,
				PopperCam.MinMouseSensitivity, PopperCam.MaxMouseSensitivity);

			return context.ReturnNothing();
		}

		PopperCam.MouseSensitivity = sensitivity;
		Log.Information("Set mouse sensitivity to {Sensitivity}x", sensitivity);

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_ui_anims_on(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		AreAnimationsOn = context.GetArgument<bool>(0);
		Log.Information("UI animations will be {State} from the next launch", AreAnimationsOn ? "on" : "off");

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_stats_on(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		WeakReferenceMessenger.Default.Send(new SetStatsShownMessage(context.GetArgument<bool>(0)));

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
			Log.Error("Invalid VSync mode: {Mode} (see {Function}())", argument, nameof(dmp_vsync_modes));

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_window_mode(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var argument = context.GetArgument<LuaValue>(0);

		if (ToEnum(argument, Enum.GetValues<DisplayServer.WindowMode>()) is { } result)
		{
			DisplayServer.WindowSetMode(result);
			Log.Information("Set window mode to {Mode}", result);
		}
		else
			Log.Error("Invalid window mode: {Mode} (see {Function}())", argument, nameof(dmp_window_modes));

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
