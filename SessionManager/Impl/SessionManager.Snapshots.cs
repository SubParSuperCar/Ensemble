using EnsembleRoot.SessionManager.Snapshots;
using Godot;
using Godot.Collections;
using Serilog;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace EnsembleRoot.SessionManager;

public partial class SessionManager
{
	private void SendSnapshots(int peerId)
	{
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

	[Rpc]
	private static void RpcRestoreSnapshot(string snapshotId, Array<Variant> payload) =>
		NetworkSnapshotRegistry.Restore(snapshotId, payload);
}
