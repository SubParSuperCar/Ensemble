using EnsembleRoot.Common.Audio;
using Godot;
using Lua;
using Serilog;
using Environment = System.Environment;

// ReSharper disable InconsistentNaming

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private const int DefaultWaitDelayMs = (int)TimeSpan.MillisecondsPerSecond / 30;

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

	private static ValueTask<int> tts(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var text = context.GetArgument<string>(0);
		var culture = context.GetArgumentOrDefault(1, "en");
		var rate = context.GetArgumentOrDefault(2, 1f);
		var pitch = context.GetArgumentOrDefault(3, 1f);
		var volume = context.GetArgumentOrDefault(4, 1f);

		TextToSpeech.Speak(text, culture, rate, pitch, volume);

		return context.ReturnNothing();
	}

	private static async ValueTask<int> wait(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var delayMs = context.GetArgumentOrDefault(0, DefaultWaitDelayMs);

		if (delayMs < 0)
		{
			Log.Error("Invalid delay: {DelayMs} ms", delayMs);
			return context.Return();
		}

		await Task.Delay(delayMs, cancellationToken).ConfigureAwait(true);

		return context.Return();
	}
}
