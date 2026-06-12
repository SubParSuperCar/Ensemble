using Godot;

// ReSharper disable UnusedMember.Global

namespace Root.Scripts.Camera;

public partial class Camera : Node3D
{
	public enum CameraMode
	{
		PopperCam,
		InvisiCam
	}

	[Export] public CameraMode Mode { get; set; }

	[Export] public Godot.Collections.Dictionary<CameraMode, PackedScene> CameraScenes { get; set; } = null!;

	[ExportCategory("")] [Export] public Node Focus { get; set; } = null!;

	[Export(PropertyHint.Range, "0,0,or_greater")]
	public float OrbitSensitivity { get; set; }

	[Export(PropertyHint.Range, "0,0,or_greater")]
	public float ScrollDollyStep { get; set; }

	[Export(PropertyHint.Range, "0,0,or_greater")]
	public float KeyDollySpeed { get; set; }

	[Export(PropertyHint.Range, "0,0,or_greater")]
	public float KeyYawSpeed { get; set; }
}
