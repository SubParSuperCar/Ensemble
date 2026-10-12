using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.GdCore.Players;
using EnsembleRoot.Replication.Actions;
using EnsembleRoot.Scripts.Profanity;
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
		var messages = manager.Messages.Select(static message => string.Create(
			CultureInfo.InvariantCulture, $"[{message.UtcPostedAt.ToLocalTime():HH:mm:ss}] {message}"));

		Log.Information(
			"Chat: {Count} message(s) (IsFileLoggingEnabled={IsFileLoggingEnabled}, IsTtsEnabled={IsTtsEnabled})\n" +
			"{Messages}",
			manager.Messages.Count, manager.IsFileLoggingEnabled, manager.IsTtsEnabled, string.Join('\n', messages));

		return context.ReturnNothing();
	}

	private static ValueTask<int> dmp_peers(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var manager = GSessionManager;
		var peers = manager.Peers.Values.OrderBy(static peer => peer.Id).Select(static peer => peer.ToDict());

		Log.Information(
			"Session: {Mode} (Version={Version}, Port={Port}, HasPassword={HasPassword}, IsDedicated={IsDedicated}, " +
			"PortMapping={PortMapping}, IsProfanityFilterEnabled={IsProfanityFilterEnabled}, " +
			"IsProfanityFilterPreferred={IsProfanityFilterPreferred})\n{Peers}",
			manager.Mode, GameVersion, manager.Port, manager.HasPassword, manager.IsDedicated,
			manager.PortMappingState, GProfanityFilterManager.IsEnabled, ProfanityFilterManager.IsPreferred,
			string.Join('\n', peers));

		return context.ReturnNothing();
	}

	private static ValueTask<int> kick_occupant(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var query = context.GetArgument<string>(0);

		if (FindPlayer(query) is { } player)
			new KickOccupantAction(player.Id).Submit();
		else
			Log.Error("Player with id or name {Query} not found", query);

		return context.ReturnNothing();
	}

	private static ValueTask<int> kick_peer(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		GSessionManager.Kick(context.GetArgument<int>(0), context.GetArgumentOrDefault(1, string.Empty));

		return context.ReturnNothing();
	}

	private static ValueTask<int> log_lan_ip4_addr(
		LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		// Only the adapter facing the LAN has a gateway; virtual ones (e.g., Docker bridges) can also report Ethernet
		var address = NetworkInterface.GetAllNetworkInterfaces()
			.Where(static adapter =>
				adapter.OperationalStatus is OperationalStatus.Up &&
				adapter.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)
			.Select(static adapter => adapter.GetIPProperties())
			.Where(static properties => properties.GatewayAddresses.Count > 0)
			.SelectMany(static properties => properties.UnicastAddresses)
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

	/// <summary>Finds a player by ID or display name.</summary>
	private static GdPlayer? FindPlayer(string query) =>
		GPlayers.GetAll().FirstOrDefault(player =>
			string.Equals(player.Id, query, StringComparison.Ordinal) ||
			string.Equals(player.Name, query, StringComparison.Ordinal));
}
