using System.Diagnostics;
using System.Globalization;
using Avalonia.Styling;
using BogaNet.TTS;
using CommunityToolkit.Mvvm.Messaging;
using Godot;
using Lua;
using Root.Common.Logging;
using Root.Common.Networking;
using Root.Scripts.World;
using Root.Ui.Impl.Messages;
using Serilog;
using Environment = System.Environment;

// ReSharper disable InconsistentNaming

namespace Root.Common.Execution;

public static partial class LuaExecutor
{
	private const string PublicIPv4AddressSourceUrl = "https://api.ipify.org";

	private static void InjectCustomFunctions(LuaTable env)
	{
		env[nameof(add_rand_insts)] = new LuaFunction(add_rand_insts);
		env[nameof(clr_insts)] = new LuaFunction(clr_insts);
		env[nameof(clr_log)] = new LuaFunction(clr_log);
		env[nameof(dmp_asm_info)] = new LuaFunction(dmp_asm_info);
		env[nameof(dmp_env)] = new LuaFunction(dmp_env);
		env[nameof(dmp_inp_map)] = new LuaFunction(dmp_inp_map);
		env[nameof(get_pub_ip4_addr)] = new LuaFunction(get_pub_ip4_addr);
		env[nameof(get_vsync_modes)] = new LuaFunction(get_vsync_modes);
		env[nameof(help)] = new LuaFunction(help);
		env[nameof(print)] = new LuaFunction(print);
		env[nameof(quit)] = new LuaFunction(quit);
		env[nameof(restart)] = new LuaFunction(restart);
		env[nameof(set_static_shader_on)] = new LuaFunction(set_static_shader_on);
		env[nameof(set_time)] = new LuaFunction(set_time);
		env[nameof(set_ui_dark_theme_on)] = new LuaFunction(set_ui_dark_theme_on);
		env[nameof(set_ui_scale)] = new LuaFunction(set_ui_scale);
		env[nameof(set_vsync_mode)] = new LuaFunction(set_vsync_mode);
		env[nameof(tts)] = new LuaFunction(tts);
		env[nameof(wait)] = new LuaFunction(wait);
	}

	private static ValueTask<int> add_rand_insts(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var plotId = context.GetArgument<int>(0);
		var count = context.GetArgument<int>(1);
		var positionRange = GPlotManager.GetHandle(plotId).GridBoundarySize / 2;

		var instances = GPlots.GetPlot(plotId)!.Instances;
		var assetIds = GAssets.GetAll().Select(asset => asset.Id).ToArray();
		var random = Random.Shared;

		var stopwatch = Stopwatch.StartNew();

		for (var i = 0; i < count; i++)
		{
			var assetId = assetIds[random.Next(assetIds.Length)];

			Vector3 position;
			Vector3 axis;

			do
			{
				position = new Vector3(
					random.Next(-(int)positionRange.X, (int)positionRange.X),
					random.Next(0, (int)positionRange.Y * 2) + 1,
					random.Next(-(int)positionRange.Z, (int)positionRange.Z));

				axis = position.Normalized();
			} while (!axis.IsNormalized());

			var rotation = new Quaternion(
				axis,
				(float)((random.NextDouble() - 0.5) * Math.Tau));

			instances.Add(assetId, position, rotation);
		}

		stopwatch.Stop();
		Log.Information("Added {Count} instance(s) to plot with id {PlotId} in {ElapsedMs:F3} ms.",
			count,
			plotId,
			stopwatch.Elapsed.TotalMilliseconds);

		context.Return();
		return default;
	}

	private static ValueTask<int> clr_insts(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var plotId = context.GetArgument<int>(0);
		var instances = GPlots.GetPlot(plotId)!.Instances;
		var count = instances.Count;

		var stopwatch = Stopwatch.StartNew();
		instances.Clear();

		stopwatch.Stop();
		Log.Information("Removed {Count} instance(s) from plot with id {PlotId} in {ElapsedMs:F3} ms.",
			count,
			plotId,
			stopwatch.Elapsed.TotalMilliseconds);

		context.Return();
		return default;
	}

	private static ValueTask<int> clr_log(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		VolatileLogHistorySink.Clear();

		context.Return();
		return default;
	}

