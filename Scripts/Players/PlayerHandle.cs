using Godot;
using Root.GdCore.Players;
using Root.GdCore.Plots;
using Root.Scripts.Cameras;

namespace Root.Scripts.Players;

[GlobalClass]
public partial class PlayerHandle : Node3D
{
	private GdOccupant _occupant = null!;
	private GdPlayer _player = null!;
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
		_player = GPlayers.GetPlayer(Id)!;

		_occupant = GPlots.GetOccupant(Id)!;
		_occupant.PlotChanged += OnPlotChanged;
	}

	public override void _ExitTree() => _occupant.PlotChanged -= OnPlotChanged;

	public override void _Ready()
	{
		Character = GetNode<CharacterBody3D>("Character");
		Character.GlobalPosition = SpawnLocation;

		var nametag = Character.GetNode<Label3D>("Nametag");
		nametag.Text = $"\"{_player.Name}\"\n({Id})\n\u2193";

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

		Controller.Camera = Camera.GetNode<Camera3D>("Camera");
	}

	private static bool IsIntersectingCuboid(Vector3 point, Transform3D transform, Vector3 size)
	{
		var localPoint = transform.AffineInverse() * point;
		var halfSize = size / 2;

		return
			Mathf.Abs(localPoint.X) < halfSize.X &&
			Mathf.Abs(localPoint.Y) < halfSize.Y &&
			Mathf.Abs(localPoint.Z) < halfSize.Z;
	}

	private void OnPlotChanged(GdPlot? plot)
	{
		if (plot is null)
			return;

		var character = (Character ?? Controller)!;
		var handle = GPlotManager.GetHandle(plot.Id);

		if (!IsIntersectingCuboid(character.GlobalPosition, handle.BoundaryTransform, handle.BoundarySize))
			character.GlobalPosition = handle.OriginTransform.Origin + SpawnOffset;
	}

	private Vector3 CalculateSpawnOffset()
	{
		var collider = (Character ?? Controller)!.GetNode<CollisionShape3D>("Collider");
		var aabb = collider.Shape.GetDebugMesh().GetAabb();

		return new Vector3(0, aabb.Size.Y / 2, 0);
	}
}
