using EnsembleRoot.SessionManager.Snapshots;
using Godot;
using Godot.Collections;
using Serilog;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace EnsembleRoot.SessionManager;

public partial class SessionManager
{
	private readonly HashSet<int> _syncedPeerIds = [];

	/// <remarks>Server-side only: whether the peer has received its snapshots and so has its world.</remarks>
	public bool IsPeerSynced(int peerId) => _syncedPeerIds.Contains(peerId);

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void RpcRequestSnapshots()
	{
		if (!IsServer)
			return;

		var senderId = Multiplayer.GetRemoteSenderId();
		EnqueueRpc(senderId, 1, () => SendSnapshots(senderId));
	}

	[Rpc]
	private void RpcRestoreSnapshot(string snapshotId, Array<Variant> payload)
	{
		NetworkSnapshotRegistry.Restore(snapshotId, payload);
		Log.Debug("Restored snapshot {SnapshotId} for peer {PeerId}", snapshotId, LocalPeerId);
	}

	private void SendSnapshots(int peerId)
	{
		if (!_peersById.ContainsKey(peerId) || !_syncedPeerIds.Add(peerId))
			return;

		var stopwatch = Stopwatch.StartNew();
		var count = 0;

		foreach (var (snapshotId, payload) in NetworkSnapshotRegistry.CaptureAll())
		{
			RpcId(peerId, MethodName.RpcRestoreSnapshot, snapshotId, payload);
			count++;
		}

		stopwatch.Stop();
		Log.Debug(
			"Sent {Count} snapshot(s) to peer {PeerId} in {ElapsedMs:F3} ms",
			count,
			peerId,
			stopwatch.Elapsed.TotalMilliseconds);
	}

	private void RpcSynced(StringName method, params Variant[] args)
	{
		foreach (var peerId in _syncedPeerIds)
			RpcId(peerId, method, args);
	}
}