	private static ValueTask<int> dmp_asm_info(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var assemblies = AppDomain.CurrentDomain.GetAssemblies()
			.Select(assembly => $"\n~~> {assembly.GetName()}").ToArray();

		Log.Information("Loaded assemblies ({Count}):\n{Assemblies}", assemblies.Length, assemblies);

		context.Return();
		return default;
	}

	private static ValueTask<int> dmp_env(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		Log.Information("Contents of _ENV:");
		DumpTable(context.State.Environment, "_ENV", []);

		context.Return();
		return default;
	}

	private static ValueTask<int> dmp_inp_map(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		Log.Information("Contents of InputMap:");

		foreach (
			var action in InputMap.GetActions()
				.Select(action => action.ToString())
				.Order(StringComparer.Ordinal))
		{
			Log.Information("{Action}:", action);

			var index = 1;
			foreach (var @event in InputMap.ActionGetEvents(action))
				Log.Information("{Index}. {Event}", index++, @event.AsText());
		}

		context.Return();
		return default;
	}

	private static void DumpTable(
		LuaTable table,
		string path,
		HashSet<LuaTable> visited)
	{
		if (!visited.Add(table))
		{
			Log.Information("{Path} = <already visited>", path);
			return;
		}

		foreach (var (luaKey, luaValue) in table.OrderBy(entry => entry.Key.ToString(), StringComparer.Ordinal))
		{
			var childPath = luaKey.Type is LuaValueType.Number
				? $"{path}[{luaKey}]"
				: $"{path}.{luaKey}";

			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch (luaValue.Type)
			{
				case LuaValueType.Table:
					var childTable = luaValue.Read<LuaTable>();

					if (visited.Contains(childTable))
						Log.Information("{Path} = <already visited>", childPath);
					else
					{
						Log.Information("{Path} = <table>", childPath);
						DumpTable(childTable, childPath, visited);
					}

					break;

				case LuaValueType.Function:
					Log.Information("{Path} = <function>", childPath);
					break;

				case LuaValueType.UserData:
					Log.Information("{Path} = <userdata>", childPath);
					break;

				case LuaValueType.Thread:
					Log.Information("{Path} = <thread>", childPath);
					break;

				case LuaValueType.String:
					Log.Information("{Path} = \"{Value}\"", childPath, EscapeString(luaValue.Read<string>()));
					break;

				default:
					Log.Information("{Path} = {Value}", childPath, luaValue);
					break;
			}
		}
	}

	private static string EscapeString(string value) =>
		value
			.Replace("\\", @"\\", StringComparison.Ordinal)
			.Replace("\r", "\\r", StringComparison.Ordinal)
			.Replace("\n", "\\n", StringComparison.Ordinal);

	private static async ValueTask<int> get_pub_ip4_addr(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		try
		{
			Log.Debug("Querying {Url}...", PublicIPv4AddressSourceUrl);
			var stopwatch = Stopwatch.StartNew();

			var address = (await Http.Client.GetStringAsync(
				PublicIPv4AddressSourceUrl,
				cancellationToken).ConfigureAwait(false)).Trim();

			stopwatch.Stop();
			Log.Information("Public IPv4 address: {Address} (PingMs={PingMs:F3})",
				address, stopwatch.Elapsed.TotalMilliseconds);

			context.Return(address);
		}
		catch (HttpRequestException exception)
		{
			Log.Error(exception, "Failed to get public IPv4 address.");
			context.Return();
		}

		return 0;
	}

	private static ValueTask<int> get_vsync_modes(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var modes = Enum.GetValues<DisplayServer.VSyncMode>();

		Log.Information("Available VSync modes:{Modes}",
			modes.Select(static mode => string.Create(CultureInfo.InvariantCulture, $"\n{mode} ({(int)mode})")));

		context.Return();
		return default;
	}

	private static ValueTask<int> help(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var env = new LuaTable();
		InjectCustomFunctions(env);

		var functions = env
			.Where(entry => entry.Value.Type is LuaValueType.Function)
			.Select(entry => entry.Key.Read<string>())
			.Order(StringComparer.Ordinal);

		Log.Information("Custom injected functions in _ENV:\n{Functions}", string.Join('\n', functions));

		context.Return();
		return default;
	}

	private static ValueTask<int> print(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var arguments = new List<string>(context.ArgumentCount);

		for (var i = 0; i < context.ArgumentCount; i++)
			arguments.Add(context.GetArgument<LuaValue>(i).ToString());

		Log.Information("Lua: \"{Message}\"", string.Join(' ', arguments));

		context.Return();
		return default;
	}

