using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Serilog;

namespace EnsembleRoot.Sessions;

public partial class SessionManager
{
	// Server-side, until the session ends. IPv6 hosts pick addresses within their /64 freely, so a ban covers it.
	private readonly HashSet<string> _bannedAddresses = new(StringComparer.Ordinal);

	public void Ban(int peerId) => Ban(peerId, string.Empty);

	/// <summary>
	///     Kicks <paramref name="peerId" /> and every other peer at its IP address, and turns the address away until the
	///     session ends.
	/// </summary>
	public void Ban(int peerId, string reason)
	{
		var address = _session?.GetAddress(peerId) ?? string.Empty;

		if (!IsServer || peerId == LocalPeerId || !Multiplayer.GetPeers().Contains(peerId) || address.Length is 0)
		{
			Log.Warning("Cannot ban peer {PeerId}", peerId);
			return;
		}

		var key = ToBanKey(address);
		_bannedAddresses.Add(key);

		Log.Information("Banning peer {PeerId} by address {Address}... (Reason={Reason})", peerId, key, reason);
		var notice = reason.Length is 0 ? "Banned by host." : $"Banned by host: {reason}";

		foreach (var otherId in Multiplayer.GetPeers())
		{
			if (!_kickedPeerIds.Contains(otherId) && IsBanned(otherId))
				Disconnect(otherId, notice);
		}
	}

	private bool IsBanned(int peerId) =>
		_session?.GetAddress(peerId) is { Length: > 0 } address && _bannedAddresses.Contains(ToBanKey(address));

	private void ClearBans() => _bannedAddresses.Clear();

	private static string ToBanKey(string address)
	{
		if (!IPAddress.TryParse(address, out var ip))
			return address;

		if (ip.IsIPv4MappedToIPv6)
			return ip.MapToIPv4().ToString();

		if (ip.AddressFamily is not AddressFamily.InterNetworkV6)
			return ip.ToString();

		var bytes = ip.GetAddressBytes();
		bytes.AsSpan(8).Clear();

		return string.Create(CultureInfo.InvariantCulture, $"{new IPAddress(bytes)}/64");
	}
}
