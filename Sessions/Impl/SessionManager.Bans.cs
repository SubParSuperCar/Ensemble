using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Serilog;

namespace EnsembleRoot.Sessions;

public partial class SessionManager
{
	private const string SubnetSuffix = "/64";

	// Server-side, until the session ends. IPv6 hosts pick addresses within their /64 freely, so a ban covers it.
	private readonly HashSet<string> _bannedAddresses = new(StringComparer.Ordinal);

	/// <remarks>IPv4 addresses and IPv6 /64 prefixes (e.g., <c>2001:db8::/64</c>), as unbans accept them.</remarks>
	public IReadOnlyCollection<string> BannedAddresses => _bannedAddresses;

	public void Ban(int peerId) => Ban(peerId, string.Empty);

	/// <summary>Bans <paramref name="peerId" />'s IP address, like <see cref="BanAddress(string, string)" />.</summary>
	public void Ban(int peerId, string reason)
	{
		if (
			!IsServer || peerId == LocalPeerId || !Multiplayer.GetPeers().Contains(peerId) ||
			_session?.GetAddress(peerId) is not { Length: > 0 } address)
		{
			Log.Warning("Cannot ban peer {PeerId}", peerId);
			return;
		}

		BanAddress(address, reason);
	}

	/// <summary>
	///     Kicks every peer connected from <paramref name="address" />, and turns it away until the session ends.
	/// </summary>
	/// <remarks>The host is never affected, even when clients share its address.</remarks>
	public void BanAddress(string address, string reason)
	{
		if (!IsServer || !TryGetBanKey(address, out var key))
		{
			Log.Warning("Cannot ban address {Address}", address);
			return;
		}

		// Peers at a banned address are kicked or turned away, so none is left to kick
		if (!_bannedAddresses.Add(key))
		{
			Log.Information("Address {Address} is already banned", key);
			return;
		}

		Log.Information("Banning address {Address}... (Reason={Reason})", key, reason);
		var notice = reason.Length is 0 ? "Banned by host." : $"Banned by host: {reason}";

		foreach (var peerId in Multiplayer.GetPeers())
		{
			if (!_kickedPeerIds.Contains(peerId) && IsBanned(peerId))
				Disconnect(peerId, notice);
		}
	}

	/// <remarks>Peers already kicked by the ban are still disconnected.</remarks>
	public void UnbanAddress(string address)
	{
		if (!TryGetBanKey(address, out var key) || !_bannedAddresses.Remove(key))
		{
			Log.Warning("Address {Address} is not banned", address);
			return;
		}

		Log.Information("Unbanned address {Address}", key);
	}

	public void UnbanAll()
	{
		Log.Information("Unbanned {Count} address(es)", _bannedAddresses.Count);
		ClearBans();
	}

	private bool IsBanned(int peerId) =>
		_bannedAddresses.Count > 0 && _session?.GetAddress(peerId) is { } address &&
		TryGetBanKey(address, out var key) && _bannedAddresses.Contains(key);

	private void ClearBans() => _bannedAddresses.Clear();

	// Stricter than IPAddress.TryParse, which takes shorthands like "12345" (0.0.48.57), and takes back an IPv6 key
	private static bool TryGetBanKey(string address, [NotNullWhen(true)] out string? key)
	{
		key = null;

		var hasSuffix = address.EndsWith(SubnetSuffix, StringComparison.Ordinal);
		var text = hasSuffix ? address[..^SubnetSuffix.Length] : address;

		if (!IPAddress.TryParse(text, out var ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any))
			return false;

		if (ip.AddressFamily is AddressFamily.InterNetwork || ip.IsIPv4MappedToIPv6)
		{
			key = !hasSuffix && (ip.IsIPv4MappedToIPv6 || text.AsSpan().Count('.') is 3)
				? ip.MapToIPv4().ToString()
				: null;

			return key is not null;
		}

		var bytes = ip.GetAddressBytes();
		bytes.AsSpan(8).Clear();
		key = string.Create(CultureInfo.InvariantCulture, $"{new IPAddress(bytes)}{SubnetSuffix}");

		return true;
	}
}
