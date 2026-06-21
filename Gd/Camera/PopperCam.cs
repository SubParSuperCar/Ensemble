using Godot;

namespace Root.Gd.Camera;

public partial class PopperCam : SpringArm3D
{
	private float _dollyMaxLog;
	private float _dollyMinLog;
	private float _pitch;
	private float _viewportDiagonal;
	private float _yaw;

	[Export] public Node3D Focus { get; set; } = null!;

	[Export] public float OrbitRatio { get; set; } = 2;

	[Export(PropertyHint.Range, "0,90,radians_as_degrees")]
	public float PitchMinMax { get; set; } = Mathf.DegToRad(85);

	[Export(PropertyHint.None, "radians_as_degrees,suffix:\u00B0/s")]
	public float YawRate { get; set; } = Mathf.DegToRad(90);

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float DollyMin
	{
		get;
		set
		{
			field = value;
			_dollyMinLog = MathF.Log(DollyMin);
		}
	} = 2;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float DollyMax
	{
		get;
		set
		{
			field = value;
			_dollyMaxLog = MathF.Log(DollyMax);
		}
	} = 32;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float DollyStep { get; set; } = 2;

	[Export(PropertyHint.None, "suffix:m/s")]
	public float DollyRate { get; set; } = 24;

	public override void _Ready()
	{
		_dollyMinLog = MathF.Log(DollyMin);
		_dollyMaxLog = MathF.Log(DollyMax);

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
					var radiansPerPixel = Mathf.Tau * OrbitRatio / _viewportDiagonal;
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
		var turnInput = Input.GetAxis("turn_left", "turn_right");

		if (turnInput != 0)
			_yaw -= turnInput * YawRate * (float)delta;

		var dollyInput = Input.GetAxis("dolly_in", "dolly_out");

		if (dollyInput != 0)
			DollyLog(dollyInput * DollyRate * (float)delta);

		// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
		GlobalPosition = Focus?.GlobalPosition ?? Vector3.Zero;

		var rotation = Rotation;
		rotation.Y = _yaw;
		rotation.X = _pitch;

		Rotation = rotation;
	}

	private void DollyLog(float delta)
	{
		var logLength = Mathf.Clamp(MathF.Log(SpringLength) + delta * LogScale(), _dollyMinLog, _dollyMaxLog);
		SpringLength = MathF.Exp(logLength);
	}

	private float LogScale() => (_dollyMaxLog - _dollyMinLog) / (DollyMax - DollyMin);

	private void UpdateViewportDiagonal()
	{
		var size = GetViewport().GetVisibleRect().Size;
		_viewportDiagonal = MathF.Max(size.Length(), 1);
	}
}
