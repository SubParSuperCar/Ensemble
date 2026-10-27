using Serilog;

namespace EnsembleRoot.Sessions;

public partial class SessionManager
{
	private static readonly TimeSpan KickTimeout = TimeSpan.FromSeconds(2);

	// Server-side, until the session ends; keyed by AddressKey
	private readonly HashSet<string> _bannedAddresses = new(StringComparer.Ordinal);

	private readonly HashSet<int> _kickedPeerIds = [];

	/// <remarks>IPv4 addresses and IPv6 /64 prefixes (e.g., <c>2001:db8::/64</c>), as unbans accept them.</remarks>
	public IReadOnlyCollection<string> BannedAddresses => _bannedAddresses;

	public void Kick(int peerId) => Kick(peerId, string.Empty);

	public void Kick(int peerId, string reason)
	{
		if (!IsServer || !IsPeerConnected(peerId))
		{
			Log.Warning("Cannot kick peer {PeerId}", peerId);
			return;
		}

		Log.Information("Kicking peer {PeerId}... (Reason={Reason})", peerId, reason);
		Disconnect(peerId, reason.Length is 0 ? "Kicked by host." : $"Kicked by host: {reason}");
	}

	public void Ban(int peerId) => Ban(peerId, string.Empty);

	/// <summary>Bans <paramref name="peerId" />'s IP address, like <see cref="BanAddress(string, string)" />.</summary>
	public void Ban(int peerId, string reason)
	{
		if (!IsServer || !IsPeerConnected(peerId) || _session?.GetAddress(peerId) is not { Length: > 0 } address)
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
		if (!IsServer || !AddressKey.TryCreate(address, out var key))
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
			if (!_kickedPeerIds.Contains(peerId) && IsBanned(peerId))
				Disconnect(peerId, notice);
	}

	/// <remarks>Peers already kicked by the ban are still disconnected.</remarks>
	public void UnbanAddress(string address)
	{
		if (!AddressKey.TryCreate(address, out var key) || !_bannedAddresses.Remove(key))
		{
			Log.Warning("Address {Address} is not banned", address);
			return;
		}

		Log.Information("Unbanned address {Address}", key);
	}

	public void UnbanAll()
	{
		Log.Information("Unbanned {Count} address(es)", _bannedAddresses.Count);
		_bannedAddresses.Clear();
	}

	private bool IsBanned(int peerId) =>
		_bannedAddresses.Count > 0 && _session?.GetAddress(peerId) is { } address &&
		AddressKey.TryCreate(address, out var key) && _bannedAddresses.Contains(key);

	private void Disconnect(int peerId, string notice)
	{
		RpcId(peerId, MethodName.RpcEndSession, notice);

		// Until disconnected, it may ignore the notice, so it gets no more updates and its requests are dropped
		_syncedPeerIds.Remove(peerId);
		_kickedPeerIds.Add(peerId);
		_pendingRpcsByPeerId.Remove(peerId);

		ScheduleForSession(KickTimeout, () =>
		{
			if (IsPeerConnected(peerId))
				ForceDisconnect(peerId);
		});
	}

	private void ClearModeration()
	{
		_bannedAddresses.Clear();
		_kickedPeerIds.Clear();
	}
}
