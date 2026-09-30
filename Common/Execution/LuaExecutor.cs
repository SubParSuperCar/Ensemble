using System.Diagnostics;
using Lua;
using Lua.Standard;
using Serilog;

namespace EnsembleRoot.Common.Execution;

public static partial class LuaExecutor
{
	public static async Task<LuaValue[]> ExecuteAsync(
		string source,
		CancellationToken cancellationToken = default)
	{
		source = source.Trim();
		Log.Information(">\n{Source}", source);

		var state = LuaState.Create();
		state.OpenStandardLibraries();

		InjectCustomFunctions(state.Environment);

		var stopwatch = Stopwatch.StartNew();
		LuaValue[] results;

		try
		{
			results = await state.DoStringAsync(source, cancellationToken: cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			Log.Information("< Canceled after {ElapsedMs:F3} ms", stopwatch.Elapsed.TotalMilliseconds);
			return [];
		}
		catch (Exception exception)
		{
			Log.Error(exception, "< Failed after {ElapsedMs:F3} ms", stopwatch.Elapsed.TotalMilliseconds);
			return [];
		}

		stopwatch.Stop();
		Log.Information(
			"< [{Results}] ({ElapsedMs:F3} ms)",
			string.Join(", ", results.Select(static value => value.ToString())),
			stopwatch.Elapsed.TotalMilliseconds);

		return results;
	}
}
