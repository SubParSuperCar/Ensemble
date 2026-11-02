using EnsembleRoot.Scripts.Players;
using Godot;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Assets.Components;

/// <summary>
///     Makes an asset a seat: while live, a character touching <see cref="Area" /> sits at its origin, following it out
///     of the physics space. Sitting and leaving are actions, which Replication submits for these events.
/// </summary>
[GlobalClass]
public partial class Seat : AssetComponent
{
	private const string HasControlsKey = "hasControls";

	/// <summary>Detects touches with its shape, and is where a seated character's origin goes.</summary>
	[Export]
	public Area3D Area { get; set; } = null!;

	/// <summary>Whether a plot's occupant sitting here can control its creation.</summary>
	public bool HasControls => Handle.Instance.Properties.GetValue(HasControlsKey).AsBool();

	public PlayerHandle? Occupant { get; internal set; }

	public static event Action<Seat>? SitRequested;
	public static event Action? LeaveRequested;

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
