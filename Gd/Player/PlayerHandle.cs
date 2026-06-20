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
	public PopperCam Camera { get; private set; } = null!;

	public override void _Ready()
	{
		Character = GetNode<CharacterBody3D>("Character");
		Character.Position = SpawnLocation;

		if (Id != Players.Local?.Id)
			return;

		var instanceId = Character.GetInstanceId();
		Character.SetScript(CharacterControllerScript);

		var controller = (CharacterController)InstanceFromId(instanceId)!;
		controller.SetPhysicsProcess(true);

		Camera = CameraScene.Instantiate<PopperCam>();
		Camera.Focus = controller;
		AddChild(Camera);

		controller.Camera = Camera;
	}

	public override void _EnterTree()
	{
		_occupant = Plots.GetOccupant(Id);
		_occupant.PlotChanged += OnPlotChanged;
	}

	public override void _ExitTree() => _occupant.PlotChanged -= OnPlotChanged;

	private void OnPlotChanged(GdPlot? plot)
	{
		if (plot?.Id is not { } id)
			return;

		var node = PlotManager.Nodes[id];
		Character.GlobalPosition = node.GlobalPosition;
	}
}
