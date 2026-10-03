using System.Diagnostics;
using EnsembleRoot.Common.Logging;
using EnsembleRoot.Common.Utils;
using Godot;
using Lua;
using Serilog;

// ReSharper disable InconsistentNaming

namespace EnsembleRoot.Common.Execution;

public static partial class LuaExecutor
{
	private static ValueTask<int> clr_log(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		Callable.From(VolatileLogHistorySink.Clear).CallDeferred();

		context.Return();
		return default;
	}

	private static ValueTask<int> dmp_asm_info(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var assemblyNames = AppDomain.CurrentDomain.GetAssemblies()
			.Select(static assembly => assembly.GetName())
			.OrderBy(static name => name.Name, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		Log.Information(
			"Loaded assemblies ({Count}):\n{Assemblies}",
			assemblyNames.Length,
			string.Join('\n', assemblyNames.Select(static name => $"~~> {name}")));

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
				.Select(static action => action.ToString())
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

		foreach (var (luaKey, luaValue) in table.OrderBy(static entry => entry.Key.ToString(), StringComparer.Ordinal))
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

	private static ValueTask<int> gc(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var before = GC.GetTotalMemory(false);

		Log.Information("GC heap size before: {BytesBefore}", ByteFormat.Humanize((ulong)before));
		var stopwatch = Stopwatch.StartNew();

		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
		GC.WaitForPendingFinalizers();
		GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

		stopwatch.Stop();

		var after = GC.GetTotalMemory(false);
		var reclaimed = Math.Max(0, before - after);

		Log.Information(
			"GC heap size after: {BytesAfter} (reclaimed {BytesReclaimed} in {ElapsedMs:F3} ms)",
			ByteFormat.Humanize((ulong)after),
			ByteFormat.Humanize((ulong)reclaimed),
			stopwatch.Elapsed.TotalMilliseconds);

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
			.Where(static entry => entry.Value.Type is LuaValueType.Function)
			.Select(static entry => $"-> {entry.Key.Read<string>()}")
			.Order(StringComparer.Ordinal);

		Log.Information("Custom injected functions in _ENV:\n{Functions}", string.Join('\n', functions));

		context.Return();
		return default;
	}

	private static ValueTask<int> print(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		Log.Information("Lua: \"{Message}\"", string.Join(' ', [.. context.Arguments]));

		context.Return();
		return default;
	}
}
