using Godot;
using Root.Core.Gd.Plot;
using Root.Gd.Camera;

// ReSharper disable MemberCanBePrivate.Global

namespace Root.Gd.Player;

public partial class PlayerHandle : Node
{
	private GdOccupant _occupant = null!;

	[Export] public Script CharacterControllerScript { get; set; } = null!;

	[Export] public PackedScene CameraScene { get; set; } = null!;

	[Export] public Vector3 SpawnLocation { get; set; }

	public string Id { get; set; } = null!;

	public CharacterBody3D Character { get; private set; } = null!;
	public CharacterController Controller { get; private set; } = null!;

	public PopperCam Camera { get; private set; } = null!;

	public override void _Ready()
	{
		Character = GetNode<CharacterBody3D>("Character");
		Character.GlobalPosition = SpawnLocation;

		if (!string.Equals(Id, GPlayers.Local?.Id, StringComparison.OrdinalIgnoreCase))
			return;

		var instanceId = Character.GetInstanceId();
		Character.SetScript(CharacterControllerScript);

		Controller = (CharacterController)InstanceFromId(instanceId)!;
		Controller.SetPhysicsProcess(true);

		Camera = CameraScene.Instantiate<PopperCam>();
		Camera.Focus = Controller;
		AddChild(Camera);

		Controller.Camera = Camera;
	}

	public override void _EnterTree()
	{
		_occupant = GPlots.GetOccupant(Id);
		_occupant.PlotChanged += OnPlotChanged;
	}

	public override void _ExitTree() => _occupant.PlotChanged -= OnPlotChanged;

	private void OnPlotChanged(GdPlot? plot)
	{
		if (plot is null)
			return;

		// TODO: Offset based on character height
		var handle = GPlotManager.GetHandle(plot);
		Controller.GlobalPosition = handle.SpawnLocation;
	}
}
