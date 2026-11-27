using System.Runtime.CompilerServices;
using EnsembleRoot.Scripts.World;
using EnsembleRoot.Sessions.Snapshots;
using Godot;
using Godot.Collections;

namespace EnsembleRoot.Replication.Snapshots;

internal static class GravitySnapshot
{
	private static Array<Variant> Capture() => [WorldManager.Instance?.World?.Gravity ?? WorldHandle.DefaultGravity];

	private static void Restore(Array<Variant> payload) =>
		WorldManager.Instance?.World?.Gravity = payload[0].AsSingle();

	[ModuleInitializer]
	internal static void Register() => NetworkSnapshotRegistry.Register(nameof(GravitySnapshot), Capture, Restore);
}
