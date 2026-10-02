using Godot;
using Godot.Collections;

namespace EnsembleRoot.SessionManager;

/// <summary>
///     A registered participant of the current session, mirrored on every peer.
/// </summary>
/// <remarks>
///     <see cref="PingMs" /> is the round-trip time to the host in milliseconds, as sampled and broadcast by the host.
///     <see cref="Address" /> is only known across the host link: the host knows every client's, and clients only the
///     host's.
/// </remarks>
public partial class Peer : RefCounted
{
	[Signal]
	public delegate void PingUpdatedEventHandler(int pingMs);

	public int Id { get; internal init; }
	public string PlayerId { get; internal init; } = string.Empty;
	public string DisplayName { get; internal init; } = string.Empty;
	public string Address { get; internal init; } = string.Empty;

	public bool IsLocal { get; internal init; }
	public bool IsHost => Id == MultiplayerPeer.TargetPeerServer;

	public double UtcJoinedAtUnix { get; } = GTimeProvider.GetUtcNow().ToUnixTimeMilliseconds() / 1000d;

	public int PingMs { get; private set; }

	public Dictionary ToDict() =>
		new()
		{
			["id"] = Id,
			["playerId"] = PlayerId,
			["displayName"] = DisplayName,
			["address"] = Address,
			["isLocal"] = IsLocal,
			["isHost"] = IsHost,
			["utcJoinedAtUnix"] = UtcJoinedAtUnix,
			["pingMs"] = PingMs
		};

	public override string ToString() =>
		$"Peer(id={Id}, playerId={PlayerId}, displayName={DisplayName}, address={Address}, pingMs={PingMs})";

	internal void SetPing(int pingMs)
	{
		if (PingMs == pingMs)
			return;

		PingMs = pingMs;
		EmitSignal(SignalName.PingUpdated, pingMs);
	}
}
