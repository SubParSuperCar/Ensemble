using Godot;

// ReSharper disable UnusedMember.Global

namespace Root.Gd.Camera;

public partial class PopperCam : SpringArm3D
{
	private float _dollyLogMax;
	private float _dollyLogMin;
	private float _pitch;
	private float _viewportDiagonal;
	private float _yaw;

	[ExportCategory("")][Export] public Node3D Focus { get; set; } = null!;

	[Export(PropertyHint.Range, "0,0,or_greater")]
	public float OrbitRatio { get; set; } = 2;

	[Export(PropertyHint.Range, "0,0,or_greater,radians_as_degrees")]
	public float PitchMinMax { get; set; } = Mathf.DegToRad(80);

	[Export(PropertyHint.Range, "0,0,or_greater")]
	public float YawRate { get; set; } = 2;

	[Export(PropertyHint.Range, "0,0,or_greater,suffix:m")]
	public float DollyMin
	{
		get;
		set
		{
			field = value;
			_dollyLogMin = MathF.Log(DollyMin);
		}
	} = 2;

	[Export(PropertyHint.Range, "0,0,or_greater,suffix:m")]
	public float DollyMax
	{
		get;
		set
		{
			field = value;
			_dollyLogMax = MathF.Log(DollyMax);
		}
	} = 20;

	[Export(PropertyHint.Range, "0,0,or_greater,suffix:m")]
	public float DollyStep { get; set; } = 2;

	[Export(PropertyHint.Range, "0,0,or_greater,suffix:m/s")]
	public float DollyRate { get; set; } = 40;

	public override void _Ready()
	{
		_dollyLogMin = MathF.Log(DollyMin);
		_dollyLogMax = MathF.Log(DollyMax);

		UpdateViewportDiagonal();
		GetViewport().SizeChanged += UpdateViewportDiagonal;
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMWindowFocusOut)
			Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Input.IsActionJustPressed("orbit_camera"))
			Input.MouseMode = Input.MouseModeEnum.Captured;
		else if (Input.IsActionJustReleased("orbit_camera"))
			Input.MouseMode = Input.MouseModeEnum.Visible;
		else
			switch (@event)
			{
				case InputEventMouseMotion motion when Input.IsActionPressed("orbit_camera"):
					var radiansPerPixel = (float)Math.Tau * OrbitRatio / _viewportDiagonal;
					_yaw -= motion.Relative.X * radiansPerPixel;
					_pitch = Mathf.Clamp(_pitch - motion.Relative.Y * radiansPerPixel, -PitchMinMax, PitchMinMax);

					break;
				case InputEventMouseButton { Pressed: true } button:
					// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
					switch (button.ButtonIndex)
					{
						case MouseButton.WheelUp:
							DollyLog(-DollyStep);
							break;
						case MouseButton.WheelDown:
							DollyLog(DollyStep);
							break;
					}

					break;
			}
	}

	public override void _PhysicsProcess(double delta)
	{
		var turn = Input.GetAxis("turn_left", "turn_right");

		if (turn != 0)
			_yaw -= turn * YawRate * (float)delta;

		var dolly = Input.GetAxis("dolly_in", "dolly_out");

		if (dolly != 0)
			DollyLog(dolly * DollyRate * (float)delta);

		// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
		GlobalPosition = Focus?.GlobalPosition ?? Vector3.Zero;

		var rotation = Rotation;
		rotation.Y = _yaw;
		rotation.X = _pitch;

		Rotation = rotation;
	}

	private void DollyLog(float delta)
	{
		var logLength = Mathf.Clamp(MathF.Log(SpringLength) + delta * LogScale(), _dollyLogMin, _dollyLogMax);
		SpringLength = MathF.Exp(logLength);
	}

	private float LogScale() => (_dollyLogMax - _dollyLogMin) / (DollyMax - DollyMin);

	private void UpdateViewportDiagonal()
	{
		var size = GetViewport().GetVisibleRect().Size;
		_viewportDiagonal = MathF.Max(size.Length(), 1);
	}
}
