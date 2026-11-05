using EnsembleRoot.Scripts.Players;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Assets.Components;

/// <summary>
///     While live, a character touching <see cref="Area" /> sits at its origin. Sitting and leaving are actions, which
///     Replication submits for these events.
/// </summary>
[GlobalClass]
public partial class Seat : AssetComponent
{
	private const string HasControlsKey = "hasControls";

	[Export] public Area3D Area { get; set; } = null!;

	public bool HasControls { get; private set; }

	public PlayerHandle? Occupant { get; internal set; }

	internal static event Action<Seat>? SitRequested;
	internal static event Action? LeaveRequested;

	// Build mode's and previews' seats never watch, and a live one starts in OnInitialized
	public override void _Ready()
	{
		Area.Monitoring = false;
		Area.BodyEntered += OnBodyEntered;
	}

	public override void _ExitTree() => Area.BodyEntered -= OnBodyEntered;

	internal static void RequestLeave() => LeaveRequested?.Invoke();

	protected override void OnInitialized()
	{
		if (!Handle.IsLive)
			return;

		HasControls = Handle.Instance.Properties.GetValue(HasControlsKey).AsBool();

		Area.CollisionLayer = 0;
		Area.CollisionMask = PhysicsLayers.Character;
		Area.Monitoring = true;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (Occupant is null && GPlayerManager.LocalHandle is { CanSit: true } local && local.Body == body)
			SitRequested?.Invoke(this);
	}
}
