using Godot;
using Root.Gd.Camera;

namespace Root.Gd.Player;

public partial class PlayerHandle : Node
{
	[Export] public Script CharacterControllerScript { get; set; } = null!;

	[Export] public PackedScene CameraScene { get; set; } = null!;

	[Export] public Vector3 SpawnLocation { get; set; }

	public string Id { get; set; } = null!;

	public override void _Ready()
	{
		if (Id != Players.Local?.Id)
			return;

		var character = GetNode<CharacterBody3D>("Character");
		character.Position = SpawnLocation;

		var instanceId = character.GetInstanceId();
		character.SetScript(CharacterControllerScript);

		var controller = (CharacterController)InstanceFromId(instanceId)!;
		controller.SetPhysicsProcess(true);

		var camera = CameraScene.Instantiate<PopperCam>();
		camera.Focus = controller;
		AddChild(camera);

		controller.Camera = camera;
	}
}
