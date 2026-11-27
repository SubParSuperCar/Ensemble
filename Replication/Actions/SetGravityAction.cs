using System.Globalization;
using System.Runtime.CompilerServices;
using EnsembleRoot.Scripts.Chat;
using EnsembleRoot.Scripts.World;
using EnsembleRoot.Sessions.Actions;
using Godot;
using Godot.Collections;
using static EnsembleRoot.Sessions.Actions.ActionValidation;

namespace EnsembleRoot.Replication.Actions;

/// <summary>Sets the world's gravity on every peer, as each simulates its own creations under it.</summary>
public readonly record struct SetGravityAction(float Gravity) : INetworkAction<SetGravityAction>
{
	public const float MaxGravity = 100f;

	public static int TokenCost { get; } = 10;

	public static SetGravityAction FromPayload(Array<Variant> payload) => new(payload[0].AsSingle());

	public Array<Variant> ToPayload() => [Gravity];

	public ActionValidation Validate(ActionSource source)
	{
		if (!source.IsHost)
			return Reject("Only the host can set the gravity.");

		return Gravity is >= 0 and <= MaxGravity ? Accept : Reject("Invalid gravity.");
	}

	public void Apply(ActionSource source)
	{
		if (WorldManager.Instance?.World is not { } world)
			return;

		world.Gravity = Gravity;

		GChatManager.Add(new ChatMessage
		{
			Text = string.Create(CultureInfo.InvariantCulture, $"The host set the gravity to {Gravity:0.##} m/s².")
		});
	}

	[ModuleInitializer]
	internal static void Register() => NetworkActionRegistry.Register<SetGravityAction>();
}
