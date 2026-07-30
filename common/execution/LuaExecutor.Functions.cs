using System.Diagnostics;
using Godot;
using Lua;
using Serilog;
using Environment = System.Environment;

// ReSharper disable InconsistentNaming

namespace Root.Common.Execution;

public partial class LuaExecutor
{
	private static void AddFunctions(LuaTable env)
	{
		env[nameof(print)] = new LuaFunction(print);
		env[nameof(quit)] = new LuaFunction(quit);
		env[nameof(help)] = new LuaFunction(help);
		env[nameof(add_test_insts)] = new LuaFunction(add_test_insts);
		env[nameof(clear_insts)] = new LuaFunction(clear_insts);
	}

	private static ValueTask<int> print(LuaFunctionExecutionContext context, CancellationToken ct)
	{
		var args = new List<string>(context.ArgumentCount);

		for (var i = 0; i < context.ArgumentCount; i++)
			args.Add(context.GetArgument<LuaValue>(i).ToString());

		Log.Information("Lua: \"{Message}\"", string.Join(' ', args));

		context.Return();
		return default;
	}

	private static ValueTask<int> quit(LuaFunctionExecutionContext context, CancellationToken ct)
	{
		if (context.ArgumentCount > 0)
			Environment.Exit(0);
		else
			(Engine.GetMainLoop() as SceneTree)?.Quit();

		context.Return();
		return default;
	}

	private static ValueTask<int> help(LuaFunctionExecutionContext context, CancellationToken ct)
	{
		var env = new LuaTable();
		AddFunctions(env);

		var functions = env
			.Where(x => x.Value.Type is LuaValueType.Function)
			.Select(x => x.Key.Read<string>())
			.Order(StringComparer.Ordinal);

		Log.Information("Functions:\n{Functions}", string.Join(Environment.NewLine, functions));

		context.Return();
		return default;
	}

	private static ValueTask<int> add_test_insts(LuaFunctionExecutionContext context, CancellationToken ct)
	{
		var plotId = context.GetArgument<int>(0);
		var count = context.GetArgument<int>(1);
		const int positionMinMax = 15;

		var instances = GPlots.Get(plotId)!.Instances;
		var assetIds = GAssets.GetAll().Select(a => a.Id).ToArray();
		var rng = Random.Shared;

		var stopwatch = Stopwatch.StartNew();

		for (var i = 0; i < count; i++)
		{
			var assetId = assetIds[rng.Next(assetIds.Length)];

			Vector3 position;
			Vector3 axis;

			do
			{
				position = new Vector3(
					rng.Next(-positionMinMax, positionMinMax),
					rng.Next(-positionMinMax, positionMinMax),
					rng.Next(-positionMinMax, positionMinMax));

				axis = position.Normalized();
			} while (!axis.IsNormalized());

			var rotation = new Quaternion(
				axis,
				(float)(rng.NextDouble() - 0.5 * Math.PI));

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

	private static ValueTask<int> clear_insts(LuaFunctionExecutionContext context, CancellationToken ct)
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
}
