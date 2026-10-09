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

	private static readonly StringName ResetAction = "char_reset";

	private GdOccupant _occupant = null!;
	private GdPlayer _player = null!;
	private double _resetHeldFor;
	private Vector3? _spawnOffset;

	[Export] public Script CharacterControllerScript { get; set; } = null!;

	[Export] public PackedScene CameraScene { get; set; } = null!;
	[Export] public Node3D TerrainNode { get; set; } = null!;

	[Export] public Vector3 SpawnLocation { get; set; }

	public string Id { get; set; } = null!;

	public CharacterBody3D Body => Character ?? Controller!;

	private CharacterBody3D? Character { get; set; }
	private CharacterController? Controller { get; set; }
	private PopperCam? Camera { get; set; }

	private Vector3 SpawnOffset => _spawnOffset ??= CalculateSpawnOffset();

	public override void _EnterTree()
	{
		_player = GPlayers.GetPlayer(Id)!;

		_occupant = GPlots.GetOccupant(Id)!;
		_occupant.PlotChanged += OnPlotChanged;
	}

	public override void _Ready()
	{
		Character = GetNode<CharacterBody3D>("Character");
		Character.GlobalPosition = SpawnLocation;

		var nametag = Character.GetNode<Label3D>("Nametag");
		nametag.Text = $"\"{_player.Name}\"\n({Id})\n\u2193";

		if (!string.Equals(Id, GPlayers.Local?.Id, StringComparison.Ordinal))
			return;

		// Only the local body is controlled, so its script is swapped in place. Swapping invalidates the managed
		// wrapper, so the node is fetched again by instance ID.
		var instanceId = Character.GetInstanceId();
		Character.SetScript(CharacterControllerScript);
		Character = null;

		Controller = (CharacterController)InstanceFromId(instanceId)!;

		Camera = CameraScene.Instantiate<PopperCam>();
		AddChild(Camera);
		Camera.Focus = Controller;

		Controller.Camera = Camera.GetNode<Camera3D>("Camera");
		Controller.Terrain = TerrainNode;

		// The node was already ready before the swap, so the new script is started by hand
		Controller._Ready();
		Controller.SetPhysicsProcess(true);
	}

	public override void _ExitTree() => _occupant.PlotChanged -= OnPlotChanged;

	public override void _Process(double delta)
	{
		if (Controller is null)
			Interpolate(delta);
		else
			UpdateReset(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Controller is not null)
			Replicate(delta);
	}

	public void Teleport(Vector3 position)
	{
		Body.GlobalPosition = position;
		Body.Velocity = Vector3.Zero;

		Log.Debug("Teleported {Player} to {Position}", _player.Name, position);
	}

	public void Respawn() => Teleport(_occupant.Plot is { } plot ? GetPlotSpawn(plot) : SpawnLocation);

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
		if (plot is null || Controller is null)
			return;

		var handle = GPlotManager.GetHandle(plot.Id);

		if (!IsInsideCuboid(Body.GlobalPosition, handle.BoundaryTransform, handle.BoundarySize))
			Body.GlobalPosition = GetPlotSpawn(plot);
	}

	private Vector3 GetPlotSpawn(GdPlot plot) => GPlotManager.GetHandle(plot.Id).OriginTransform.Origin + SpawnOffset;

	private Vector3 CalculateSpawnOffset()
	{
		var collider = Body.GetNode<CollisionShape3D>("Collider");
		var aabb = collider.Transform * collider.Shape.GetDebugMesh().GetAabb();

		return new Vector3(0, -aabb.Position.Y, 0);
	}

	private static bool IsInsideCuboid(Vector3 point, Transform3D transform, Vector3 size)
	{
		var localPoint = (transform.AffineInverse() * point).Abs();
		var halfSize = size / 2;

		return localPoint.X < halfSize.X && localPoint.Y < halfSize.Y && localPoint.Z < halfSize.Z;
	}
}
