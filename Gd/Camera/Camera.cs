using Godot;

// ReSharper disable UnusedMember.Global

namespace Root.Gd.Camera;

public partial class Camera : Node3D
{
	public enum CameraMode
	{
		PopperCam,
		InvisiCam
	}

	// TODO: Update Camera on CameraMode sets
	[Export] public CameraMode Mode { get; set; }

	[Export] public Godot.Collections.Dictionary<CameraMode, PackedScene> CameraScenes { get; set; } = [];

	[ExportCategory("")][Export] public Node Focus { get; set; } = null!;

	[Export(PropertyHint.Range, "0,0,or_greater")]
	public float OrbitSensitivity { get; set; }

	[Export(PropertyHint.Range, "0,0,or_greater")]
	public float ScrollDollyStep { get; set; }

	[Export(PropertyHint.Range, "0,0,or_greater")]
	public float KeyDollySpeed { get; set; }

	[Export(PropertyHint.Range, "0,0,or_greater")]
	public float KeyYawSpeed { get; set; }

	public override void _Ready()
	{
		var scene = CameraScenes[Mode];
		var node = scene.Instantiate();
		AddChild(node);
	}
}
