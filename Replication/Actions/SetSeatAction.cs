using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EnsembleRoot.Scripts.Assets.Components;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

/// <summary>Sits the player in an instance's seat, or stands them up with a null plot.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct SetSeatAction(int? PlotId, int InstanceId) : INetworkAction<SetSeatAction>
{
	public static int TokenCost { get; } = 10;

	private Seat? Seat =>
		PlotId is { } plotId ? GPlotManager.GetHandleOrNull(plotId)?.Creation?.GetSeat(InstanceId) : null;

	public static SetSeatAction FromPayload(Array<Variant> payload) =>
		new(payload[0].AsInt32() is var plotId and not None ? plotId : null, payload[1].AsInt32());

	public Array<Variant> ToPayload() => [PlotId ?? None, InstanceId];

	public ActionValidation Validate(ActionSource source)
	{
		if (GPlayerManager.GetHandleOrNull(source.PlayerId) is not { } player)
			return Reject("Player not found.");

		if (PlotId is null)
			return player.Seat is null ? Reject("Player not seated.") : Accept;

		if (player.Seat is not null)
			return Reject("Player already seated.");

		return Seat switch
		{
			null => Reject("Seat not found."),
			{ Handle.Assembly.IsKilled: true } => Reject("Seat out of the world."),
			{ Occupant: not null } => Reject("Seat taken."),
			_ => Accept
		};
	}

	public void Apply(ActionSource source)
	{
		var player = GPlayerManager.GetHandleOrNull(source.PlayerId);

		if (Seat is { } seat)
			player?.Sit(seat);
		else
			player?.Vacate();
	}

	[ModuleInitializer]
	internal static void Register()
	{
		NetworkActionRegistry.Register<SetSeatAction>();

		Seat.SitRequested += static seat => new SetSeatAction(seat.Handle.Plot.Id, seat.Handle.InstanceId).Submit();
		Seat.LeaveRequested += static () => new SetSeatAction(null, None).Submit();
	}
}
