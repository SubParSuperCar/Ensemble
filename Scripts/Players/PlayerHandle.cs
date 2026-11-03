using EnsembleRoot.Common.Input;
using EnsembleRoot.GdCore.Players;
using EnsembleRoot.GdCore.Plots;
using EnsembleRoot.Scripts.Assets.Components;
using EnsembleRoot.Scripts.Cameras;
using Godot;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global

namespace EnsembleRoot.Scripts.Players;

[GlobalClass]
public partial class PlayerHandle : Node3D
{
	private const double ResetHoldDuration = 1d;

	private static readonly StringName ResetAction = "character_reset";
	private static readonly StringName FlashlightAction = "character_flashlight_toggle";

	private Aabb? _bodyBounds;
	private CharacterController? _controller;
	private SpotLight3D _flashlight = null!;
	private GdOccupant _occupant = null!;
	private GdPlayer _player = null!;
	private GdPlot? _plot;
	private double _resetHeldFor;

	[Export] public Script CharacterControllerScript { get; set; } = null!;

	[Export] public PackedScene CameraScene { get; set; } = null!;
	[Export] public Node3D TerrainNode { get; set; } = null!;

	[Export] public Vector3 SpawnLocation { get; set; }

	public string Id { get; set; } = null!;

	public CharacterBody3D Body { get; private set; } = null!;

	public Aabb BodyBounds => _bodyBounds ??= CalculateBodyBounds();

	private Vector3 SpawnOffset => new(0, -BodyBounds.Position.Y, 0);

	public override void _EnterTree()
	{
		_player = GPlayers.GetPlayer(Id)!;

		_occupant = GPlots.GetOccupant(Id)!;

		OnPlotChanged(_occupant.Plot);
		_occupant.PlotChanged += OnPlotChanged;
	}

	public override void _Ready()
	{
		// After a seat's creation moves it, and the camera after the character, so they all interpolate together
		ProcessPhysicsPriority = 1;

		Body = GetNode<CharacterBody3D>("Character");
		Body.GlobalPosition = SpawnLocation;

		var nametag = Body.GetNode<Label3D>("Nametag");
		nametag.Text = $"\"{_player.Name}\"\n({Id})\n\u2193";

		_flashlight = Body.GetNode<SpotLight3D>("Flashlight");

		if (!string.Equals(Id, GPlayers.Local?.Id, StringComparison.Ordinal))
		{
			// Moved by Interpolate every frame, not by physics
			Body.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
			return;
		}

		// Only the local body is controlled, so its script is swapped in place. Swapping invalidates the managed
		// wrapper, so the node is fetched again by instance ID.
		var instanceId = Body.GetInstanceId();
		Body.SetScript(CharacterControllerScript);
		Body = _controller = (CharacterController)InstanceFromId(instanceId)!;

		var camera = CameraScene.Instantiate<PopperCam>();
		camera.ProcessPhysicsPriority = ProcessPhysicsPriority + 1;
		AddChild(camera);
		camera.Focus = _controller;

		_controller.Camera = camera.GetNode<Camera3D>("Camera");
		_controller.Terrain = TerrainNode;

		// The node was already ready before the swap, so the new script is started by hand
		_controller._Ready();
		_controller.SetPhysicsProcess(true);
	}

	public override void _ExitTree()
	{
		_occupant.PlotChanged -= OnPlotChanged;
		_plot?.IsSpawnedChanged -= OnIsPlotSpawnedChanged;

		Seat?.Occupant = null;
	}

	public override void _Process(double delta)
	{
		if (_controller is null)
		{
			Interpolate(delta);
			return;
		}

		UpdateReset(delta);
		UpdateSeatLeave();

		if (!InputSink.IsSunk && Input.IsActionJustPressed(FlashlightAction))
			_flashlight.Visible = !_flashlight.Visible;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_standUp is var (above, seat))
		{
			_standUp = null;
			StandUp(above, seat);
		}

		if (Seat is not null)
			FollowSeat();
		else if (_controller is not null)
			Replicate();
	}

	public void Teleport(Vector3 position)
	{
		// Only the host can free the seat, so the character moves once it has
		if (Seat is not null)
		{
			_teleportAfterSeat = position;
			Seat.RequestLeave();
			return;
		}

		_standUp = null;

		Body.Velocity = Vector3.Zero;
		Jump(position);

		Log.Debug("Teleported {Player} to {Position}", _player.Name, position);
	}

	public void Respawn() => Teleport(_occupant.Plot is { } plot ? GetPlotSpawn(plot) : SpawnLocation);

	// Jolt moves a kinematic body to a new transform during the next step, shoving whatever it touched where it was;
	// leaving the space for the move makes it a jump
	private void Jump(Vector3 position)
	{
		var rid = Body.GetRid();

		PhysicsServer3D.BodySetSpace(rid, default);
		Body.GlobalPosition = position;
		Body.ForceUpdateTransform();
		PhysicsServer3D.BodySetSpace(rid, GetWorld3D().Space);
		Body.ResetPhysicsInterpolation();
	}

	private void UpdateReset(double delta)
	{
		if (InputSink.IsSunk || !Input.IsActionPressed(ResetAction))
		{
			_resetHeldFor = 0;
			return;
		}

		var heldFor = _resetHeldFor + delta;

		if (_resetHeldFor < ResetHoldDuration && heldFor >= ResetHoldDuration)
			Respawn();

		_resetHeldFor = heldFor;
	}

	private void OnPlotChanged(GdPlot? plot)
	{
		Vacate();

		_plot?.IsSpawnedChanged -= OnIsPlotSpawnedChanged;
		_plot = plot;
		_plot?.IsSpawnedChanged += OnIsPlotSpawnedChanged;

		ReturnToPlot();
	}

	// A live creation can carry the character off, but building happens inside the boundary
	private void OnIsPlotSpawnedChanged(bool isSpawned)
	{
		if (!isSpawned)
			ReturnToPlot();
	}

	private void ReturnToPlot()
	{
		if (_plot is null || _controller is null)
			return;

		var handle = GPlotManager.GetHandle(_plot.Id);

		if (!IsInsideCuboid(Body.GlobalPosition, handle.BoundaryTransform, handle.BoundarySize))
			Teleport(GetPlotSpawn(_plot));
	}

	private Vector3 GetPlotSpawn(GdPlot plot) => GPlotManager.GetHandle(plot.Id).OriginTransform.Origin + SpawnOffset;

	private Aabb CalculateBodyBounds()
	{
		var collider = Body.GetNode<CollisionShape3D>("Collider");
		return collider.Transform * collider.Shape.GetDebugMesh().GetAabb();
	}

	private static bool IsInsideCuboid(Vector3 point, Transform3D transform, Vector3 size)
	{
		var localPoint = (transform.AffineInverse() * point).Abs();
		var halfSize = size / 2;

		return localPoint.X < halfSize.X && localPoint.Y < halfSize.Y && localPoint.Z < halfSize.Z;
	}
}
