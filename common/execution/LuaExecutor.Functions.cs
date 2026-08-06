using System.Diagnostics;
using Godot;
using Lua;
using Root.Common.Logging;
using Root.Common.Network;
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
		env[nameof(quit)] = new LuaFunction(quit);
		env[nameof(help)] = new LuaFunction(help);
		env[nameof(add_test_insts)] = new LuaFunction(add_test_insts);
		env[nameof(clear_insts)] = new LuaFunction(clear_insts);
		env[nameof(get_pub_ip4_addr)] = new LuaFunction(get_pub_ip4_addr);
		env[nameof(wipe_log)] = new LuaFunction(wipe_log);
		env[nameof(dump_env)] = new LuaFunction(dump_env);
		env[nameof(dump_input_map)] = new LuaFunction(dump_input_map);
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

	private static ValueTask<int> add_test_insts(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var plotId = context.GetArgument<int>(0);
		var count = context.GetArgument<int>(1);
		const int positionMinMax = 15;

		var instances = GPlots.Get(plotId)!.Instances;
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
					random.Next(-positionMinMax, positionMinMax),
					random.Next(-positionMinMax, positionMinMax),
					random.Next(-positionMinMax, positionMinMax));

				axis = position.Normalized();
			} while (!axis.IsNormalized());

			var rotation = new Quaternion(
				axis,
				(float)(random.NextDouble() - 0.5 * Math.PI));

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

	private static ValueTask<int> clear_insts(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var plotId = context.GetArgument<int>(0);
		var instances = GPlots.Get(plotId)!.Instances;
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

	private static ValueTask<int> wipe_log(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		LogHistorySinkVolatile.Clear();

		context.Return();
		return default;
	}

	private static ValueTask<int> dump_env(
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

	private static ValueTask<int> dump_input_map(
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
}
