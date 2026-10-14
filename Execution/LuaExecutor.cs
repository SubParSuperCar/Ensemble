using System.Diagnostics;
using Lua;
using Lua.Standard;
using Serilog;

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	// Like Roblox Studio's script timeout: the main thread can't hang on a script that never yields
	private static readonly TimeSpan MaxBlockingTime = TimeSpan.FromSeconds(5);

	// Per run, since several scripts can run at once
	private static readonly AsyncLocal<CancellationTokenSource?> Budget = new();

	public static async Task<LuaResult> ExecuteAsync(string source, CancellationToken cancellationToken = default)
	{
		source = source.Trim();
		Log.Information(">\n{Source}", source);

		var state = LuaState.Create();
		state.OpenStandardLibraries();
		InjectCustomFunctions(state.Environment);

		using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		budget.CancelAfter(MaxBlockingTime);
		Budget.Value = budget;

		var stopwatch = Stopwatch.StartNew();
		LuaValue[] results;

		try
		{
			results = await state.DoStringAsync(source, cancellationToken: budget.Token).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			Log.Error(
				"< Timed out after {ElapsedMs:F3} ms: blocked the main thread for {Seconds} s without yielding",
				stopwatch.Elapsed.TotalMilliseconds, MaxBlockingTime.TotalSeconds);

			return new LuaResult(LuaOutcome.TimedOut, []);
		}
		catch (OperationCanceledException)
		{
			Log.Information("< Canceled after {ElapsedMs:F3} ms", stopwatch.Elapsed.TotalMilliseconds);
			return new LuaResult(LuaOutcome.Canceled, []);
		}
		catch (Exception exception) when (
			exception is LuaRuntimeException { InnerException: null } or LuaCompileException or LuaParseException)
		{
			// A script error, e.g., a bad argument, whose stack trace would only point into the interpreter
			Log.Error(
				// ReSharper disable once ExceptionLoggedAsTextProblem
				"< Failed after {ElapsedMs:F3} ms: {Error}", stopwatch.Elapsed.TotalMilliseconds, exception.Message);

			return new LuaResult(LuaOutcome.Failed, []);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "< Failed after {ElapsedMs:F3} ms", stopwatch.Elapsed.TotalMilliseconds);
			return new LuaResult(LuaOutcome.Failed, []);
		}

		stopwatch.Stop();
		Log.Information(
			"< [{Results}] ({ElapsedMs:F3} ms)", string.Join(", ", results), stopwatch.Elapsed.TotalMilliseconds);

		return new LuaResult(LuaOutcome.Succeeded, results);
	}

	// Yielding doesn't block the main thread, so it pauses the budget
	private static void PauseBudget() => Budget.Value?.CancelAfter(Timeout.InfiniteTimeSpan);
	private static void ResumeBudget() => Budget.Value?.CancelAfter(MaxBlockingTime);
}
