using Godot;
using Root.Core.Gd.Plot;
using Root.Scripts.Camera;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Scripts.Player;

public partial class PlayerHandle : Node
{
	private GdOccupant _occupant = null!;
	private Vector3? _spawnOffset;

	[Export] public Script CharacterControllerScript { get; set; } = null!;

	[Export] public PackedScene CameraScene { get; set; } = null!;

	[Export] public Vector3 SpawnLocation { get; set; }

	public string Id { get; set; } = null!;

	public CharacterBody3D? Character { get; private set; }
	public CharacterController? Controller { get; private set; }

	public PopperCam? Camera { get; private set; }

	public Vector3 SpawnOffset => _spawnOffset ??= CalculateSpawnOffset();

	public override void _EnterTree()
	{
		_occupant = GPlots.GetOccupant(Id)!;
		_occupant.PlotChanged += OnPlotChanged;
	}

	public override void _ExitTree() => _occupant.PlotChanged -= OnPlotChanged;

	public override void _Ready()
	{
		Character = GetNode<CharacterBody3D>("Character");
		Character.GlobalPosition = SpawnLocation;

		if (!string.Equals(Id, GPlayers.Local?.Id, StringComparison.Ordinal))
			return;

		var instanceId = Character.GetInstanceId();
		Character.SetScript(CharacterControllerScript);
		Character = null;

		Controller = (CharacterController)InstanceFromId(instanceId)!;
		Controller.SetPhysicsProcess(true);

		Camera = CameraScene.Instantiate<PopperCam>();
		Camera.Focus = Controller;
		AddChild(Camera);

		Controller.Camera = Camera;
	}

	private void OnPlotChanged(GdPlot? plot)
	{
		if (plot is null)
			return;

		var handle = GPlotManager.GetHandle(plot);
		(Character ?? Controller)!.GlobalPosition = handle.SpawnLocation + SpawnOffset;
	}

	private Vector3 CalculateSpawnOffset()
	{
		var collider = (Character ?? Controller)!.GetNode<CollisionShape3D>("Collider");
		var aabb = collider.Shape.GetDebugMesh().GetAabb();

		return new Vector3(0, aabb.Size.Y / 2, 0);
	}
}
