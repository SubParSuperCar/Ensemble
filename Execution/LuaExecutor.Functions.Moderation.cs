using Lua;
using Serilog;

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private static ValueTask<int> ban_addr(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		GSessionManager.BanAddress(context.GetArgument<string>(0), context.GetArgumentOrDefault(1, string.Empty));

		return context.ReturnNothing();
	}

	private static ValueTask<int> ban_peer(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (FindPeerId(context.GetArgument<LuaValue>(0)) is { } peerId)
			GSessionManager.Ban(peerId, context.GetArgumentOrDefault(1, string.Empty));

		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_bans(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var addresses = GSessionManager.BannedAddresses;

		Log.Information(
			"Banned addresses: {Count}\n{Addresses}",
			addresses.Count, string.Join('\n', addresses.Order(StringComparer.Ordinal)));

		return context.ReturnNothing();
	}

	private static ValueTask<int> kick_peer(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		if (FindPeerId(context.GetArgument<LuaValue>(0)) is { } peerId)
			GSessionManager.Kick(peerId, context.GetArgumentOrDefault(1, string.Empty));

		return context.ReturnNothing();
	}

	private static ValueTask<int> unban_addr(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		GSessionManager.UnbanAddress(context.GetArgument<string>(0));

		return context.ReturnNothing();
	}

	private static ValueTask<int> unban_all(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		GSessionManager.UnbanAll();

		return context.ReturnNothing();
	}
}
