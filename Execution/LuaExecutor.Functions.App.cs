using System.Diagnostics;
using EnsembleRoot.Common.Audio;
using EnsembleRoot.Common.Time;
using Godot;
using Lua;
using Serilog;
using Environment = System.Environment;

// ReSharper disable InconsistentNaming

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	// A 60 Hz frame, like Luau's task.wait()
	private const double DefaultWaitSeconds = 1 / 60d;

	// Keeps shifted dates well within DateTimeOffset's range
	private static readonly TimeSpan MaxClockOffset = TimeSpan.FromDays(100 * 365.25);

	private static async ValueTask<int> quit(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (context.HasArgument(0))
		{
			Log.Information("Force quitting...");
			await Log.CloseAndFlushAsync().ConfigureAwait(false);
			Environment.Exit(0);
		}
		else
		{
			Log.Information("Quitting...");
			GMain.Quit();
		}

		return context.Return();
	}

	private static ValueTask<int> restart(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		Log.Information("Restarting...");

		OS.SetRestartOnExit(true, OS.GetCmdlineArgs());
		GMain.Quit();

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_clock_offset(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var seconds = context.GetArgumentOrDefault(0, 0d);

		if (!double.IsFinite(seconds) || Math.Abs(seconds) > MaxClockOffset.TotalSeconds)
		{
			Log.Error("Invalid clock offset: {Seconds} s", seconds);
			return context.ReturnNothing();
		}

		var offset = TimeSpan.FromSeconds(seconds);

		GTimeProvider.Source =
			offset == TimeSpan.Zero ? TimeProvider.System : new OffsetTimeProvider(TimeProvider.System, offset);

		Log.Information("Set clock offset to {Offset}", offset);

		return context.ReturnNothing();
	}

	private static ValueTask<int> tts(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var text = context.GetArgument<string>(0);
		var culture = context.GetArgumentOrDefault(1, "en");
		var rate = context.GetArgumentOrDefault(2, 1f);
		var pitch = context.GetArgumentOrDefault(3, 1f);
		var volume = context.GetArgumentOrDefault(4, 1f);

		TextToSpeech.Speak(text, culture, rate, pitch, volume, cancellationToken);

		return context.ReturnNothing();
	}

	// Takes and returns seconds, like Luau's task.wait(), which returns the time actually waited
	private static async ValueTask<int> wait(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var seconds = context.GetArgumentOrDefault(0, DefaultWaitSeconds);

		if (!double.IsFinite(seconds) || seconds < 0)
		{
			Log.Error("Invalid wait: {Seconds} s", seconds);
			return context.Return();
		}

		var start = Stopwatch.GetTimestamp();
		await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken).ConfigureAwait(true);

		return context.Return(Stopwatch.GetElapsedTime(start).TotalSeconds);
	}
}
