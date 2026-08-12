using System.Diagnostics;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.Messaging;
using Godot;
using Lua;
using Root.Common.Logging;
using Root.Common.Network;
using Root.Scripts.Main;
using Root.Ui.Impl.Messages;
using Serilog;
using Environment = System.Environment;

// ReSharper disable InconsistentNaming

namespace Root.Common.Execution;

public partial class LuaExecutor
{
	private const string PublicIp4AddressSourceUrl = "https://api.ipify.org";

	// TODO: Auto-scan functions
	private static void AddFunctions(LuaTable env)
	{
		env[nameof(print)] = new LuaFunction(print);
		env[nameof(help)] = new LuaFunction(help);
		env[nameof(quit)] = new LuaFunction(quit);
		env[nameof(wait)] = new LuaFunction(wait);
		env[nameof(add_test_insts)] = new LuaFunction(add_test_insts);
		env[nameof(clr_insts)] = new LuaFunction(clr_insts);
		env[nameof(clr_log)] = new LuaFunction(clr_log);
		env[nameof(set_time)] = new LuaFunction(set_time);
		env[nameof(dmp_env)] = new LuaFunction(dmp_env);
		env[nameof(dmp_inp_map)] = new LuaFunction(dmp_inp_map);
		env[nameof(get_pub_ip4_addr)] = new LuaFunction(get_pub_ip4_addr);
		env[nameof(set_ui_dark_theme_enabled)] = new LuaFunction(set_ui_dark_theme_enabled);
		env[nameof(set_static_shader_enabled)] = new LuaFunction(set_static_shader_enabled);
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

	private static ValueTask<int> help(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var env = new LuaTable();
		AddFunctions(env);

		var functions = env
			.Where(entry => entry.Value.Type is LuaValueType.Function)
			.Select(entry => entry.Key.Read<string>())
			.Order(StringComparer.Ordinal);

		Log.Information("Custom functions in _ENV:\n{Functions}", string.Join(Environment.NewLine, functions));

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

	private static async ValueTask<int> wait(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var timeMs = context.ArgumentCount > 0 ? context.GetArgument<int>(0) : (int)TimeSpan.MillisecondsPerSecond / 60;
		await Task.Delay(timeMs, cancellationToken).ConfigureAwait(false);

		context.Return();
		return 0;
	}

	private static ValueTask<int> add_test_insts(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var plotId = context.GetArgument<int>(0);
		var count = context.GetArgument<int>(1);
		var positionRange = GPlotManager.GetHandle(plotId).BoundarySize / 2 - Vector3.One;

		var instances = GPlots.GetPlot(plotId)!.Instances;
		var assetIds = GAssets.GetAll().Select(a => a.Id).ToArray();
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
					random.Next(0, (int)positionRange.Y * 2 - 1) + 0.5f,
					random.Next(-(int)positionRange.Z, (int)positionRange.Z));

				axis = position.Normalized();
			} while (!axis.IsNormalized());

			var rotation = new Quaternion(
				axis,
				(float)((random.NextDouble() - 0.5) * Math.Tau));

			instances.Add(assetId, position, rotation);
		}

		stopwatch.Stop();
		Log.Information("Added {Count} instance(s) to plot {PlotId} in {ElapsedMs:F3} msec",
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
		Log.Information("Removed {Count} instance(s) from plot {PlotId} in {ElapsedMs:F3} msec",
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
		LogHistorySinkVolatile.Clear();

		context.Return();
		return default;
	}

	private static ValueTask<int> set_time(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var timeOfDay = Main.Instance?.Game?.GetNode("Sky/TimeOfDay");
		if (timeOfDay is null)
			goto Return;

		if (context.ArgumentCount is 0)
		{
			timeOfDay.Set("game_time_enabled", true);
			timeOfDay.Set("system_sync", true);

			Log.Information("Synced lighting time to system clock");

			goto Return;
		}

		var time = context.GetArgument<float>(0) % 24;
		timeOfDay.Set("game_time_enabled", false);
		timeOfDay.Set("system_sync", false);
		timeOfDay.Set("current_time", time);

		Log.Information("Set lighting time to {Hours} hour(s) after midnight", time);

	Return:
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

		foreach (var (luaKey, luaValue) in table.OrderBy(e => e.Key.ToString(), StringComparer.Ordinal))
		{
			var childPath = luaKey.Type is LuaValueType.Number
				? $"{path}[{luaKey}]"
				: $"{path}.{luaKey}";

			// ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
			switch (luaValue.Type)
			{
				case LuaValueType.Table:
					{
						var childTable = luaValue.Read<LuaTable>();

						if (visited.Contains(childTable))
							Log.Information("{Path} = <already visited>", childPath);
						else
						{
							Log.Information("{Path} = <table>", childPath);
							DumpTable(childTable, childPath, visited);
						}

						break;
					}

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

	private static ValueTask<int> dmp_inp_map(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		Log.Information("Contents of InputMap:");

		foreach (var action in InputMap.GetActions()
					 .Select(a => a.ToString())
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
			Log.Debug("Querying {Url}...", PublicIp4AddressSourceUrl);

			var address = (await Http.Client.GetStringAsync(
				PublicIp4AddressSourceUrl,
				cancellationToken).ConfigureAwait(false)).Trim();

			Log.Information("Public IPv4 address: {Address}", address);
			context.Return(address);
		}
		catch (HttpRequestException exception)
		{
			Log.Error(exception, "Failed to get public IPv4 address");
			context.Return();
		}

		return 0;
	}

	private static ValueTask<int> set_ui_dark_theme_enabled(
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
		WeakReferenceMessenger.Default.Send(new SetThemeMessage(theme));

		context.Return();
		return default;
	}

	private static ValueTask<int> set_static_shader_enabled(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var isVisible = context.GetArgument<bool>(0);
		Log.Information("Setting temporal static shader visibility to: {IsVisible}", isVisible);

		var temporalShader = Main.Instance?.GetNode<CanvasLayer>("Temporal Static");
		temporalShader?.Visible = isVisible;

		context.Return();
		return default;
	}
}
