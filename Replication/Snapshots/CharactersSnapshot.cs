using System.Runtime.CompilerServices;
using EnsembleRoot.Sessions.Snapshots;
using Godot;
using Godot.Collections;
using Serilog;

namespace EnsembleRoot.Replication.Snapshots;

/// <summary>
///     The characters' latest states, which the host relays anyway, so a joining peer sees idle characters right away
///     instead of after their next keepalive.
/// </summary>
internal static class CharactersSnapshot
{
	private static Array<Variant> Capture()
	{
		var payload = new Array<Variant>();

		foreach (var player in GPlayers.GetAll())
			if (GPlayerManager.GetHandleOrNull(player.Id)?.ReplicatedState is var (position, yaw))
				payload.Add(new Array<Variant> { player.Id, position, yaw });

		return payload;
	}

	private static void Restore(Array<Variant> payload)
	{
		foreach (var entry in payload)
		{
			if (
				entry.VariantType is not Variant.Type.Array ||
				entry.AsGodotArray() is not
				[
					{ VariantType: Variant.Type.String } playerId, { VariantType: Variant.Type.Vector3 } position,
					{ VariantType: Variant.Type.Float } yaw
				])
			{
				Log.Warning("Skipped malformed character in snapshot");
				continue;
			}

			GPlayerManager.GetHandleOrNull(playerId.AsString())
				?.RestoreReplicatedState(position.AsVector3(), yaw.AsSingle());
		}
	}

	[ModuleInitializer]
	internal static void Register() => NetworkSnapshotRegistry.Register(nameof(CharactersSnapshot), Capture, Restore);
}
