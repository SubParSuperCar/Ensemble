using System.Runtime.CompilerServices;
using EnsembleRoot.Sessions.Snapshots;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.Replication.Snapshots;

internal static class ProfanityFilterSnapshot
{
	private static Array<Variant> Capture() => [GProfanityFilterManager.IsEnabled];

	private static void Restore(Array<Variant> payload) => GProfanityFilterManager.SetEnabled(payload[0].AsBool());

	[ModuleInitializer]
	internal static void Register() =>
		NetworkSnapshotRegistry.Register(nameof(ProfanityFilterSnapshot), Capture, Restore);
}
