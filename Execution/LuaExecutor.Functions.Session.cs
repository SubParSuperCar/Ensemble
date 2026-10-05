using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using EnsembleRoot.Common.Networking;
using Lua;
using Serilog;

// ReSharper disable InconsistentNaming

namespace EnsembleRoot.Execution;

public static partial class LuaExecutor
{
	private const string PublicIPv4AddressSourceUrl = HttpsScheme + "api.ipify.org";

	private static ValueTask<int> dmp_peers(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var manager = GSessionManager;

		Log.Information(
			"Session: {Mode} (Version={Version}, Port={Port}, HasPassword={HasPassword}, IsDedicated={IsDedicated}, " +
			"PortMapping={PortMapping})",
			manager.Mode,
			SessionManager.SessionManager.Version,
			manager.Port,
			manager.HasPassword,
			manager.IsDedicated,
			manager.PortMappingState);

		foreach (var peer in manager.Peers.Values.OrderBy(static peer => peer.Id))
			Log.Information("{$Peer}", peer.ToDict());

		return context.ReturnNothing();
	}

	private static ValueTask<int> kick(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		GSessionManager.Kick(context.GetArgument<int>(0), context.GetArgumentOrDefault(1, string.Empty));

		return context.ReturnNothing();
	}

	private static ValueTask<int> log_lan_ip4_addr(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		var address = NetworkInterface.GetAllNetworkInterfaces()
			.Where(static ni =>
				ni.OperationalStatus is OperationalStatus.Up &&
				ni.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)
			.SelectMany(static ni => ni.GetIPProperties().UnicastAddresses)
			.Select(static a => a.Address)
			.FirstOrDefault(static a => a.AddressFamily is AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));

		Log.Information("Local Area Network (LAN) IPv4 address: {Address}", address);

		return context.ReturnNothing();
	}

	private static async ValueTask<int> log_wan_ip4_addr(
		LuaFunctionExecutionContext context,
		CancellationToken cancellationToken)
	{
		try
		{
			Log.Debug("Querying {Url}...", PublicIPv4AddressSourceUrl);
			var stopwatch = Stopwatch.StartNew();

			var address = (await Http.Client.GetStringAsync(
				PublicIPv4AddressSourceUrl,
				cancellationToken).ConfigureAwait(true)).Trim();

			stopwatch.Stop();
			Log.Information(
				"Wide Area Network (WAN) IPv4 address: {Address} (RequestMs={RequestMs:F3})",
				address,
				stopwatch.Elapsed.TotalMilliseconds);
		}
		catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
		{
			Log.Error(exception, "Failed to get public IPv4 address");
		}

		return context.Return();
	}
}
