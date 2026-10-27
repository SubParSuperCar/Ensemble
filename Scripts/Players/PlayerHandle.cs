using EnsembleRoot.Common.Input;
using EnsembleRoot.GdCore.Players;
using EnsembleRoot.GdCore.Plots;
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

	private Aabb? _bodyBounds;
	private CharacterController? _controller;
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
		Body = GetNode<CharacterBody3D>("Character");
		Body.GlobalPosition = SpawnLocation;

		var nametag = Body.GetNode<Label3D>("Nametag");
		nametag.Text = $"\"{_player.Name}\"\n({Id})\n\u2193";

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
	}

	public override void _Process(double delta)
	{
		if (_controller is null)
			Interpolate(delta);
		else
			UpdateReset(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_controller is not null)
			Replicate();
	}

	public void Teleport(Vector3 position)
	{
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
		var space = PhysicsServer3D.BodyGetSpace(rid);

		PhysicsServer3D.BodySetSpace(rid, default);
		Body.GlobalPosition = position;
		Body.ForceUpdateTransform();
		PhysicsServer3D.BodySetSpace(rid, space);
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