	private static ValueTask<int> quit(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		if (context.ArgumentCount > 0)
			Environment.Exit(0);
		else
			(Engine.GetMainLoop() as SceneTree)?.Quit();

		context.Return();
		return default;
	}

	private static ValueTask<int> restart(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		OS.SetRestartOnExit(true, OS.GetCmdlineArgs());
		(Engine.GetMainLoop() as SceneTree)?.Quit();

		context.Return();
		return default;
	}

	private static ValueTask<int> set_static_shader_on(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var isVisible = context.GetArgument<bool>(0);
		Log.Information("Setting Temporal Static shader visibility to: {IsVisible}", isVisible);

		// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
		var temporalShader = Main.Instance?.GetNode<CanvasLayer>("Temporal Static");
		temporalShader?.Visible = isVisible;

		context.Return();
		return default;
	}

	private static ValueTask<int> set_time(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		SetTimeOfDay(context);

		context.Return();
		return default;
	}

	private static void SetTimeOfDay(LuaFunctionExecutionContext context)
	{
		var timeOfDay = WorldManager.Instance?.World?.GetNode("Sky/TimeOfDay");
		if (timeOfDay is null)
			return;

		if (context.ArgumentCount is 0)
		{
			timeOfDay.Set("game_time_enabled", true);
			timeOfDay.Set("system_sync", true);

			Log.Information("Synced lighting time to system clock.");
			return;
		}

		var time = context.GetArgument<float>(0) % 24;

		timeOfDay.Set("game_time_enabled", false);
		timeOfDay.Set("system_sync", false);
		timeOfDay.Set("current_time", time);

		Log.Information("Set lighting time to {Hours} hour(s) after midnight.", time);
	}

	private static ValueTask<int> set_ui_dark_theme_on(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		bool? useDarkTheme = context.ArgumentCount > 0 ? context.GetArgument<bool>(0) : null;

		var theme = useDarkTheme switch
		{
			true => ThemeVariant.Dark,
			false => ThemeVariant.Light,
			_ => ThemeVariant.Default
		};

		Log.Information("Setting UI theme variant to: {$Theme}", theme);
		WeakReferenceMessenger.Default.Send(new SetUiThemeMessage(theme));

		context.Return();
		return default;
	}

	private static ValueTask<int> set_ui_scale(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var scale = context.GetArgument<double>(0);

		Log.Information("Setting UI render scale to: {Scale}", scale);
		WeakReferenceMessenger.Default.Send(new SetUiRenderScaleMessage(scale));

		context.Return();
		return default;
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

				if (Enum.IsDefined(typeof(DisplayServer.VSyncMode), value))
					mode = (DisplayServer.VSyncMode)value;
				break;
		}

		if (mode is { } result)
		{
			DisplayServer.WindowSetVsyncMode(result);
			Log.Debug("Set VSync mode to: {Mode}", result);
		}
		else
			Log.Error("Invalid VSync mode: {Mode}", argument);

		context.Return();
		return default;
	}

	private static ValueTask<int> tts(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var text = context.GetArgument<string>(0);
		var culture = context.ArgumentCount > 1 ? context.GetArgument<string>(1) : "en";
		var rate = context.ArgumentCount > 2 ? context.GetArgument<float>(2) : 1;
		var pitch = context.ArgumentCount > 3 ? context.GetArgument<float>(3) : 1;
		var volume = context.ArgumentCount > 4 ? context.GetArgument<float>(4) : 1;

		try
		{
			var voice = Speaker.Instance.VoiceForCulture(culture);

			_ = Speaker.Instance.SpeakAsync(text, voice, rate, pitch, volume)
				.ContinueWith(
					task => Log.Error(task.Exception, "TTS failed during playback."),
					CancellationToken.None,
					TaskContinuationOptions.OnlyOnFaulted,
					TaskScheduler.Default);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "TTS failed during setup.");
		}

		context.Return();
		return default;
	}

	private static async ValueTask<int> wait(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var timeMs = context.ArgumentCount > 0 ? context.GetArgument<int>(0) : (int)TimeSpan.MillisecondsPerSecond / 30;
		await Task.Delay(timeMs, cancellationToken).ConfigureAwait(false);

		context.Return();
		return 0;
	}
}
