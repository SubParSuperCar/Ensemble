using System.Globalization;
using System.Runtime.CompilerServices;
using EnsembleRoot.Scripts.Chat;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

/// <summary>
///     Removes a player from their plot. The host can kick anyone else, and owners their plot's occupants but the host.
/// </summary>
public readonly record struct KickOccupantAction(string PlayerId) : INetworkAction<KickOccupantAction>
{
	public static int TokenCost { get; } = 20;

	public static KickOccupantAction FromPayload(Array<Variant> payload) => new(payload[0].AsString());

	public Array<Variant> ToPayload() => [PlayerId];

	public ActionValidation Validate(ActionSource source)
	{
		if (GPlots.GetOccupant(PlayerId)?.Plot is not { } plot)
			return Reject("Player not on a plot.");

		if (string.Equals(PlayerId, source.PlayerId, StringComparison.Ordinal))
			return Reject("Players can't kick themselves.");

		// The host has authority over every plot
		if (source.IsHost)
			return Accept;

		if (!string.Equals(plot.Occupants.Owner?.Player.Id, source.PlayerId, StringComparison.Ordinal))
			return Reject("Player not the owner.");

		return GSessionManager.GetPeerByPlayerId(PlayerId) is { IsHost: true }
			? Reject("Host can't be kicked.")
			: Accept;
	}

	public void Apply(ActionSource source)
	{
		var plotId = GPlots.GetOccupant(PlayerId)!.Plot!.Id;
		var name = GPlayers.GetPlayer(PlayerId)?.Name;
		var text = string.Create(CultureInfo.InvariantCulture, $"{name} was kicked from plot {plotId}.");

		GPlots.SetPlot(PlayerId, None);
		GChatManager.Add(new ChatMessage { Text = text });
	}

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<KickOccupantAction>();
}
