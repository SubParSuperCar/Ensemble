using Godot;
using Godot.Collections;
using Serilog;

namespace EnsembleRoot.Sessions.Snapshots;

public static class NetworkSnapshotRegistry
{
	private static readonly System.Collections.Generic.Dictionary<string, Entry> EntriesBySnapshotId =
		new(StringComparer.Ordinal);

	public static void Register(string snapshotId, Func<Array<Variant>> capture, Action<Array<Variant>> restore)
	{
		if (!EntriesBySnapshotId.TryAdd(snapshotId, new Entry(capture, restore)))
			throw new InvalidOperationException($"Snapshot with id {snapshotId} is already registered.");
	}

	internal static IEnumerable<(string SnapshotId, Array<Variant> Payload)> CaptureAll() =>
		EntriesBySnapshotId.Select(static entry => (entry.Key, entry.Value.Capture()));

	internal static void Restore(string snapshotId, Array<Variant> payload)
	{
		if (!EntriesBySnapshotId.TryGetValue(snapshotId, out var entry))
		{
			Log.Warning("Discarded unknown snapshot {SnapshotId}", snapshotId);
			return;
		}

		try
		{
			entry.Restore(payload);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to restore snapshot {SnapshotId}", snapshotId);
		}
	}

	// ReSharper disable once MemberHidesStaticFromOuterClass
	private readonly record struct Entry(Func<Array<Variant>> Capture, Action<Array<Variant>> Restore);
}
