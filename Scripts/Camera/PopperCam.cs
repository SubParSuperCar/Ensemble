using Godot;

namespace Root.Scripts.Camera;

public partial class PopperCam : SpringArm3D
{
	private Vector2 _capturedMousePosition;
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
	} = 1.25f;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider,suffix:m")]
	public float DollyMax
	{
		get;
		set
		{
			field = value;
			_dollyMaxLog = MathF.Log(DollyMax);
		}
	} = 48;

	[Export(PropertyHint.Range, "0,0,or_greater,hide_slider")]
	public float DollyStep { get; set; } = 2;

	[Export] public float DollyRate { get; set; } = 24;

	public override void _Ready()
	{
		_yaw = Rotation.Y;
		_pitch = Rotation.X;

		_dollyMinLog = MathF.Log(DollyMin);
		_dollyMaxLog = MathF.Log(DollyMax);

		RecalculateViewportDiagonal();
		GetViewport().SizeChanged += RecalculateViewportDiagonal;
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMWindowFocusOut)
			ReleaseMouse();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Input.IsActionJustPressedByEvent("orbit_camera", @event))
			CaptureMouse();
		else if (Input.IsActionJustReleasedByEvent("orbit_camera", @event))
			ReleaseMouse();
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
							ApplyDollyDelta(-DollyStep);
							break;
						case MouseButton.WheelDown:
							ApplyDollyDelta(DollyStep);
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
			ApplyDollyDelta(dollyInput * DollyRate * (float)delta);

		// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
		GlobalPosition = Focus?.GlobalPosition ?? Vector3.Zero;

		var rotation = Rotation;
		rotation.Y = _yaw;
		rotation.X = _pitch;

		Rotation = rotation;
	}

	private void ApplyDollyDelta(float delta)
	{
		var logScale = (_dollyMaxLog - _dollyMinLog) / (DollyMax - DollyMin);
		var logLength = Mathf.Clamp(MathF.Log(SpringLength) + delta * logScale, _dollyMinLog, _dollyMaxLog);
		SpringLength = MathF.Exp(logLength);
	}

	private void RecalculateViewportDiagonal()
	{
		var size = GetViewport().GetVisibleRect().Size;
		_viewportDiagonal = MathF.Max(size.Length(), 1);
	}

	private void CaptureMouse()
	{
		_capturedMousePosition = GetViewport().GetMousePosition();
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	private void ReleaseMouse()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetViewport().WarpMouse(_capturedMousePosition);
	}
}
