using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using EnsembleRoot.Common.Networking;
using EnsembleRoot.Scripts.Profanity;
using EnsembleRoot.Sessions;
using Lua;
using Serilog;

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private const string PublicIPv4AddressSourceUrl = HttpsScheme + "api.ipify.org";

	// Worse than any real connection worth simulating
	private const int MaxSimulatedDelayMs = 4096;

	private static ValueTask<int> ban_peer(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		GSessionManager.Ban(context.GetArgument<int>(0), context.GetArgumentOrDefault(1, string.Empty));

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

	private static ValueTask<int> set_net_sim(LuaFunctionExecutionContext context, CancellationToken cancellationToken)
	{
		var latencyMs = context.GetArgumentOrDefault(0, 0d);
		var jitterMs = context.GetArgumentOrDefault(1, 0d);
		var lossRate = context.GetArgumentOrDefault(2, 0d);

		if (
			latencyMs is not (>= 0 and <= MaxSimulatedDelayMs) || jitterMs is not (>= 0 and <= MaxSimulatedDelayMs) ||
			lossRate is not (>= 0 and <= 1))
		{
			Log.Error(
				"Invalid network simulation: latency and jitter take 0 to {Max} ms, and loss 0 to 1",
				MaxSimulatedDelayMs);

			return context.ReturnNothing();
		}

		NetworkConditions.Set((int)latencyMs, (int)jitterMs, lossRate);
		Log.Information(
			"Simulating {LatencyMs} ms latency, {JitterMs} ms jitter, and {Loss:P0} loss on incoming live state",
			(int)latencyMs, (int)jitterMs, lossRate);

		return context.ReturnNothing();
	}
}
