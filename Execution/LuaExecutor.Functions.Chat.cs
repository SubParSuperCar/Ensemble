using System.Globalization;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Scripts.Profanity;
using EnsembleRoot.Sessions.Actions;
using Lua;
using Serilog;

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	// ReSharper disable once InconsistentNaming
	private static ValueTask<int> chat(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		SendChatMessageAction.Send(context.GetArgument<string>(0));

		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_chat(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var manager = GChatManager;
		var messages = manager.Messages.Select(static message => string.Create(
			CultureInfo.InvariantCulture, $"[{message.UtcPostedAt.ToLocalTime():HH:mm:ss}] {message}"));

		Log.Information(
			"Chat: {Count} message(s) (IsFileLoggingEnabled={IsFileLoggingEnabled}, IsTtsEnabled={IsTtsEnabled})\n" +
			"{Messages}",
			manager.Messages.Count, manager.IsFileLoggingEnabled, manager.IsTtsEnabled, string.Join('\n', messages));

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_chat_log_on(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var isEnabled = context.GetArgument<bool>(0);
		GChatManager.IsFileLoggingEnabled = isEnabled;

		Log.Information("Set chat file logging to {IsEnabled}", isEnabled);

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_chat_tts_on(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var isEnabled = context.GetArgument<bool>(0);
		GChatManager.IsTtsEnabled = isEnabled;

		Log.Information("Set chat TTS to {IsEnabled}", isEnabled);

		return context.ReturnNothing();
	}

	private static ValueTask<int> set_profanity_filter_on(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var isEnabled = context.GetArgument<bool>(0);

		if (GSessionManager is { IsActive: true, IsServer: false })
		{
			Log.Warning("Only the host can toggle the profanity filter");
			return context.ReturnNothing();
		}

		ProfanityFilterManager.IsPreferred = isEnabled;

		if (GSessionManager.IsActive)
			new SetProfanityFilterAction(isEnabled).Submit();

		Log.Information("Set profanity filter to {IsEnabled}", isEnabled);

		return context.ReturnNothing();
	}
}
