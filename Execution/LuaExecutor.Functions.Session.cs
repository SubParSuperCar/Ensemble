using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Scripts.Chat;
using EnsembleRoot.Sessions.Actions;
using Lua;
using Serilog;

// ReSharper disable InconsistentNaming

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private const string PublicIPv4AddressSourceUrl = HttpsScheme + "api.ipify.org";

	private static ValueTask<int> chat(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		SendChatMessageAction.Send(context.GetArgument<string>(0));

		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_chat(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var manager = GChatManager;

		Log.Information(
			"Chat: {Count} message(s) (IsFilterEnabled={IsFilterEnabled}, IsFilterPreferred={IsFilterPreferred}, " +
			"IsFileLoggingEnabled={IsFileLoggingEnabled})",
			manager.Messages.Count, manager.IsFilterEnabled, ChatManager.IsFilterPreferred,
			manager.IsFileLoggingEnabled);

		foreach (var message in manager.Messages)
			Log.Information("{$Message}", message.ToDict());

		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_peers(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var manager = GSessionManager;

		Log.Information(
			"Session: {Mode} (Version={Version}, Port={Port}, HasPassword={HasPassword}, IsDedicated={IsDedicated}, " +
			"PortMapping={PortMapping})",
			manager.Mode, GameVersion, manager.Port, manager.HasPassword, manager.IsDedicated,
			manager.PortMappingState);

		foreach (var peer in manager.Peers.Values.OrderBy(static peer => peer.Id))
			Log.Information("{$Peer}", peer.ToDict());

		return context.ReturnNothing();
	}

	private static ValueTask<int> kick(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		GSessionManager.Kick(context.GetArgument<int>(0), context.GetArgumentOrDefault(1, string.Empty));

		return context.ReturnNothing();
	}

	private static ValueTask<int> log_lan_ip4_addr(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var address = NetworkInterface.GetAllNetworkInterfaces()
			.Where(static adapter =>
				adapter.OperationalStatus is OperationalStatus.Up &&
				adapter.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)
			.SelectMany(static adapter => adapter.GetIPProperties().UnicastAddresses)
			.Select(static unicast => unicast.Address)
			.FirstOrDefault(static ip => ip.AddressFamily is AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

		Log.Information("Local Area Network (LAN) IPv4 address: {Address}", address);

		return context.ReturnNothing();
	}

	private static async ValueTask<int> log_wan_ip4_addr(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		try
		{
			Log.Debug("Querying {Url}...", PublicIPv4AddressSourceUrl);
			var stopwatch = Stopwatch.StartNew();

			var response = await Http.Client.GetStringAsync(PublicIPv4AddressSourceUrl, cancellationToken)
				.ConfigureAwait(true);

			stopwatch.Stop();
			Log.Information(
				"Wide Area Network (WAN) IPv4 address: {Address} (queried in {ElapsedMs:F3} ms)",
				response.Trim(), stopwatch.Elapsed.TotalMilliseconds);
		}
		catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
		{
			Log.Error(exception, "Failed to query WAN IPv4 address");
		}

		return context.Return();
	}

	private static ValueTask<int> set_chat_filter_on(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var isEnabled = context.GetArgument<bool>(0);

		if (GSessionManager is { IsActive: true, IsServer: false })
		{
			Log.Warning("Only the host can toggle the chat filter");
			return context.ReturnNothing();
		}

		ChatManager.IsFilterPreferred = isEnabled;

		if (GSessionManager.IsActive)
			new SetChatFilterAction(isEnabled).Submit();

		Log.Information("Set chat filter to {IsEnabled}", isEnabled);

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
}
